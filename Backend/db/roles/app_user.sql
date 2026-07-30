-- Least-privilege role used for the app's own connection string
-- (ConnectionStrings__AppDb). No SUPERUSER, no CREATE, no ownership of any
-- table - just USAGE plus CRUD on what migrations_user has created (or
-- will create). Run this after migrations_user.sql.
--
-- Usage:
--   psql "<superuser connection string>" \
--     -v app_password="'<password>'" \
--     -f db/roles/app_user.sql

-- See migrations_user.sql for why the password is threaded through a GUC
-- rather than referenced directly inside the DO block.
SET myvars.app_password = :'app_password';

DO $$
DECLARE
    password text := current_setting('myvars.app_password');
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'app_user') THEN
        EXECUTE format('CREATE ROLE app_user WITH LOGIN PASSWORD %L', password);
    END IF;
END
$$;

GRANT USAGE ON SCHEMA public TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_user;

-- So every future migration (run by migrations_user) automatically grants
-- app_user access to the tables/sequences it creates, without re-running
-- this script after every schema change.
ALTER DEFAULT PRIVILEGES FOR ROLE migrations_user IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_user;
ALTER DEFAULT PRIVILEGES FOR ROLE migrations_user IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO app_user;
