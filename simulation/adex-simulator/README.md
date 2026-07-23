# adex-simulator

Offline synthetic environments and policy evaluation for ADEX.

**Never in the online request path.** Production decisions are made by the C#
service; Python is reserved for simulation, offline experimentation and
evaluation (ADR-0003). This package reads exported data and writes reports. It
never writes production state.

**Every result it produces is synthetic** and carries a banner saying so. Known
conversion rates make regret measurable and "the policy converged" a checkable
claim — but they describe a simulation, never a real audience, and must never be
presented as commercial uplift (`AGENTS.md` invariant 10).

## Setup

Zero runtime dependencies; only the test and lint tooling needs installing.

```bash
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements-dev.lock   # Windows
# .venv/bin/python -m pip install -r requirements-dev.lock     # macOS / Linux
```

## Commands

```bash
python -m pytest
python -m ruff check .
python -m ruff format --check .
python -m adex_simulator --list
python -m adex_simulator --scenario services-clear-winner --rounds 20000 --seed 20260722
```

## What is here

| Module | Purpose |
| --- | --- |
| `seeds.py` | Deterministic decision-seed derivation, byte-identical to `Adex.Domain.Policies.DecisionSeed`. |
| `policies.py` | Offline reference implementation of `uniform-random`, pure and seeded. |
| `environment.py` | Bernoulli environments with known true rates, so regret is measurable. |
| `scenarios.py` | Named reproducible scenarios for two unrelated reference tenants. |
| `runner.py` | Runs a policy against a scenario and formats a labelled report. |

## The cross-language contract

`seeds.py` is a second implementation of the C# seed derivation, on purpose. The
two must agree exactly, because that is what makes promoting a policy from
research to production verifiable.

The agreement is enforced by shared vectors in
`tests/fixtures/decision-seed-vectors.json` at the repository root, asserted from
both sides:

- Python: `tests/test_seeds.py`
- C#: `tests/unit/Adex.UnitTests/CrossLanguageSeedTests.cs`

If either implementation drifts, one of the two suites fails. The fixtures, not
the prose, are the contract.

## What is deliberately absent

No Thompson Sampling, no contextual policies, no off-policy evaluation harness,
no plots. Those arrive with roadmap task 005, after the reward pipeline they
would learn from is verified — a bandit converging on a bug looks exactly like a
bandit working (ADR-0009).

No NumPy, SciPy or pandas yet either: nothing here needs them, and a dependency
that exists "for later" is a dependency nobody has justified. They arrive with
the evaluation harness that uses them.
