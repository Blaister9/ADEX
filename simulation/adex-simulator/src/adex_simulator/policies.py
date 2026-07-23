"""Offline reference implementations of the ADEX policies.

These exist to evaluate and compare policies before they are promoted to the
online path, never to serve traffic: production decisions are made by
``Adex.Domain`` in C# (ADR-0003).

Only ``uniform-random`` is implemented, matching what the foundation actually
ships. Adding Thompson Sampling here before the reward pipeline is verified
would be optimizing against a signal nobody has checked (ADR-0009).
"""

from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class Selection:
    """The outcome of a policy evaluation."""

    alternative: str
    #: Probability with which this policy would have chosen ``alternative``.
    #: Recorded from the first policy onwards, because off-policy evaluation
    #: cannot be added to unlogged history.
    propensity: float
    explanation: str


@dataclass(frozen=True, slots=True)
class UniformRandomPolicy:
    """Pick uniformly from the eligible set, using the seed handed in.

    Pure by construction: no clock, no ambient randomness, no I/O. The same
    inputs always produce the same selection, which is what makes a stored
    decision replayable.
    """

    key: str = "uniform-random"
    version: int = 1

    def select(self, eligible: Sequence[str], seed: int) -> Selection:
        if not eligible:
            raise ValueError("A policy is never called with an empty eligible set.")

        index = seed % len(eligible)
        return Selection(
            alternative=eligible[index],
            propensity=1.0 / len(eligible),
            explanation=f"Uniform selection {index + 1} of {len(eligible)} from the eligible set.",
        )
