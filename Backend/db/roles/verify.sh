#!/usr/bin/env bash
# Proves least-privilege actually holds: app_user must NOT be able to DROP
# TABLE. Run after migrations_user.sql, app_user.sql, and at least one
# migration have been applied.
#
# Usage: PGHOST=localhost PGPORT=5433 PGDATABASE=tanakh \
#        APP_USER_PASSWORD='...' ./db/roles/verify.sh
set -euo pipefail

: "${PGHOST:=localhost}"
: "${PGPORT:=5432}"
: "${PGDATABASE:=tanakh}"
: "${APP_USER:=app_user}"
: "${APP_USER_PASSWORD:?APP_USER_PASSWORD must be set}"

log=$(mktemp)
if PGPASSWORD="$APP_USER_PASSWORD" psql -h "$PGHOST" -p "$PGPORT" -U "$APP_USER" -d "$PGDATABASE" \
    -c "DROP TABLE IF EXISTS subscribers;" >"$log" 2>&1; then
    echo "FAIL: app_user was able to DROP TABLE - least-privilege is broken." >&2
    cat "$log" >&2
    exit 1
fi

if ! grep -qiE "permission denied|must be owner" "$log"; then
    echo "FAIL: DROP TABLE failed, but not with a permission/ownership error - investigate." >&2
    cat "$log" >&2
    exit 1
fi

echo "OK: app_user cannot DROP TABLE (blocked by ownership/permissions, as expected)."
