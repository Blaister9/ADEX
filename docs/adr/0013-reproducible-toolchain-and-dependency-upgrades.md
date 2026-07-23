# ADR-0013 — Reproducible toolchain and dependency upgrades

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation remediation agent (task 003)

## Context

ADR-0002 selected supported major lines, but several pins still allowed two
clean clones to resolve different toolchains or Python dependencies. Its support
phase descriptions also became stale: Node 22 is Maintenance LTS and Python
3.12 is security-only. Microsoft requires the current .NET patch for support.

## Decision

| Component | Exact version | Support rationale on 2026-07-22 |
| --- | --- | --- |
| .NET SDK / runtime packages | SDK `10.0.302`; ASP.NET Core `10.0.10` | .NET 10 is active LTS through 2028-11-14; `10.0.10` is the current security patch. |
| Node.js | `22.22.3` | Maintenance LTS through 2027-04-30. Retained for ecosystem stability; ADEX needs no Node 24 feature. |
| pnpm | `11.6.0` | Exact package-manager version recorded in `packageManager` and the engine check. |
| Python | `3.13.14` | Current maintained 3.13 bugfix line with binary installation support; replaces security-only 3.12. |
| Python test tools | pytest `9.1.1`, Ruff `0.15.22` and exact transitives | Recorded in `requirements-dev.lock`; the simulator has no runtime dependencies. |

`global.json` disables roll-forward. `.nvmrc`, package engines and
`.python-version` pin exact interpreters. CI reads those files rather than
duplicating versions. Python CI installs the lock, not the optional dependency
range.

Upgrades are monthly, and immediately for a relevant security advisory. An
upgrade changes the pin and lock in one review, runs clean installs plus all
tests, and refreshes this support table through a superseding ADR when the
selected major line or rationale changes.

## Alternatives considered

- **Floating patches and broad dependency ranges.** Rejected because a green
  commit could fail later without a repository diff.
- **Move Node to the newest LTS solely because it is newer.** Rejected; the
  current maintenance line remains supported and no product requirement needs
  the newer runtime.
- **Keep Python 3.12.** Rejected because it is security-only and current
  security releases no longer ship normal Windows installers.

## Consequences

- A contributor may need to install the exact patch before the repository runs.
- Security upgrades produce explicit repository diffs instead of silently
  changing CI.
- Python dependency audit results are attributable to the committed lock.
