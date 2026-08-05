# Tanakh API — Backend

## Configuration

All secrets are supplied via configuration, never committed to the repo. The app reads a
`Sms` section bound to `Tanakh.Infrastructure.Options.SmsOptions` (reminders are sent via
SMS4FREE - email is no longer used for anything, reminders or otherwise):

| Key | Purpose |
|---|---|
| `Sms:Key` | SMS4FREE API key (from the account's own API page) |
| `Sms:User` | SMS4FREE account username (the registered mobile number) |
| `Sms:Pass` | SMS4FREE account password |
| `Sms:Sender` | Sender name/number shown to recipients. Currently the registered mobile number (SMS4FREE locks the sender to it on the free 10-message trial) - must switch to an English business/app name once the trial is used up, which likely needs its own sender-verification step with SMS4FREE per the 2020 telecom regulation. |
| `Sms:ApiUrl` | SMS4FREE send endpoint. Defaults to `https://api.sms4free.co.il/ApiSMS/v2/SendSMS` - **verify this against the account's own API page before relying on it**, don't assume it's still current. |
| `Sms:TimeoutSeconds` | HTTP timeout for the send call. Default 15. |
| `Sms:DryRun` | When `true` (the default), no HTTP call is made - the dispatcher builds the message and logs it as if sent. **Must stay `true` in every non-production environment** - there is no SMS4FREE sandbox account, so this is the only way to test without spending real messages. |

There's also a `TanakhData` section bound to `Tanakh.Infrastructure.Options.TanakhDataOptions`:

| Key | Purpose |
|---|---|
| `TanakhData:DataDirectory` | Overrides where `TanakhData.json`/`TanakhStructure.json` are read from. Optional — defaults to `Data/` under the app's content root. |

And a `Hashing` section bound to `Tanakh.Infrastructure.Options.HashingOptions`:

| Key | Purpose |
|---|---|
| `Hashing:Pepper` | HMAC key used by `IHashingService` (`consent_records.ip_hash`) and by `UnsubscribeTokenService` to sign manage tokens. Never rotate without a documented migration plan — rotating it invalidates every manage token currently in circulation. |

And an `Admin` section bound to `Tanakh.Infrastructure.Options.AdminOptions`, for the hidden admin panel (login → SMS OTP → cookie session, `AdminAuthController`/`AdminController`):

| Key | Purpose |
|---|---|
| `Admin:Username` | The single admin login username. |
| `Admin:PasswordHash` | Output of `dotnet run -- --hash-admin-password <password>` (PBKDF2-HMACSHA256, never a plaintext password). Re-run and replace to change the password. |
| `Admin:Phone` | E.164 phone number (`+9725XXXXXXXX`) that login OTP codes are sent to, via the same `ISmsSender`/SMS4FREE integration reminders use. |
| `Admin:LowBalanceThreshold` | SMS4FREE balance threshold for the future low-balance dashboard warning. Default 50. |

And a `Cors:AllowedOrigins` array — the frontend origin(s) allowed to make credentialed requests (required for the admin cookie to work cross-origin; `AllowAnyOrigin()` is incompatible with `credentials: include`):

| Key | Purpose |
|---|---|
| `Cors:AllowedOrigins:0`, `:1`, ... | Exact origins (scheme+host+port, no path) allowed to call the API with credentials. Locally this is `https://localhost:4200` (see `appsettings.Development.json`); production must set this to the real Cloudflare Pages origin before launch (see `docs/LAUNCH-CHECKLIST.md` L-01, same "no prod domain chosen yet" gap as `apiUrl`). |

### Development

This project already has a `UserSecretsId` in `Tanakh.Api/Tanakh.Api.csproj`. Set secrets locally with:

```
dotnet user-secrets set "Sms:Key" "..."
dotnet user-secrets set "Sms:User" "..."
dotnet user-secrets set "Sms:Pass" "..."
dotnet user-secrets set "Sms:Sender" "..."
dotnet user-secrets set "Hashing:Pepper" "any long random string, dev-only value is fine locally"
dotnet user-secrets set "Admin:Username" "..."
dotnet user-secrets set "Admin:PasswordHash" "$(dotnet run -- --hash-admin-password 'your-dev-password')"
dotnet user-secrets set "Admin:Phone" "+9725XXXXXXXX"
```

(Run `dotnet user-secrets init` first only if `UserSecretsId` is ever missing from the csproj.)
`Sms:DryRun` defaults to `true`, so local dev never calls the real SMS4FREE API unless you
explicitly set it to `false`.

### Production

Supply the same keys as environment variables, using `__` (double underscore) in place of `:`:

```
Sms__Key
Sms__User
Sms__Pass
Sms__Sender
Sms__DryRun=false
Hashing__Pepper
Admin__Username
Admin__PasswordHash
Admin__Phone
Cors__AllowedOrigins__0
```

Azure Key Vault integration was intentionally not added — this app's deployment targets
(Render/Neon/Cloudflare Pages, per the free-tier hosting plan) are not Azure, so there is no
Key Vault to integrate with.

## Database — dev seed data / reset

See `docs/database.md` for full setup (docker-compose, roles, migrations). Once `ConnectionStrings__AppDb`/`ConnectionStrings__MigrationsDb` and `Hashing:Pepper` are configured and migrations have been applied:

```bash
# Populate sample data (a few subscribers by phone number, reading
# positions, a sent/pending/failed reminder delivery each).
# Idempotent - running it again does nothing if subscribers already exist.
dotnet run -- --seed

# Drop the schema (rolled all the way down via migrations, not a literal
# DROP DATABASE - see DatabaseSeeder.ResetSchemaAsync for why), migrate back
# up to latest, then seed.
dotnet run -- --reset-db
```

Note the `--` before the flag — without it, `dotnet run` doesn't forward `--seed`/`--reset-db` to the app. Both are **hard-blocked outside `Development`** (`ASPNETCORE_ENVIRONMENT`) and throw if attempted anywhere else.
