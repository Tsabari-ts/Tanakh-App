-- Runs automatically on first container start (docker-entrypoint-initdb.d).
-- citext backs subscribers.email (case-insensitive uniqueness) - see D-04.
CREATE EXTENSION IF NOT EXISTS citext;
