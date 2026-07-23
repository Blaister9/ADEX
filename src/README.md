# ADEX backend (.NET)

The authoritative online path: everything a client can observe about a decision
is produced here (ADR-0003). One deployable, four projects, dependencies
pointing inwards.

| Project | Depends on | Contents |
| --- | --- | --- |
| `Adex.Domain` | nothing (BCL only) | Identifiers, placement/alternative grammar, decision context, immutable decision and event records, the pure policy abstraction and `uniform-random`, deterministic seed derivation. |
| `Adex.Application` | Domain | Ports (`IClock`, `IDecisionStore`, `IEventStore`, `IPolicyResolver`, `ITenantDirectory`, `ISeedSaltProvider`, `IDependencyProbe`) and the two use cases. |
| `Adex.Infrastructure` | Application, Domain | Adapters: ULID generation, dependency probes for PostgreSQL and Redis, the development in-memory stores, configuration-backed tenancy, DI wiring. |
| `Adex.Api` | all of the above | Composition root: minimal-API endpoints, tenant and correlation handling, validation, problem+json, OpenTelemetry. |

`Adex.Domain` has **no NuGet dependency at all**. That is a deliberate,
checkable property; a pull request that adds one has to explain why.

## Running locally

```bash
docker compose up -d
dotnet run --project src/Adex.Api
```

The API listens on `http://localhost:5080`. `GET /health/ready` reports each
dependency and whether it is required.

## What is real and what is not

Real: the decision path end to end in process, uniform-random selection, seed
determinism and replayability, the full decision audit record, idempotent event
ingestion, cross-tenant refusal, clock-skew rejection, health probes,
OpenTelemetry wiring, problem+json errors.

Not real yet, and named so in the code rather than dressed up:

- `Persistence/InMemory/*` — development-only stores. Nothing survives a
  restart. Selecting `Adex:Persistence:Provider=Postgres` fails fast with a
  pointer to roadmap task 004 rather than silently falling back.
- `Policies/UniformRandomPolicyResolver` — resolves every placement to
  `uniform-random` v1 because there is no configuration store yet.
- `Tenancy/ConfiguredTenantDirectory` — API keys from configuration, with no
  hashing, origin allow-list, rotation or rate limiting.

The API refuses to start with any of these outside the Development environment.

## Conventions

- All timestamps are UTC; non-UTC input is rejected, never converted.
- No database entity is serialized to a client; DTOs in `Adex.Api/Contracts`
  are the only wire types.
- Every use case takes its tenant explicitly. There is no ambient current tenant.
- Warnings are errors (`Directory.Build.props`).
