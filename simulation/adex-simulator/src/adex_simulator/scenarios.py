"""Named, reproducible scenarios.

Two unrelated reference tenants, always. If a scenario only makes sense for one
of them, the thing being modelled is tenant configuration rather than engine
behaviour — and the engine is what this simulator exists to test.

None of these numbers describe a real business. They are chosen to make a
policy's behaviour observable: a clear optimum, a near-tie, and a low-signal
case where a naive policy looks convincing on far too little evidence.
"""

from __future__ import annotations

from dataclasses import dataclass

from adex_simulator.environment import BernoulliEnvironment


@dataclass(frozen=True, slots=True)
class Scenario:
    name: str
    tenant_id: str
    placement: str
    description: str
    true_rates: dict[str, float]

    def environment(self, seed: int) -> BernoulliEnvironment:
        return BernoulliEnvironment(true_rates=self.true_rates, seed=seed)


SCENARIOS: dict[str, Scenario] = {
    "services-clear-winner": Scenario(
        name="services-clear-winner",
        tenant_id="ten_01JQZ6A1B2C3D4E5F6G7H8J9K0",
        placement="homepage.primary-cta",
        description="One alternative is clearly better. Any working policy must find it.",
        true_rates={
            "request-callback": 0.030,
            "book-appointment": 0.075,
            "see-pricing": 0.041,
        },
    ),
    "services-near-tie": Scenario(
        name="services-near-tie",
        tenant_id="ten_01JQZ6A1B2C3D4E5F6G7H8J9K0",
        placement="homepage.primary-cta",
        description=(
            "Two alternatives differ by half a percentage point. A policy that claims "
            "confidence here quickly is wrong, not fast."
        ),
        true_rates={
            "request-callback": 0.052,
            "book-appointment": 0.057,
        },
    ),
    "catalog-low-signal": Scenario(
        name="catalog-low-signal",
        tenant_id="ten_01JQZ6B2C3D4E5F6G7H8J9K0M1",
        placement="catalog.recommendation-slot",
        description=(
            "Rare conversions. Exists to expose policies and dashboards that read "
            "noise as a result."
        ),
        true_rates={
            "order-by-popularity": 0.004,
            "order-by-margin": 0.006,
            "order-by-recency": 0.003,
        },
    ),
}


def get(name: str) -> Scenario:
    try:
        return SCENARIOS[name]
    except KeyError:
        available = ", ".join(sorted(SCENARIOS))
        raise KeyError(f"Unknown scenario '{name}'. Available: {available}.") from None
