"""Deterministic decision-seed derivation.

This is a deliberate second implementation of ``Adex.Domain.Policies.DecisionSeed``.
The two languages must agree exactly, because that is what lets a policy proven
offline in Python be reimplemented in C# and validated against shared fixtures
rather than against a description of the algorithm (ADR-0003).

The shared vectors live in ``tests/fixtures/decision-seed-vectors.json`` at the
repository root and are asserted from both sides.
"""

from __future__ import annotations

import hashlib
import hmac

#: Unit separator between fields, so two different field splits can never
#: produce identical material and therefore an identical assignment.
FIELD_SEPARATOR = "\x1f"


def derive_seed(
    salt: bytes,
    tenant_id: str,
    placement: str,
    policy_key: str,
    policy_version: int,
    subject_or_decision_id: str,
) -> int:
    """Return the 32-bit seed a policy draws from.

    Mirrors the C# implementation byte for byte: HMAC-SHA256 over the
    separator-joined fields, then the first four bytes read little-endian.
    """
    if not subject_or_decision_id:
        raise ValueError("A subject or decision identifier is required to derive a seed.")

    material = FIELD_SEPARATOR.join(
        [tenant_id, placement, policy_key, str(policy_version), subject_or_decision_id]
    )
    digest = hmac.new(salt, material.encode("utf-8"), hashlib.sha256).digest()
    return int.from_bytes(digest[:4], byteorder="little", signed=False)
