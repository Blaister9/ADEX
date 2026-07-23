# Architecture Decision Records

Every architectural decision that constrains future work is recorded here as a
numbered, immutable document. Superseding a decision means writing a new ADR and
marking the old one `Superseded by ADR-XXXX` — never editing history in place.

## Format

```markdown
# ADR-XXXX — Title

- Status: Proposed | Accepted | Superseded by ADR-YYYY | Rejected
- Date: YYYY-MM-DD
- Deciders: <role or agent>

## Context
## Decision
## Alternatives considered
## Consequences
```

## Index

| ADR | Title | Status |
| --- | --- | --- |
| [0001](0001-modular-monolith-over-microservices.md) | Modular monolith over microservices | Accepted |
| [0002](0002-toolchain-and-runtime-baseline.md) | Toolchain and runtime baseline | Accepted |
| [0003](0003-language-boundaries.md) | Language boundaries: C# online path, TypeScript SDK, Python simulation | Accepted |
| [0004](0004-postgresql-system-of-record-redis-optional.md) | PostgreSQL as system of record, Redis as optional acceleration | Accepted |
| [0005](0005-monorepo-organization.md) | Monorepo organization | Accepted |
| [0006](0006-api-contract-and-versioning.md) | Contract-first API and versioning strategy | Accepted |
| [0007](0007-tenant-isolation-strategy.md) | Tenant isolation strategy | Accepted |
| [0008](0008-anonymous-identity-and-privacy-defaults.md) | Anonymous identity and privacy defaults | Accepted |
| [0009](0009-initial-policy-sequence.md) | Initial policy sequence | Accepted |
| [0010](0010-database-migration-mechanism.md) | Database migration mechanism | Accepted |
| [0011](0011-observability-baseline.md) | Observability baseline | Accepted |
| [0012](0012-idempotency-and-deduplication.md) | Idempotency and event deduplication | Accepted |
