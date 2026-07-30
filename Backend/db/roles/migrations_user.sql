-- Least-privilege role used exclusively for `dotnet ef database update` /
-- the migration step in CI - it owns the schema, so it's the only role
-- that ever runs DDL. Never used for the app's own connection string.
--
-- Run once per environment as a superuser (local docker-compose) or as the
-- project's default/owner role (Neon - Neon gives you an owner role by
-- default; either rename its usage to this purpose or create this role
-- explicitly and transfer ownership to it, per docs/database.md).
--
-- Usage:
--   psql "<superuser connection string>" \
--     -v migrations_password="'<password>'" \
--     -f db/roles/migrations_user.sql

-- psql does not substitute :'variables' inside a dollar-quoted ($$...$$)
-- PL/pgSQL body, so the password is passed in via a session GUC set in a
-- plain statement first, then read back inside the DO block.
SET myvars.migrations_password = :'migrations_password';

DO $$
DECLARE
    password text := current_setting('myvars.migrations_password');
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'migrations_user') THEN
        EXECUTE format('CREATE ROLE migrations_user WITH LOGIN PASSWORD %L', password);
    END IF;
END
$$;

-- migrations_user owns the schema - CREATE/ALTER/DROP TABLE only happens
-- through this role, never through app_user (see app_user.sql).
ALTER SCHEMA public OWNER TO migrations_user;

-- Nobody gets to CREATE in this schema except the schema owner.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

GRANT USAGE ON SCHEMA public TO migrations_user;
