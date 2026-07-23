# Current handoff

## Task

Task 003 — remediate every finding in
`docs/reviews/001-foundation-review.md` on the existing
`feature/001-foundation` branch.

## Branch and commit

- Branch: `feature/001-foundation`
- Reviewed baseline: `deaf78a1227b4b0df96c7ed04adb6d636176cb02`
- Remediation commits:
  - `da9e99e` — executable API contract, idempotency, allow-lists, size limits,
    observability and contract tests
  - `fd7dc89` — exact toolchains, shared policy vectors and ADR updates
  - documentation closeout — the commit containing this handoff
- Remote: `origin/feature/001-foundation`

## Objective completed

FND-001 through FND-010 are implemented and locally verified. FND-011 requires
the final GitHub Actions run to be green; its run URL is added after the final
push. Detailed per-finding evidence is durable in
`docs/reviews/001-foundation-remediation.md`.

The executable foundation now enforces closed request shapes, tenant-specific
context allow-lists, 24-hour tenant-scoped decision retry idempotency, and the
4-KiB UTF-8 event-properties limit. Runtime request examples and response
schemas are exercised by the hosted API. C# and Python share policy behavior
vectors. All three OpenTelemetry signals have executable tests. Toolchains and
Python dependencies are exactly reproducible.

## Files changed

- API/application/infrastructure: strict request validation, idempotency port
  and development store, context policy, structured request logging, Redis
  degradation metric and neutral readiness detail.
- Contract/tests: executable status set, runtime invalid examples, JSON Schema
  response evaluation, idempotency/isolation/concurrency/size/OTel tests.
- Simulation/tooling: shared policy fixture, Python consumer, exact toolchain
  pins, Python lock and CI install/audit changes.
- Documentation: ADR-0013, ADR-0014, lifecycle/quality/readmes, remediation
  report and this handoff.

Use `git show --stat` on the remediation commits for the authoritative file
list.

## Architecture decisions

- ADR-0013 supersedes ADR-0002: exact .NET/Node/pnpm/Python pins, locked Python
  dependencies and a controlled security upgrade process.
- ADR-0014 supersedes ADR-0011: OpenTelemetry structured request logs are real;
  foundation `adex.cache.degraded` measures optional Redis degradation without
  falsely claiming a PostgreSQL fallback.
- OpenAPI describes the executable foundation. Deferred origin, placement and
  rate-limit responses were removed rather than faked.
- Idempotency/context adapters remain explicitly development-only until durable
  PostgreSQL configuration/persistence lands.

## Commands executed

- Mandatory Git inspection, fetch, checkout, pull and `dev...feature` diff.
- Exact .NET 10.0.302 restore/build/test commands.
- `pnpm install --frozen-lockfile`, `pnpm run verify`, `pnpm run format`.
- Clean Python 3.13.14 container install, Ruff and pytest.
- NuGet/npm/Python vulnerability audits.
- Docker-backed integration tests with `ADEX_INTEGRATION=1`.
- `git diff --check` and targeted diff review.
- Final clean-clone and GitHub Actions commands: pending final commit/push.

## Tests and results

- .NET build: success, 0 warnings, 0 errors.
- Unit: 73 passed.
- Contract: 53 passed.
- Integration without opt-in: 5 passed, 5 skipped explicitly.
- Integration with real dependencies: 10 passed.
- TypeScript: 67 passed; lint, type-check, build, format and neutrality passed.
- Python 3.13.14 clean container: 33 passed; Ruff lint/format passed.
- NuGet/npm/Python dependency audits: no known vulnerabilities.
- Final GitHub Actions run: pending final push.

## Known failures

None in the completed local validation. The first local Python clean check found
one formatting violation in the new policy-vector test; it was formatted and
the repeated exact-toolchain check passed.

## Risks and unresolved questions

- FND-011 is not closed until the final GitHub Actions run is green.
- In-memory idempotency is neither durable nor shared across processes. The
  PostgreSQL adapter must enforce `(tenant_id, idempotency_key)` uniqueness and
  the 24-hour retention policy.
- Context allow-lists are tenant-scoped development configuration; placement
  scoping/versioning belongs with the durable placement configuration task.
- Redis is not yet a read-through application cache. The current degraded
  metric measures dependency observations, not affected decision volume.

## Recommended next task

After final CI is green, merge readiness can be reassessed. The next product
task should implement the durable PostgreSQL vertical slice and RLS without
regressing the executable contract controls added here.

## Paths the next agent should read first

1. `AGENTS.md`
2. `docs/reviews/001-foundation-remediation.md`
3. `docs/reviews/001-foundation-review.md`
4. `docs/adr/0013-reproducible-toolchain-and-dependency-upgrades.md`
5. `docs/adr/0014-foundation-observability-semantics.md`
6. `src/Adex.Application/Decisions/RequestDecision.cs`
7. `src/Adex.Api/Validation/RequestValidation.cs`
8. `tests/contract/Adex.ContractTests`
