# ADR-0010 — Database migration mechanism

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

The schema must carry `tenant_id` on every tenant-owned table, composite keys,
Row Level Security policies, partial and expression indexes, and eventually
partitioning for events (see ADR-0007). These are PostgreSQL features that
object-relational migration generators express poorly or not at all, and getting
them wrong is a tenant-isolation bug.

## Decision

**SQL-first, forward-only, versioned migrations.**

- Migrations are plain `.sql` files in `infra/local/migrations/`, named
  `NNNN_snake_case_description.sql`, applied in lexical order.
- A single table `adex.schema_migrations (version text primary key, applied_at
  timestamptz not null default now(), checksum text not null)` records what has
  been applied.
- Each migration file is idempotent where practical (`IF NOT EXISTS`) and is
  never edited after being applied outside a developer machine; corrections ship
  as a new file.
- Migrations are **forward-only**. A rollback is a new forward migration. Down
  scripts are not maintained, because a tested rollback of a data-bearing
  migration is a fiction more often than not.
- Local development applies migration `0001` automatically through the
  PostgreSQL container's `docker-entrypoint-initdb.d` mount; a real runner
  (executed by the API on start behind a flag, or a `dotnet` CLI verb) is
  introduced with the persistence task, when there is more than one migration to
  order.
- Data access uses raw SQL over Npgsql (with Dapper considered as a mapping
  convenience later). No object-relational mapper is adopted in the foundation.

The foundation ships exactly one migration: the `adex` schema, the
`schema_migrations` table, and the application role grants. **Entity tables are
deliberately not created here** — they are designed with the first vertical
slice, and inventing them now would produce a schema no code has validated.

## Alternatives considered

- **EF Core migrations.** Strong tooling and C#-native, rejected for now:
  RLS policies, `FORCE ROW LEVEL SECURITY`, composite tenant keys and partition
  attachment all end up as raw-SQL escape hatches anyway, so the model-first
  benefit largely evaporates while the runtime dependency and the query-shape
  opacity remain. Reconsider if the admin/configuration module grows enough CRUD
  surface to make it worthwhile — that would be its own ADR.
- **DbUp / Fluent Migrator / Flyway / Liquibase.** All workable. Deferred rather
  than rejected: with one migration file, adding a migration framework now would
  be ceremony. The chosen file naming and `schema_migrations` table are
  compatible with adopting DbUp or Flyway later.
- **Auto-apply on every API start, unconditionally.** Rejected: concurrent
  instances racing on DDL, and no separation between deploy and release.

## Consequences

- Reviewers read exactly the SQL that will run in production.
- Someone must own migration ordering discipline; CI gains a check that
  migration filenames are unique and monotonically numbered.
- Local `docker compose down -v` followed by `up` is the supported way to reset
  a development database.
