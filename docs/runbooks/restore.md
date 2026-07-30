# Restore runbook

Two independent backup mechanisms exist (see `docs/database.md` "Backups").
This runbook covers restoring from either.

## Path 1: Neon point-in-time restore (primary, fast path)

Use this for "restore to N minutes/hours ago" on staging/prod.

1. In the Neon console, open the project and the affected branch.
2. Use **Restore** (or **Time Travel**) to either roll the branch back to a
   timestamp, or create a **new branch** from that timestamp — prefer a new
   branch when you want to inspect/verify data before committing to a
   rollback, since it doesn't touch the live branch.
3. Point a scratch environment's `ConnectionStrings__AppDb`/
   `ConnectionStrings__MigrationsDb` at the new branch and verify the data
   looks right (see "Verifying a restore" below) before promoting it or
   repointing production traffic at it.
4. Only repoint the real `ConnectionStrings__*` env vars at the restored
   branch once verified — this is a manual, deliberate step, not automatic.

## Path 2: pg_dump restore (provider-independent second copy)

Use this if Neon itself is unavailable, or to restore into a fully separate
Postgres instance (e.g. local docker-compose, a new cloud provider).

```bash
# 1. Create a scratch database to restore into - never restore directly
#    over a live database you might still need.
psql "<direct connection string, as migrations_user or the bootstrap role>" \
  -c "CREATE DATABASE tanakh_restore_scratch;"

# 2. Restore the dump into it.
pg_restore --dbname="<connection string pointed at tanakh_restore_scratch>" \
  --no-owner --role=migrations_user /path/to/tanakh-<timestamp>.dump

# 3. Verify (see below), then either promote the scratch DB or extract
#    what you need from it.
```

## Verifying a restore

At minimum, confirm:

- All 8 tables exist: `subscribers`, `reading_progress`,
  `reminder_deliveries`, `email_events`, `suppression_list`,
  `consent_records`, `audit_log`, `__EFMigrationsHistory`.
- `__EFMigrationsHistory` lists every migration you expect
  (`dotnet ef migrations list` should match).
- Row counts / a few known rows in `subscribers` look right for the point
  in time you restored to.
- The append-only triggers on `consent_records`/`audit_log` are present
  (`\d consent_records` / `\d audit_log` should show the trigger) — a
  restore that misses the trigger has silently lost that protection.

## Drill log

| Date | Environment | Method | Outcome |
|---|---|---|---|
| 2026-07-30 | Local docker-compose (`tanakh-postgres`), restored into a scratch database (`tanakh_restore_drill`) on the same instance | `pg_dump --format=custom` → `pg_restore --no-owner` | **Success.** Inserted a marker row into `subscribers`, dumped the live dev DB, restored into a fresh scratch database, confirmed the marker row and all 8 tables (incl. `__EFMigrationsHistory`) were present and correct. Scratch database and dump file deleted after verification. |

**This drill exercised the pg_dump/pg_restore mechanism only, against local
docker-compose — it has not yet been run against a real Neon project**
(no Neon project exists for this app yet; staging/prod are still to be
provisioned per `docs/database.md`). Before launch, repeat this drill at
least once against the real staging Neon project — both the PITR path
(Path 1) and the pg_dump path (Path 2) — and add a new row to this table
recording that outcome.
