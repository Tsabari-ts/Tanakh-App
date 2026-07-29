# Tanakh API — Backend

## Configuration

All secrets are supplied via configuration, never committed to the repo. The app reads an
`Email` section bound to `Tanakh.Options.EmailOptions`:

| Key | Purpose |
|---|---|
| `Email:EmailAddress` | Sending mailbox address |
| `Email:Password` | Sending mailbox password / app password |
| `Email:RecipientAddress` | Address that receives subscribe/unsubscribe notifications |
| `Email:SmtpServer` | SMTP host |
| `Email:SmtpPort` | SMTP port |

If any of these are missing, `EmailSender.SendMessage` returns `false` instead of sending —
it does not crash the request.

### Development

This project already has a `UserSecretsId` in `Tanakh.csproj`. Set secrets locally with:

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
