# Task 003 — Foundation remediation

- Date: 2026-07-22
- Branch: `feature/001-foundation`
- Implementation commits: `da9e99e`, `fd7dc89`
- Review source: `docs/reviews/001-foundation-review.md`
- Scope: all findings FND-001 through FND-011

## Outcome

All BLOCKER, HIGH, MEDIUM and LOW findings were corrected locally. FND-011 is
closed only when GitHub Actions completes on the final pushed commit; the final
run URL and conclusion are recorded below after push.

## Finding disposition

| Finding | Severity | Status | Remediation and objective evidence |
| --- | --- | --- | --- |
| FND-001 | BLOCKER | Resolved | Request DTOs capture unmapped members and boundary validation returns `422 unknown_field` with JSON Pointers. Contract tests cover decision root, nested alternative, event root, and every applicable published invalid request example. |
| FND-002 | HIGH | Resolved | Added tenant-scoped 24-hour decision idempotency with canonical SHA-256 request fingerprints and per-key serialization. Same key/body replays the complete original response; changed body returns `409 idempotency-key-reused`; other tenants are independent. Tests cover sequential, conflict, tenant, 16 concurrent retries and expiry. The adapter is explicitly development-only/in-memory. |
| FND-003 | HIGH | Resolved | Added `IContextKeyPolicy` and configured tenant-specific allow-lists. The privacy-safe base keys are shared; two reference tenants have different extensions. Syntactically valid undeclared `email` is rejected, and cross-tenant key tests prove isolation. |
| FND-004 | MEDIUM | Resolved | The executable OpenAPI no longer advertises deferred `403`, `404` or `429` behavior, and its key description explicitly identifies the development adapter. OpenAPI tests assert the exact status sets. Architecture docs distinguish target lifecycle from executable foundation. Origin, placement configuration and rate limiting remain explicit roadmap work, not current API claims. |
| FND-005 | MEDIUM | Resolved | Event `properties` are measured after canonical JSON serialization in UTF-8 and capped at 4,096 bytes. Tests accept exactly 4,096, reject 4,097 and reject a multibyte payload whose UTF-16 character count would understate its byte size. |
| FND-006 | MEDIUM | Resolved | The hosted API receives all applicable invalid request examples. Runtime decision, event and problem responses are evaluated against the published JSON Schemas with external common-schema references and format assertions. Exact OpenAPI status tests cover executable operations. Removing an output field, accepting an unknown request field or changing a documented status now fails contract tests. |
| FND-007 | MEDIUM | Resolved | Added shared `uniform-random-policy-vectors.json` with ordered alternatives, seed, expected selection and propensity. C# and Python consume the same vectors; changes to modulo, order or propensity fail at least one side. |
| FND-008 | MEDIUM | Resolved | Exact pins: .NET SDK 10.0.302 with roll-forward disabled, ASP.NET Core 10.0.10, Node 22.22.3, pnpm 11.6.0 and Python 3.13.14. Python direct/transitive test dependencies are exact in `requirements-dev.lock`; pytest is 9.1.1. ADR-0013 records current support phases and upgrade policy. Clean Python 3.13.14 and pnpm installs passed; NuGet, npm and Python audits reported no known vulnerabilities. |
| FND-009 | MEDIUM | Resolved | Added OpenTelemetry logging, one structured bounded-cardinality request event, and an in-memory exporter test for correlation/tenant/route/status/duration. `adex.cache.degraded` increments on the executable Redis-unavailable readiness path and a meter-listener test checks the count/tags. ADR-0014 corrects the metric semantics: it measures optional-dependency degradation, not a nonexistent PostgreSQL fallback. |
| FND-010 | LOW | Resolved | Redis health details now say ADEX continues without cache acceleration. Integration tests prohibit a PostgreSQL claim in both unreachable and unconfigured Redis results. |
| FND-011 | NOTE | Pending final CI record | The temporal minimum-release-age policy was not weakened. A clean frozen pnpm install passes. Final GitHub Actions run is recorded after push. |

## Validation evidence

Executed from `C:\Proyectos\ADEX` unless noted:

- .NET SDK `10.0.302` restore/build: success, zero warnings and zero errors.
- Unit tests: 73 passed.
- Contract tests: 53 passed.
- Integration tests without opt-in: 5 passed, 5 explicitly skipped.
- Integration tests with `ADEX_INTEGRATION=1`: 10 passed against healthy
  PostgreSQL and Redis containers.
- `pnpm install --frozen-lockfile`: success.
- `pnpm run verify`: 67 tests passed plus lint, type-check, builds and domain
  neutrality.
- `pnpm run format`: success.
- Clean `python:3.13.14-slim`: locked install, Ruff lint/format and 33 tests
  passed.
- `pip-audit -r requirements-dev.lock`: no known vulnerabilities.
- `dotnet list ... --vulnerable --include-transitive`: no vulnerable packages.
- `pnpm audit --audit-level high`: no known vulnerabilities.
- `git diff --check`: clean.

## Architecture decisions

- ADR-0013 supersedes ADR-0002 for exact, reproducible toolchains and the
  controlled dependency-upgrade process.
- ADR-0014 supersedes ADR-0011 to make foundation logging and cache-degradation
  semantics truthful and executable.
- No deferred production capability was disguised as complete: PostgreSQL
  persistence/RLS, origin enforcement, placement configuration and rate
  limiting remain roadmap items.

## Final CI

- Commit: pending final push
- Run: pending final push
- Conclusion: pending final push

## Residual risks

- Decision idempotency and context configuration use development-only,
  process-local adapters until the PostgreSQL task implements durable,
  multi-instance semantics.
- `adex.cache.degraded` currently measures readiness observations. There is no
  honest per-request cache-bypass count until a real read-through adapter
  exists.
- Product capabilities already deferred by the foundation review remain
  deferred; none is presented as remediated by this task.
