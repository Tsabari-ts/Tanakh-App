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

### Development

This project already has a `UserSecretsId` in `Tanakh.Api/Tanakh.Api.csproj`. Set secrets locally with:

```
dotnet user-secrets set "Email:EmailAddress" "your-address@example.com"
dotnet user-secrets set "Email:Password" "your-app-password"
dotnet user-secrets set "Email:RecipientAddress" "recipient@example.com"
dotnet user-secrets set "Email:SmtpServer" "smtp.example.com"
dotnet user-secrets set "Email:SmtpPort" "587"
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
```

Azure Key Vault integration was intentionally not added — this app's deployment targets
(Render/Neon/Cloudflare Pages, per the free-tier hosting plan) are not Azure, so there is no
Key Vault to integrate with.
