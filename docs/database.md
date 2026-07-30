# Database

PostgreSQL 16+, via EF Core 10 + the Npgsql provider. Naming convention is
snake_case in the database (`UseSnakeCaseNamingConvention()`), PascalCase in
C# — never use `[Column]` attributes to force a name; if the generated name
is wrong, that's a naming-convention bug, not something to paper over per
entity.

## Environments

| Environment | Where | Notes |
|---|---|---|
| Local dev | `docker-compose.yml` (`postgres:16-alpine`) | `docker compose up -d` from `Backend/`. Bootstrapped with `POSTGRES_*` from `.env` (copy `.env.example`). |
| Staging | Neon — separate project (or branch of the prod project) | Use a dedicated Neon **branch**, not a schema, so staging can be reset/reseeded independently of prod. |
| Production | Neon — dedicated project | Never share a Neon project between staging and prod; branches inherit the parent's data, which is the wrong direction for prod → staging isolation. |

Each environment is a **separate Neon project or branch**, each with its own
connection strings. Nothing environment-specific is hardcoded in the app —
see "Connection strings" below.

## Connection strings

The app and its migrations read **exactly two** environment variables, and
nothing else (no `appsettings.*.json` fallback, no hardcoded defaults):

| Variable | Used by | Neon connection type |
|---|---|---|
| `ConnectionStrings__AppDb` | The running app (`AddDbContextPool` in `Program.cs`) | **Pooled** (PgBouncer-backed) — Neon's connection string with `-pooler` in the endpoint hostname. Neon's serverless compute can suspend/resume and drop idle connections, so the app also has `EnableRetryOnFailure()` configured. |
| `ConnectionStrings__MigrationsDb` | `dotnet ef` only, via `AppDbContextFactory` (design-time factory, bypasses the app host) | **Direct** (unpooled) — DDL and long-running schema changes should not go through a transaction-pooling proxy, which can silently break session-level features migrations sometimes rely on (advisory locks, `SET` statements). |

Local dev (`.env`, see `.env.example`):

```
ConnectionStrings__AppDb=Host=localhost;Port=5432;Database=tanakh;Username=tanakh;Password=changeme
ConnectionStrings__MigrationsDb=Host=localhost;Port=5432;Database=tanakh;Username=tanakh;Password=changeme
```

(Same value for both locally — there's no pooler in front of the docker-compose instance.)

Neon (staging/prod), set as real environment variables in the hosting
platform — never in a committed file:

```
ConnectionStrings__AppDb=Host=ep-xxxx-pooler.us-east-2.aws.neon.tech;Port=5432;Database=tanakh;Username=app_user;Password=...;Ssl Mode=Require;Trust Server Certificate=false
ConnectionStrings__MigrationsDb=Host=ep-xxxx.us-east-2.aws.neon.tech;Port=5432;Database=tanakh;Username=migrations_user;Password=...;Ssl Mode=Require;Trust Server Certificate=false
```

Note the two hostnames: the pooled one has `-pooler` in it (copy from Neon's
dashboard "Pooled connection" tab), the direct one doesn't. `Username`
differs too — `app_user` (least-privilege) for the app, `migrations_user`
(schema owner) for migrations, both set up in a later task. **`Ssl
Mode=Require;Trust Server Certificate=false` is mandatory for every
non-local environment** — Neon requires TLS, and this setting both enforces
it and validates the server certificate (as opposed to `Trust Server
Certificate=true`, which would accept any certificate and defeat the
point).

## Local dev setup

```bash
cd Backend
cp .env.example .env   # fill in real values, .env is gitignored
docker compose up -d
dotnet ef database update --project Tanakh.Infrastructure --startup-project Tanakh.Api
```

If port 5432 is already bound locally (another Postgres container, a
system-installed instance, etc.), override `POSTGRES_PORT` in `.env` (and
update the `Port=` in both connection strings to match) rather than fighting
over the port.

## Migrations

Migrations live in `Tanakh.Infrastructure` (co-located with `AppDbContext`);
`Tanakh.Api` is the `--startup-project` because that's where
`Microsoft.EntityFrameworkCore.Design` is referenced. Design-time context
creation goes through `AppDbContextFactory` (an
`IDesignTimeDbContextFactory<AppDbContext>`), **not** through
`Tanakh.Api/Program.cs` — this is deliberate, so `dotnet ef` always uses
`ConnectionStrings__MigrationsDb` regardless of what the app's own DI
container is wired to.

```bash
dotnet ef migrations add <Name> --project Tanakh.Infrastructure --startup-project Tanakh.Api
dotnet ef migrations script --project Tanakh.Infrastructure --startup-project Tanakh.Api
dotnet ef database update --project Tanakh.Infrastructure --startup-project Tanakh.Api
```

## Encryption at rest

Neon encrypts data at rest by default for every project (AES-256, managed by
the underlying cloud provider's storage layer) — no configuration required
on our side. See [Neon's security docs](https://neon.tech/docs/security/security-overview)
for the current statement. This is not something we configure or can turn
off; it's a property of the platform.

## Backups

See `docs/runbooks/restore.md` for the restore procedure and the record of
the last drill. Two independent mechanisms are in place:

1. **Neon point-in-time restore (PITR)** — Neon retains a history of every
   branch for a configurable retention window (set per-project in the Neon
   console). This is the primary, fast recovery path for "restore to 10
   minutes ago" scenarios.
2. **Scheduled `pg_dump` to object storage** — a provider-independent second
   copy, so a Neon-account-level incident doesn't leave us with zero backups.
   Runs daily (see the scheduled job / CI workflow that invokes it).

## Raw SQL policy

All queries go through EF Core / LINQ, which parameterizes automatically. If
raw SQL is ever genuinely needed:

- **Allowed**: `FromSql($"...")` / `ExecuteSql($"...")` (the `FormattableString`
  overloads — interpolated values are parameterized), or `FromSqlRaw`/
  `ExecuteSqlRaw` with explicit `NpgsqlParameter` objects.
- **Forbidden**: `FromSqlRaw($"...")` or `ExecuteSqlRaw($"...")` built from C#
  string interpolation or concatenation — these are the unparameterized,
  injectable overloads. Also forbidden: building `ORDER BY`/table names from
  user input (can't be parameterized — use a whitelist map instead).

`.github/workflows/backend-ci.yml` greps every push/PR for
`FromSqlRaw`/`ExecuteSqlRaw` and fails the build on any hit — the intent is
that this repo has zero legitimate uses of either, so any hit needs a human
to look at it, not a smarter grep. If a real, parameterized use of
`FromSqlRaw`/`ExecuteSqlRaw` is ever added, update the CI step to allow-list
that specific line with a comment explaining why `FromSql`/`ExecuteSql`
wasn't enough.
