#!/usr/bin/env bash
# Provider-independent second backup copy (see docs/database.md "Backups").
# Neon's own point-in-time restore is the primary/fast recovery path; this
# script exists so a Neon-account-level incident doesn't leave us with zero
# backups. Intended to run on a daily schedule (cron / CI scheduled
# workflow), against the direct (unpooled) connection.
#
# Usage: DATABASE_URL='postgresql://user:pass@host:port/db?sslmode=require' \
#        BACKUP_DIR=/path/to/dir ./db/dumps/pg_dump.sh
#
# Upload the resulting file to object storage with whatever CLI your
# provider uses (aws s3 cp, rclone, az storage blob upload, ...) - not
# included here, since that choice depends on where backups are actually
# stored, which isn't decided by this script.
set -euo pipefail

: "${DATABASE_URL:?DATABASE_URL must be set (the direct/unpooled connection string)}"
: "${BACKUP_DIR:=.}"

mkdir -p "$BACKUP_DIR"

timestamp=$(date -u +%Y%m%dT%H%M%SZ)
out_file="$BACKUP_DIR/tanakh-$timestamp.dump"

pg_dump "$DATABASE_URL" --format=custom --file="$out_file"

echo "Backup written to $out_file"
