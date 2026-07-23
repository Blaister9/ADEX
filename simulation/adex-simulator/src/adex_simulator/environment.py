"""Synthetic environments for offline evaluation.

Everything produced here is SYNTHETIC. It validates engineering and algorithm
behaviour and must never be presented as evidence of commercial uplift
(``AGENTS.md`` invariant 10). The report writer stamps that on every artefact.
"""

from __future__ import annotations

import random
from collections.abc import Mapping
from dataclasses import dataclass, field


@dataclass(frozen=True, slots=True)
class BernoulliEnvironment:
    """A placement whose alternatives convert at fixed, known probabilities.

    Known optima are the point: with a ground truth in hand, a policy's regret
    is measurable, and "the policy converged" becomes a checkable claim instead
    of an impression.
    """

    #: alternative key -> true conversion probability
    true_rates: Mapping[str, float]
    seed: int
    _rng: random.Random = field(init=False, repr=False, compare=False)

    def __post_init__(self) -> None:
        if not self.true_rates:
            raise ValueError("An environment needs at least one alternative.")
        for key, rate in self.true_rates.items():
            if not 0.0 <= rate <= 1.0:
                raise ValueError(f"Conversion rate for '{key}' must be within [0, 1], got {rate}.")

        # Seeded per environment so a run is reproducible from its scenario name
        # and seed alone.
        object.__setattr__(self, "_rng", random.Random(self.seed))

    @property
    def alternatives(self) -> tuple[str, ...]:
        return tuple(self.true_rates)

    @property
    def best_alternative(self) -> str:
        return max(self.true_rates, key=lambda key: self.true_rates[key])

    @property
    def best_rate(self) -> float:
        return self.true_rates[self.best_alternative]

    def reward(self, alternative: str) -> int:
        """Draw a Bernoulli reward for one impression of ``alternative``."""
        if alternative not in self.true_rates:
            raise KeyError(f"'{alternative}' is not an alternative of this environment.")
        return 1 if self._rng.random() < self.true_rates[alternative] else 0

    def regret(self, alternative: str) -> float:
        """Expected reward given up by showing ``alternative`` instead of the best one."""
        return self.best_rate - self.true_rates[alternative]
