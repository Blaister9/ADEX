"""Runs a policy against a synthetic environment and reports what happened."""

from __future__ import annotations

from dataclasses import dataclass, field

from adex_simulator.policies import UniformRandomPolicy
from adex_simulator.scenarios import Scenario
from adex_simulator.seeds import derive_seed

# ASCII only: this banner is printed to consoles and CI logs whose encoding we do
# not control, and a mangled warning is a weaker warning.
SYNTHETIC_BANNER = (
    "SYNTHETIC RESULTS - generated from a simulated environment with known "
    "conversion rates. These numbers say something about the engine and nothing "
    "about any real audience. They must never be presented as commercial uplift."
)


@dataclass(frozen=True, slots=True)
class RunResult:
    scenario: str
    policy: str
    policy_version: int
    rounds: int
    seed: int
    impressions: dict[str, int]
    conversions: dict[str, int]
    cumulative_regret: float
    best_alternative: str

    @property
    def total_conversions(self) -> int:
        return sum(self.conversions.values())

    @property
    def observed_rate(self) -> float:
        return self.total_conversions / self.rounds if self.rounds else 0.0

    @property
    def share_of_best(self) -> float:
        """Fraction of impressions that went to the truly best alternative."""
        return self.impressions.get(self.best_alternative, 0) / self.rounds if self.rounds else 0.0


@dataclass(frozen=True, slots=True)
class SimulationRunner:
    """Deterministic given ``(scenario, seed, rounds)`` — no ambient randomness anywhere."""

    salt: bytes = b"simulation-seed-salt"
    policy: UniformRandomPolicy = field(default_factory=UniformRandomPolicy)

    def run(self, scenario: Scenario, rounds: int, seed: int) -> RunResult:
        if rounds <= 0:
            raise ValueError("A run needs at least one round.")

        environment = scenario.environment(seed)
        eligible = list(environment.alternatives)

        impressions = dict.fromkeys(eligible, 0)
        conversions = dict.fromkeys(eligible, 0)
        cumulative_regret = 0.0

        for round_index in range(rounds):
            # One synthetic subject per round, seeded exactly the way the online
            # path seeds a real one.
            subject = f"anon_sim_{seed}_{round_index}"
            draw = derive_seed(
                self.salt,
                scenario.tenant_id,
                scenario.placement,
                self.policy.key,
                self.policy.version,
                subject,
            )

            selection = self.policy.select(eligible, draw)
            impressions[selection.alternative] += 1
            conversions[selection.alternative] += environment.reward(selection.alternative)
            cumulative_regret += environment.regret(selection.alternative)

        return RunResult(
            scenario=scenario.name,
            policy=self.policy.key,
            policy_version=self.policy.version,
            rounds=rounds,
            seed=seed,
            impressions=impressions,
            conversions=conversions,
            cumulative_regret=cumulative_regret,
            best_alternative=environment.best_alternative,
        )


def format_report(scenario: Scenario, result: RunResult) -> str:
    lines = [
        SYNTHETIC_BANNER,
        "",
        f"scenario        : {result.scenario}",
        f"description     : {scenario.description}",
        f"policy          : {result.policy} v{result.policy_version}",
        f"rounds          : {result.rounds}",
        f"seed            : {result.seed}",
        f"best alternative: {result.best_alternative} (known: the environment is synthetic)",
        "",
        f"{'alternative':<24}{'impressions':>12}{'conversions':>13}{'observed rate':>16}",
    ]

    for alternative in sorted(result.impressions):
        shown = result.impressions[alternative]
        converted = result.conversions[alternative]
        rate = converted / shown if shown else 0.0
        lines.append(f"{alternative:<24}{shown:>12}{converted:>13}{rate:>16.4f}")

    lines += [
        "",
        f"total conversions   : {result.total_conversions}",
        f"observed rate       : {result.observed_rate:.4f}",
        f"share of best shown : {result.share_of_best:.4f}",
        f"cumulative regret   : {result.cumulative_regret:.2f} expected conversions given up",
    ]

    return "\n".join(lines)
