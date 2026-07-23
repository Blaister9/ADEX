"""Python half of the shared uniform-random policy behavior contract."""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from adex_simulator.policies import UniformRandomPolicy

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "tests"
    / "fixtures"
    / "uniform-random-policy-vectors.json"
)


def load_vectors() -> list[dict[str, object]]:
    return json.loads(FIXTURE.read_text(encoding="utf-8"))["vectors"]


@pytest.mark.parametrize("vector", load_vectors(), ids=lambda vector: str(vector["name"]))
def test_uniform_random_matches_shared_policy_vectors(vector: dict[str, object]) -> None:
    selection = UniformRandomPolicy().select(
        [str(value) for value in vector["eligible_alternatives"]],  # type: ignore[union-attr]
        int(vector["seed"]),  # type: ignore[arg-type]
    )

    assert selection.alternative == vector["expected_alternative"]
    assert selection.propensity == pytest.approx(float(vector["expected_propensity"]))  # type: ignore[arg-type]
