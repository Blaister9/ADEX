# Local infrastructure

Everything needed to run ADEX on a developer machine. **Development only** — see
the header of `docker-compose.yml` for what this deliberately does not provide.

## Start and stop

```bash
cp .env.example .env
docker compose up -d
docker compose ps
docker compose down
```

`docker compose down -v` also drops the volume, which is the supported way to
reset the database: migrations are forward-only, so there is no down script
(ADR-0010).

## Checks that do not need the containers running

```bash
docker compose config --quiet
```

Validates the compose file, variable interpolation included. It requires a
`.env` (or the variables exported) because `POSTGRES_PASSWORD` has no default —
a compose file that ships a working password teaches everyone the password.

## Migrations

`migrations/NNNN_snake_case_description.sql`, applied in lexical order.

Locally they are applied by the PostgreSQL container's
`docker-entrypoint-initdb.d` mount, which runs **only on an empty data
directory**. Adding a migration therefore means either `docker compose down -v`
locally, or applying it by hand:

```bash
docker compose exec -T postgres psql -U adex -d adex < infra/local/migrations/0002_....sql
```

A real runner — executed by the API behind a flag, or as a CLI verb — arrives
with roadmap task 004, when there is more than one migration to order. The
`adex.schema_migrations` ledger and the file naming are already compatible with
adopting DbUp or Flyway later.

### Migration checklist

Every migration that adds a tenant-owned table must include, in the same file:

1. a non-null `tenant_id` column,
2. a primary key and every unique constraint composite on `tenant_id`,
3. composite foreign keys, so a child row cannot point at another tenant's parent,
4. `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` **and** `FORCE ROW LEVEL SECURITY`,
5. a policy comparing `tenant_id` to the `app.tenant_id` session setting,
6. grants for the application role, which is not the table owner.

Items 4–6 are what make a forgotten `WHERE tenant_id = ...` return zero rows
instead of another tenant's data (ADR-0007).

## Ports

| Service | Host port | Why not the default |
| --- | --- | --- |
| PostgreSQL | `127.0.0.1:55432` | So a locally installed PostgreSQL keeps `5432` |
| Redis | `127.0.0.1:56379` | Same reasoning for `6379` |

Both bind to the loopback interface: nothing here is reachable from the network.

## Deployment

`infra/deploy/` is intentionally empty in this phase. Nothing about a target
environment has been decided, and a plausible-looking deployment manifest for an
undecided platform is worse than none.
