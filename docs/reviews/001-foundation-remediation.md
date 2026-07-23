# Foundation remediation 001

## Estado inicial

Task 003 remediated the adversarial foundation review on the existing
`feature/001-foundation` branch. The initial tree was clean, the branch contained
the review commit, and `main` and `dev` both remained at `cc586fa`.

## Commit de revisión utilizado

`deaf78a1227b4b0df96c7ed04adb6d636176cb02`

## Resumen de correcciones

All eleven findings were reproduced or directly verified. The API now rejects
unknown request members, enforces tenant-scoped context keys and 24-hour
decision idempotency, measures event properties as serialized UTF-8, and
publishes only executable response statuses. Hosted contract tests exercise
published invalid examples and runtime JSON Schema responses. C# and Python
share deterministic policy vectors. Toolchains and Python dependencies are
exactly pinned. OpenTelemetry logging and cache-degradation semantics are
executable and tested. Redis readiness text is neutral. The supply-chain age
failure was resolved without weakening the policy by constraining Vitest's
optional `happy-dom` peer to a mature release.

## Matriz de resolución

| ID | Severidad | Estado | Causa raíz | Archivos modificados | Prueba añadida o ejecutada | Criterio de cierre | Evidencia |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FND-001 | BLOCKER | RESOLVED | ASP.NET deserialization silently discarded unmapped JSON members. | `PublicContracts.cs`, `RequestValidation.cs`, decision/event contract tests | Hosted invalid examples and root/nested unknown-field tests | Every published invalid example returns the documented `422` error. | 53 contract tests passed; CI 29978450774 green. |
| FND-002 | HIGH | RESOLVED | Decision retry semantics had no idempotency port or key/body conflict handling. | `IDecisionIdempotencyStore.cs`, `InMemoryDecisionIdempotencyStore.cs`, `RequestDecision.cs`, `DecisionEndpoints.cs` | Replay, conflict, tenant isolation, 16-way concurrency and expiry tests | Same tenant/key/body replays; changed body is `409`; scope and 24-hour expiry are explicit. | 73 unit and 53 contract tests passed. |
| FND-003 | HIGH | RESOLVED | Context validation used one hard-coded global list. | `IContextKeyPolicy.cs`, `ConfiguredContextKeyPolicy.cs`, development configuration | Two-tenant allow-list and undeclared-email rejection tests | Allowed context is tenant-specific and undeclared keys are rejected. | Cross-tenant contract tests passed. |
| FND-004 | MEDIUM | RESOLVED | OpenAPI advertised deferred `403`, `404` and `429` behavior. | `adex-public-v1.yaml`, contract package/readmes, architecture docs | Exact operation response-set tests | The executable contract contains only implemented statuses and labels development adapters. | OpenAPI tests and 67 TypeScript tests passed. |
| FND-005 | MEDIUM | RESOLVED | The event limit used an imprecise representation instead of serialized UTF-8 bytes. | `RequestValidation.cs`, `EventContractTests.cs` | Exact 4096/4097-byte and multibyte tests | Exactly 4096 bytes passes; any larger serialized object fails. | Hosted contract tests passed. |
| FND-006 | MEDIUM | RESOLVED | Static schema checks did not prove runtime request/response conformance. | `PublishedInvalidExampleTests.cs`, `RuntimeResponseSchemaTests.cs`, contract test dependencies | Hosted invalid examples and JSON Schema response evaluation | Runtime decision, event and problem bodies validate and documented failures are executable. | 53 contract tests passed. |
| FND-007 | MEDIUM | RESOLVED | C# and Python policy implementations had no shared behavioral oracle. | `uniform-random-policy-vectors.json`, `CrossLanguagePolicyTests.cs`, `test_policy_vectors.py` | Shared seed/selection/propensity vectors in both runtimes | Both implementations consume the same deterministic vectors. | 73 unit and 33 Python tests passed; CI simulator job green. |
| FND-008 | MEDIUM | RESOLVED | Floating toolchains and Python dependency ranges prevented exact reproduction. | `global.json`, `.nvmrc`, `.python-version`, `package.json`, `requirements-dev.lock`, CI, ADR-0013 | Clean exact-toolchain installs and three ecosystem audits | SDKs and direct/transitive Python test dependencies are exact; security upgrade policy is recorded. | .NET 10.0.302, Node 22.22.3, pnpm 11.6.0 and Python 3.13.14 passed; no known vulnerabilities. |
| FND-009 | MEDIUM | RESOLVED | Observability claims exceeded executable signals and described a nonexistent fallback. | request logging/telemetry files, `ObservabilityContractTests.cs`, ADR-0014 | In-memory log exporter and meter-listener tests | Structured request log fields and truthful cache-degradation count/tags are tested. | Contract observability tests and CI passed. |
| FND-010 | LOW | RESOLVED | Redis health detail falsely implied PostgreSQL fallback. | `RedisDependencyProbe.cs`, `DependencyProbeTests.cs` | Unconfigured and unreachable Redis integration assertions | Detail states operation continues without cache acceleration and makes no fallback claim. | 10 container-backed integration tests passed. |
| FND-011 | NOTE | RESOLVED | Vitest auto-resolved newly published `happy-dom 20.11.1`, which violated pnpm's minimum-release-age policy in a fresh CI environment; the first local store had a cached policy result. | `pnpm-workspace.yaml`, `pnpm-lock.yaml` | Fresh-store frozen install, full Node verification and GitHub Actions | The age control stays enabled and a mature compatible peer resolution produces a reproducible frozen install. | Clean clone at `23b40b9` downloaded all 213 packages and passed; CI 29978450774 green. |

