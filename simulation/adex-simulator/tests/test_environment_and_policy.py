from __future__ import annotations

import pytest

from adex_simulator import scenarios
from adex_simulator.environment import BernoulliEnvironment
from adex_simulator.policies import UniformRandomPolicy
from adex_simulator.runner import SimulationRunner


def test_rewards_are_bernoulli() -> None:
    environment = BernoulliEnvironment({"a": 0.5}, seed=1)
    assert {environment.reward("a") for _ in range(200)} <= {0, 1}


def test_observed_rate_approaches_the_true_rate() -> None:
    environment = BernoulliEnvironment({"a": 0.25}, seed=42)
    draws = [environment.reward("a") for _ in range(20_000)]
    assert abs(sum(draws) / len(draws) - 0.25) < 0.02


def test_the_optimum_is_known_because_the_environment_is_synthetic() -> None:
    environment = scenarios.get("services-clear-winner").environment(seed=1)
    assert environment.best_alternative == "book-appointment"
    assert environment.regret("book-appointment") == 0.0
    assert environment.regret("request-callback") > 0.0


def test_an_unknown_alternative_is_an_error_not_a_zero_reward() -> None:
    environment = BernoulliEnvironment({"a": 0.5}, seed=1)
    with pytest.raises(KeyError):
        environment.reward("not-configured")


@pytest.mark.parametrize("rate", [-0.1, 1.1])
def test_impossible_conversion_rates_are_rejected(rate: float) -> None:
    with pytest.raises(ValueError):
        BernoulliEnvironment({"a": rate}, seed=1)


def test_an_environment_needs_alternatives() -> None:
    with pytest.raises(ValueError):
        BernoulliEnvironment({}, seed=1)


def test_uniform_policy_is_pure() -> None:
    policy = UniformRandomPolicy()
    assert policy.select(["a", "b", "c"], 12345) == policy.select(["a", "b", "c"], 12345)


def test_uniform_policy_reports_its_propensity() -> None:
    assert UniformRandomPolicy().select(["a", "b", "c", "d"], 1).propensity == pytest.approx(0.25)


def test_uniform_policy_refuses_an_empty_eligible_set() -> None:
    with pytest.raises(ValueError):
        UniformRandomPolicy().select([], 1)


def test_uniform_policy_allocates_evenly_over_many_subjects() -> None:
    scenario = scenarios.get("services-clear-winner")
    result = SimulationRunner().run(scenario, rounds=30_000, seed=11)

    expected = result.rounds / len(result.impressions)
    for shown in result.impressions.values():
        assert abs(shown - expected) < expected * 0.05


def test_uniform_policy_accumulates_regret_because_it_never_learns() -> None:
    # Stage 2 of the policy sequence deliberately does not adapt. Its purpose is
    # to validate the reward pipeline, and this asserts it is not quietly doing
    # something cleverer than advertised (ADR-0009).
    scenario = scenarios.get("services-clear-winner")
    result = SimulationRunner().run(scenario, rounds=5_000, seed=5)

    assert result.cumulative_regret > 0
    assert 0.30 < result.share_of_best < 0.36
