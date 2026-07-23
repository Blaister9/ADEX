-- ---------------------------------------------------------------------------
-- 0001_bootstrap
--
-- Creates the ADEX schema and the migration ledger. Nothing else.
--
-- Entity tables (tenants, api keys, placements, alternatives, policy bindings,
-- decisions, events, rewards) are deliberately NOT created here. They are
-- designed together with the persistence adapters in roadmap task 004, where
-- `tenant_id` on every table, composite keys and Row Level Security are
-- implemented and tested as one piece. Inventing a schema now would produce
-- tables no code has ever validated.
--
-- Migrations are SQL-first and forward-only (ADR-0010). A correction is a new
-- numbered file, never an edit to this one.
-- ---------------------------------------------------------------------------

BEGIN;

CREATE SCHEMA IF NOT EXISTS adex;

COMMENT ON SCHEMA adex IS
    'ADEX application schema. Every tenant-owned table in here carries tenant_id, '
    'composite keys and a Row Level Security policy (ADR-0007).';

CREATE TABLE IF NOT EXISTS adex.schema_migrations (
    version     text        NOT NULL PRIMARY KEY,
    applied_at  timestamptz NOT NULL DEFAULT now(),
    checksum    text        NOT NULL
);

COMMENT ON TABLE adex.schema_migrations IS
    'Applied migrations, newest last. Forward-only: rollbacks ship as new migrations.';

-- The checksum is recorded so a later runner can detect an edited migration.
-- This bootstrap file is applied by the container entrypoint rather than by a
-- runner, so its checksum is stated rather than computed.
INSERT INTO adex.schema_migrations (version, checksum)
VALUES ('0001_bootstrap', 'applied-by-docker-entrypoint-initdb')
ON CONFLICT (version) DO NOTHING;

-- ADEX stores no local times. Setting this at the database level means a
-- misconfigured client session cannot silently reinterpret a stored instant.
DO $$
BEGIN
    EXECUTE format('ALTER DATABASE %I SET timezone TO ''UTC''', current_database());
END
$$;

COMMIT;
