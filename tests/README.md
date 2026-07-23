# Tests

Split by kind rather than by production module, so a CI job maps one-to-one to a
directory and the suites that need infrastructure are separable from the ones
that do not (ADR-0005).

| Directory | Needs containers? | What it proves |
| --- | --- | --- |
| `unit/Adex.UnitTests` | No | Domain and application behaviour: identifier grammar, policy purity and determinism, seed derivation, decision audit content, replay, idempotency, clock-skew rules, tenant scoping, health aggregation. |
| `contract/Adex.ContractTests` | No | The running API matches `packages/contracts`: status codes, problem+json envelope, field-level error codes, correlation echo, `202`-on-replay, cross-tenant refusal, readiness semantics. Hosts the real `Program` through `WebApplicationFactory`. |
| `integration/Adex.IntegrationTests` | Optional | Dependency probes and the bootstrap migration against real PostgreSQL and Redis. Infrastructure-dependent tests **skip**, never silently pass, when the containers are absent. |
| `e2e` | Yes (task 004) | The full vertical path. Intentionally empty — see below. |

## Running

```bash
dotnet test Adex.slnx -c Release
```

Infrastructure-dependent tests additionally need:

```bash
docker compose up -d
ADEX_INTEGRATION=1 dotnet test tests/integration/Adex.IntegrationTests
```

TypeScript suites live with their packages and run from the repository root:

```bash
pnpm vitest run
```

## Why `e2e` is empty

The end-to-end path — reference page → decision → persisted decision → event →
attributed reward → analytics query — needs durable persistence, which does not
exist yet (the foundation runs on an explicitly named development in-memory
store). Writing an e2e suite against the in-memory store would assert that a
cache behaves like a cache.

`e2e` is filled by roadmap task 004, together with the PostgreSQL adapters. Its
first test is the reference integration page that must also render correctly
with the SDK disabled.
