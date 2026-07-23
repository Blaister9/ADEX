# Current handoff

## Task

Task 003 — remediate all FND-001 through FND-011 findings from
`docs/reviews/001-foundation-review.md` on the existing foundation branch.

## Branch and commit

- Branch: `feature/001-foundation`
- Review baseline: `deaf78a1227b4b0df96c7ed04adb6d636176cb02`
- Remediation commits:
  - `da9e99e` — executable contract, idempotency, allow-lists, limits and
    observability
  - `fd7dc89` — exact toolchains, shared policy vectors and ADRs
  - `31f2ed2` — initial remediation evidence and handoff
  - `23b40b9` — mature optional DOM peer and reproducible pnpm lock
  - documentation closeout — the commit containing this handoff
- Remote: `origin/feature/001-foundation`

## Objective completed

All eleven findings are `RESOLVED`: 1 BLOCKER, 2 HIGH, 6 MEDIUM, 1 LOW and
1 NOTE. Detailed causes, tests and closure evidence are in
`docs/reviews/001-foundation-remediation.md`. No finding is pending, deferred,
accepted as risk or externally blocked.

## Files changed

- API/application/infrastructure: strict request validation, tenant-scoped
  idempotency and context policy, truthful Redis readiness and OpenTelemetry
  signals.
- Contracts/tests: executable OpenAPI statuses, hosted invalid examples,
  runtime JSON Schema validation and isolation/concurrency/boundary tests.
- Tooling/simulation: exact runtimes and Python lock, shared policy vectors,
  mature optional Vitest peer and CI audit alignment.
- Documentation: ADR-0013, ADR-0014, architecture/readmes, remediation report
  and this handoff.

Use `git show --stat da9e99e fd7dc89 23b40b9` for the authoritative product and
tooling file list.

## Architecture decisions

- ADR-0013 supersedes ADR-0002 with exact toolchains and a controlled
  dependency-upgrade process.
- ADR-0014 supersedes ADR-0011 with executable structured logs and neutral
  cache-degradation semantics.
- OpenAPI describes the executable foundation; deferred production behavior is
  not advertised.
- Idempotency and context adapters are explicitly development-only pending the
  PostgreSQL vertical slice.

## Commands executed

- Mandatory Git status/remote/branch/log/fetch/checkout/pull and
  `dev...feature` inspection.
- Exact .NET restore, Release build, unit, contract and container-backed
  integration tests.
- pnpm frozen installs, lint, format, type-check, build, tests, neutrality and
  audit.
- Python 3.13.14 locked install, Ruff, pytest and shared-vector test.
- Docker Compose configuration, clean migration, health and Redis degradation.
- NuGet/npm/Python advisories, Gitleaks, clean-clone checks and
  `git diff --check`.
- `gh run watch 29978450774 --exit-status`.

## Tests and results

- .NET Release build: success, 0 warnings and 0 errors.
- Unit: 73 passed.
- Contract: 53 passed.
- Integration without opt-in: 5 passed, 5 skipped explicitly.
- Integration with real PostgreSQL and Redis: 10 passed.
- TypeScript: 67 passed; lint, format, type-check, builds and neutrality passed.
- Python 3.13.14: 33 passed; Ruff lint/format passed.
- Clean clone at `23b40b9`: fresh-store frozen pnpm install downloaded all 213
  packages and full Node verification passed.
- NuGet/npm/Python dependency audits: no known vulnerabilities.
- GitHub Actions run 29978450774: `success`; every job passed.

## Known failures

No current failure. Historical regressions were corrected and revalidated:

- Ruff initially found one formatting violation in the shared-vector test.
- CI run 29978287206 rejected newly published `happy-dom 20.11.1`; the specific
  mature-peer constraint fixed the clean resolution while preserving the
  minimum-release-age policy.

## Risks and unresolved questions

- In-memory idempotency is not durable or shared across processes.
- Context allow-lists are tenant-scoped development configuration; durable
  placement scoping/versioning remains future work.
- Redis is not yet a read-through application cache.
- GitHub emits non-blocking Node 20 deprecation annotations for current action
  majors; schedule their maintenance upgrade.

## Recommended next task

Perform final human validation of Task 003 and, if accepted, integrate
`feature/001-foundation` into `dev` through the repository's protected review
process. After integration, begin the separately scoped durable PostgreSQL
vertical slice and RLS task.

## Paths the next agent should read first

1. `AGENTS.md`
2. `docs/reviews/001-foundation-remediation.md`
3. `docs/reviews/001-foundation-review.md`
4. `docs/adr/0013-reproducible-toolchain-and-dependency-upgrades.md`
5. `docs/adr/0014-foundation-observability-semantics.md`
6. `src/Adex.Application/Decisions/RequestDecision.cs`
7. `src/Adex.Api/Validation/RequestValidation.cs`
8. `tests/contract/Adex.ContractTests`
