"""The Python half of the cross-language seed contract.

`tests/unit/Adex.UnitTests/CrossLanguageSeedTests.cs` asserts the same file from
the C# side. If either implementation drifts, one of the two suites fails.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from adex_simulator.seeds import derive_seed

FIXTURES = Path(__file__).resolve().parents[3] / "tests" / "fixtures" / "decision-seed-vectors.json"


def load_vectors() -> list[dict[str, object]]:
    document = json.loads(FIXTURES.read_text(encoding="utf-8"))
    return document["vectors"]


def test_fixture_file_is_present() -> None:
    assert FIXTURES.exists(), f"Missing shared fixture file: {FIXTURES}"
    assert len(load_vectors()) >= 5


@pytest.mark.parametrize("vector", load_vectors(), ids=lambda vector: str(vector["name"]))
def test_matches_the_shared_vectors(vector: dict[str, object]) -> None:
    actual = derive_seed(
        str(vector["salt"]).encode("utf-8"),
        str(vector["tenant_id"]),
        str(vector["placement"]),
        str(vector["policy_key"]),
        int(vector["policy_version"]),  # type: ignore[arg-type]
        str(vector["subject_or_decision_id"]),
    )
    assert actual == vector["expected_seed"]


def test_is_stable_for_the_same_inputs() -> None:
    args = (b"salt", "ten_x", "placement", "uniform-random", 1, "subject")
    assert derive_seed(*args) == derive_seed(*args)


def test_separates_every_field() -> None:
    baseline = derive_seed(b"salt", "ten_a", "placement", "uniform-random", 1, "subject")

    assert baseline != derive_seed(b"salt", "ten_b", "placement", "uniform-random", 1, "subject")
    assert baseline != derive_seed(b"salt", "ten_a", "other", "uniform-random", 1, "subject")
    assert baseline != derive_seed(b"salt", "ten_a", "placement", "ab-split", 1, "subject")
    assert baseline != derive_seed(b"salt", "ten_a", "placement", "uniform-random", 2, "subject")
    assert baseline != derive_seed(b"salt", "ten_a", "placement", "uniform-random", 1, "other")
    assert baseline != derive_seed(b"rotated", "ten_a", "placement", "uniform-random", 1, "subject")


def test_field_boundaries_cannot_be_forged_by_concatenation() -> None:
    # Without a separator, ("ab", "c") and ("a", "bc") would hash identically and
    # two different tenants could share an assignment.
    assert derive_seed(b"salt", "ab", "c", "uniform-random", 1, "s") != derive_seed(
        b"salt", "a", "bc", "uniform-random", 1, "s"
    )


def test_requires_a_subject_or_decision_identifier() -> None:
    with pytest.raises(ValueError):
        derive_seed(b"salt", "ten_a", "placement", "uniform-random", 1, "")
