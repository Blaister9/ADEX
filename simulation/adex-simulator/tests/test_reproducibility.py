"""Reproducibility is a stated quality attribute, so it is asserted, not assumed."""

from __future__ import annotations

import pytest

from adex_simulator import scenarios
from adex_simulator.runner import SYNTHETIC_BANNER, SimulationRunner, format_report


def test_the_same_seed_reproduces_the_same_run() -> None:
    scenario = scenarios.get("services-clear-winner")
    runner = SimulationRunner()

    first = runner.run(scenario, rounds=2_000, seed=20260722)
    second = runner.run(scenario, rounds=2_000, seed=20260722)

    assert first == second


def test_a_different_seed_changes_the_outcome() -> None:
    scenario = scenarios.get("services-clear-winner")
    runner = SimulationRunner()

    first = runner.run(scenario, rounds=2_000, seed=1)
    second = runner.run(scenario, rounds=2_000, seed=2)

    assert first.conversions != second.conversions


def test_the_report_is_byte_identical_for_the_same_seed() -> None:
    scenario = scenarios.get("catalog-low-signal")
    runner = SimulationRunner()

    first = format_report(scenario, runner.run(scenario, rounds=1_000, seed=7))
    second = format_report(scenario, runner.run(scenario, rounds=1_000, seed=7))

    assert first == second


def test_every_report_is_labelled_synthetic() -> None:
    # AGENTS.md invariant 10: synthetic output may never look like commercial
    # evidence. The label is part of the artefact, not of the surrounding prose.
    scenario = scenarios.get("services-near-tie")
    report = format_report(scenario, SimulationRunner().run(scenario, rounds=100, seed=3))

    assert SYNTHETIC_BANNER in report
    assert "SYNTHETIC" in report


def test_a_run_needs_at_least_one_round() -> None:
    with pytest.raises(ValueError):
        SimulationRunner().run(scenarios.get("services-clear-winner"), rounds=0, seed=1)