## Validaciones locales

- .NET 10.0.302 Release build: 0 warnings, 0 errors.
- Unit tests: 73 passed.
- Contract tests: 53 passed.
- Integration without opt-in: 5 passed, 5 explicitly skipped.
- Integration with PostgreSQL and Redis: 10 passed.
- TypeScript: lint, format, type-check and builds passed; 67 tests passed.
- Python 3.13.14: Ruff lint/format passed; 33 tests passed.
- Domain-neutrality check and `docker compose config` passed.
- PostgreSQL/Redis startup, clean migration, health, missing-Redis,
  tenant-isolation, idempotency, invalid-payload and missing-credential paths
  passed through the local and CI suites.
- Gitleaks passed.
- NuGet, npm and PyPI audits reported no known vulnerabilities.
- `git diff --check` passed.

## Validación desde entorno limpio

The pushed commit `23b40b960cd5d864eaf8e2e5532685ad9c5d2f83` was cloned to
`C:\Users\santi\AppData\Local\Temp\adex-remediation-clean-23b40b9`. With Node
22.22.3 and pnpm 11.6.0, `pnpm install --frozen-lockfile` used a new store,
downloaded all 213 packages, passed the release-age policy, and `pnpm verify`
passed all 67 tests plus lint, type-check, builds and domain neutrality. An
earlier clean clone also passed the complete .NET and Python exact-toolchain
suites; GitHub Actions independently repeated every workflow job from a clean
runner.

## Resultado de GitHub Actions

- Commit: `23b40b960cd5d864eaf8e2e5532685ad9c5d2f83`
- Run: `https://github.com/Blaister9/ADEX/actions/runs/29978450774`
- Conclusion: `success`
- Jobs: .NET, TypeScript, simulator, dependency report, infrastructure and
  secret scan all passed.

## Regresiones encontradas durante la corrección

- The first clean Python run found one Ruff formatting violation in the new
  policy-vector test; formatting and the repeated clean run passed.
- CI run `29978287206` exposed `happy-dom 20.11.1` as newer than the minimum
  release age. Moving the specific mature-version constraint to
  `pnpm-workspace.yaml` regenerated the lock without weakening the policy.

## Riesgos residuales

- Decision idempotency and context configuration are explicitly
  development-only and process-local until durable PostgreSQL adapters exist.
- `adex.cache.degraded` measures dependency observations, not decision volume,
  until a real read-through cache exists.
- GitHub reports non-blocking Node 20 deprecation annotations for
  `actions/checkout@v4` (and other current action majors). Updating action
  majors is a maintenance task; all jobs currently execute successfully.
- Origin enforcement, durable placement configuration and rate limiting remain
  planned production capabilities and are not represented as implemented.

## Recomendación final

READY FOR FINAL VALIDATION
