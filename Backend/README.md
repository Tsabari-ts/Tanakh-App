# Tanakh API — Backend

## Configuration

All secrets are supplied via configuration, never committed to the repo. The app reads an
`Email` section bound to `Tanakh.Infrastructure.Options.EmailOptions`:

| Key | Purpose |
|---|---|
| `Email:EmailAddress` | Sending mailbox address |
| `Email:Password` | Sending mailbox password / app password |
| `Email:RecipientAddress` | Address that receives subscribe/unsubscribe notifications |
| `Email:SmtpServer` | SMTP host |
| `Email:SmtpPort` | SMTP port |

Email configuration is entirely optional and has no startup validation, by design: if any of
these are missing (or only some are set), `EmailSender.SendMessage` returns `false` instead of
sending — it does not crash the request, and the app starts normally either way.

There's also a `TanakhData` section bound to `Tanakh.Infrastructure.Options.TanakhDataOptions`:

| Key | Purpose |
|---|---|
| `TanakhData:DataDirectory` | Overrides where `TanakhData.json`/`TanakhStructure.json` are read from. Optional — defaults to `Data/` under the app's content root. |

And a `Hashing` section bound to `Tanakh.Infrastructure.Options.HashingOptions`:

| Key | Purpose |
|---|---|
| `Hashing:Pepper` | HMAC key used by `IHashingService` to hash emails for `suppression_list.email_hash` — never the plaintext address. Unlike `Email:*`, this has **no** graceful-degradation path: `EmailSender` treats "can't evaluate the suppression check" the same as "recipient is suppressed" (fail closed) and simply doesn't send, rather than crashing the request. Never rotate without a documented migration plan — rotating it invalidates every existing `suppression_list` lookup. |

### Development

This project already has a `UserSecretsId` in `Tanakh.Api/Tanakh.Api.csproj`. Set secrets locally with:

```
dotnet user-secrets set "Email:EmailAddress" "your-address@example.com"
dotnet user-secrets set "Email:Password" "your-app-password"
dotnet user-secrets set "Email:RecipientAddress" "recipient@example.com"
dotnet user-secrets set "Email:SmtpServer" "smtp.example.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Hashing:Pepper" "any long random string, dev-only value is fine locally"
```

(Run `dotnet user-secrets init` first only if `UserSecretsId` is ever missing from the csproj.)

### Production

Supply the same keys as environment variables, using `__` (double underscore) in place of `:`:

```
Email__EmailAddress
Email__Password
Email__RecipientAddress
Email__SmtpServer
Email__SmtpPort
Hashing__Pepper
```

Azure Key Vault integration was intentionally not added — this app's deployment targets
(Render/Neon/Cloudflare Pages, per the free-tier hosting plan) are not Azure, so there is no
Key Vault to integrate with.

## Database — dev seed data / reset

See `docs/database.md` for full setup (docker-compose, roles, migrations). Once `ConnectionStrings__AppDb`/`ConnectionStrings__MigrationsDb` and `Hashing:Pepper` are configured and migrations have been applied:

```bash
# Populate sample data (subscribers across every status, reading positions,
# some deliveries, some email events, a couple of suppression entries).
# Idempotent - running it again does nothing if subscribers already exist.
dotnet run -- --seed

# Drop the schema (rolled all the way down via migrations, not a literal
# DROP DATABASE - see DatabaseSeeder.ResetSchemaAsync for why), migrate back
# up to latest, then seed.
dotnet run -- --reset-db
```

Note the `--` before the flag — without it, `dotnet run` doesn't forward `--seed`/`--reset-db` to the app. Both are **hard-blocked outside `Development`** (`ASPNETCORE_ENVIRONMENT`) and throw if attempted anywhere else.
