# Backend Modernization (B-01..B-18) — Handoff

**Written:** 2026-07-30
**Repo:** `C:\Users\Tomer\Desktop\tomer\myProjects\Tanakh` (git remote `Tsabari-ts/Tanakh-App`, public, default branch `master`)
**Scope:** 18-task ASP.NET Core modernization spec (B-01..B-18), executed one task per commit, in the wave order below. **17 of 18 tasks are done and committed.** This document is a complete handoff so a fresh session can continue at B-15 (the last remaining task) with zero re-derivation.

---

## 1. Executive summary

The backend (`Backend/Tanakh.csproj`, single project, namespace `Tanakh`) started on **.NET 5** (EOL), with hardcoded absolute file paths from the original developer's machine, file-based secrets, no exception handling strategy, a dead commercial dependency (ServiceStack), a TFM-mismatched legacy cache package, Swashbuckle years out of date, and nullable reference types off (implicit nulls everywhere). It is now on **.NET 10**, builds with **zero warnings** under `TreatWarningsAsErrors=true` with NRT fully enabled, has no hardcoded paths, secrets come from User Secrets/env vars only, has a real production exception handler + HSTS, no dead dependencies, and a documented OpenAPI surface gated to dev only.

Along the way, **4 real pre-existing bugs were found and fixed** (not just papered over — see §5). Every task was verified by actually running the app (not just `dotnet build`) — hitting real endpoints, forcing real failure conditions (corrupted JSON files, missing config, Production-mode Docker runs on Linux), and checking response bodies/headers/logs, per this spec's explicit verification requirement.

**Nothing was done that wasn't asked.** Two decision points came up (B-07's cache backend, B-03's discovery of a real latent bug in `GetChapter`) and both were surfaced to the user via `AskUserQuestion` before proceeding, per the spec's ground rules.

---

## 2. Environment — read this before running anything

**This machine's system-wide `dotnet` (`C:\Program Files\dotnet`) only has SDK 8.0.100 installed, and the agent has no write access to that directory (verified).** A .NET 10 SDK (10.0.302) was installed side-by-side this session at `C:\Users\Tomer\dotnet10` via the official `dotnet-install.ps1 -Channel 10.0 -InstallDir "C:\Users\Tomer\dotnet10"` script.

**Every `dotnet` command in a fresh Bash tool call must be prefixed** (shell state does not persist between Bash tool calls in this environment):
```bash
export DOTNET_ROOT="C:\Users\Tomer\dotnet10"; export PATH="C:\Users\Tomer\dotnet10:$PATH"; cd "C:\Users\Tomer\Desktop\tomer\myProjects\Tanakh\Backend" && dotnet build
```
`Backend/global.json` pins SDK `10.0.302` (`rollForward: latestFeature`). Without the PATH prefix above, `dotnet` resolves to the system 8.0.100 install and **fails loudly** with a clear "install 10.0.302" message (this is intentional/correct behavior from global.json, not a bug) rather than silently building wrong.

**Other environment quirks discovered this session, all load-bearing for testing:**
- `dotnet run` and `dotnet SomeDll.dll` use the **current working directory** as `IHostEnvironment.ContentRootPath`, not the dll's own folder. When testing a Release build by running the dll directly, `cd` into `bin/Release/net10.0/` first — otherwise relative paths (e.g. `Data/TanakhStructure.json`) resolve against the wrong directory.
- `UseHsts()` middleware **excludes `localhost`/`127.0.0.1`/`[::1]` by default** — to verify the HSTS header, bind Kestrel to `0.0.0.0` and curl the machine's actual LAN IP (`powershell.exe -NoProfile -Command "(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { \$_.InterfaceAlias -notmatch 'Loopback' }).IPAddress"`), with `curl -k` to skip the dev cert's hostname mismatch.
- Use the **PowerShell tool**, not the Bash tool, for `Stop-Process`/`Get-NetIPAddress` etc. — the Bash tool runs via git-bash, which mangles PowerShell `$(...)` syntax before it reaches `powershell.exe`.
- Background server test pattern used throughout: start with Bash `run_in_background: true`, `sleep 5`, `curl` against it, then `tasklist //FI "IMAGENAME eq dotnet.exe"` to find the PID and `powershell.exe -NoProfile -Command "Stop-Process -Id <pid> -Force"` to stop it. Occasionally `rm -rf bin obj` fails with "Device or resource busy" right after stopping a process — harmless, `bin`/`obj` are gitignored, don't fight it.
- Docker Desktop is installed and running on this machine (used for B-08's Linux verification: `docker build -t x ./Backend` then `docker run -d -p 8099:8080 x`). **Always `rm -rf Backend/bin Backend/obj` before a Docker build** — a missing `.dockerignore` (now present, but verify it stays) or a stale local build will get copied into the image and clobber the container's own `dotnet restore`, producing a confusing `NU1064`-style "package not found" error that has nothing to do with the actual Dockerfile.
- **A real, pre-existing User Secret exists** for this project's `UserSecretsId` (`b54476fa-a2b7-4e0a-a3fe-83715f7795c0`, already in `Tanakh.csproj` before this modernization started): `Email:Password` is set to what looks like a real Gmail app password. It predates this session — **never overwrite, delete, or print it**. The other `Email:*` keys (`EmailAddress`, `RecipientAddress`, `SmtpServer`, `SmtpPort`) are **not** set. `dotnet user-secrets list` (with the PATH prefix above) shows current state.
- No test project exists in the solution yet (`Backend/Tanakh.sln` → `Backend/Tanakh.csproj` only, single project). This matters for B-18 (which asks for a before/after data-loading test) — see that task's notes below.

---

## 3. Commits made this session (chronological, oldest first)

Full history: `git log --oneline` from `dba9c4f` through `cc897a5`.

| Commit | Message |
|---|---|
| `dba9c4f` | Upgrade backend to .NET 10 (net5.0→net10.0 retarget, package version bumps; done *before* this spec existed, in a prior conversation) |
| `9ea30c5` | B-01: Pin SDK version with global.json |
| `fb5f8ce` | B-02: Convert Startup.cs + Program.cs to minimal hosting model |
| `2af2d41` | B-08: Eliminate all absolute disk paths |
| `7c843b7` | B-10: Load secrets from environment variables / user-secrets |
| `7a7156f` | B-14: Production exception handler + HSTS |
| `fc2475d` | B-05: Remove ServiceStack dependency |
| `b96b1b0` | B-07: Replace System.Runtime.Caching with IMemoryCache behind ITanakhCache |
| `694120e` | B-06: Replace Swashbuckle with built-in OpenAPI + Scalar |
| `f286c51` | B-03: Enable Nullable Reference Types and fix every warning |
| `cc897a5` | B-18: Migrate Newtonsoft.Json to System.Text.Json |
| `53b7423` | B-04: Split into Tanakh.Domain/Infrastructure/Api/Tests |
| `c1488e2` | B-09: Move remaining configuration to the Options pattern |
| `ad122e1` | B-16: Extract business logic from controllers into services |
| `e12c625` | B-11: Make all I/O asynchronous |
| `d67e71c` | B-12: Add CancellationToken to endpoints and outbound calls |
| `ec40b93` | B-13: Standardize errors on ProblemDetails |
| `e913f20` | B-17: Add health checks |

**Note on ordering vs the spec's recommended wave order:** the spec lists `B-08, B-10, B-14` as Wave 1 and `B-05, B-07, B-06` as Wave 2 — executed in exactly that order. Within B-01/B-02 (Wave 0) and B-03 (Wave 3), also exactly as specified. No reordering happened; the commit list above **is** the wave order, task-by-task.

---

## 4. Current backend state (as of `53b7423`)

**As of B-04, the backend is 4 projects, not 1.** `Tanakh.sln` → `Tanakh.Domain` (zero package refs), `Tanakh.Infrastructure` (references Domain), `Tanakh.Api` (references both — this is the executable/startup project, renamed from the original `Tanakh.csproj`, same `UserSecretsId`), `Tanakh.Tests` (new, xUnit + NetArchTest.Rules, references all three).

```
Backend/
├── .config/dotnet-tools.json          (declares dotnet-ef 8.0.7 — unused, leftover, not yet addressed)
├── .dockerignore                      (added B-08: bin/, obj/, .vs/, *.user — matches anywhere in the tree, still valid post-split)
├── Dockerfile                         (updated B-04 for multi-project restore/publish of Tanakh.Api.csproj specifically; entrypoint Tanakh.Api.dll)
├── README.md                          (added B-10: documents Email:* config keys — untouched by B-04)
├── Tanakh.sln                         (4 projects: Domain, Infrastructure, Api, Tests)
├── global.json                        (added B-01: pins SDK 10.0.302 — applies to the whole Backend/ tree including all 4 project subfolders)
├── Tanakh.Domain/                     (zero external PackageReferences — enforced by Tanakh.Tests' architecture test)
│   ├── Tanakh.Domain.csproj
│   ├── Caching/ITanakhCache.cs        (moved from old Backend/Caching/, namespace now Tanakh.Domain.Caching)
│   ├── IEmailSender.cs                (NEW in B-04 — extracted so Infrastructure's EmailSender is swappable/mockable)
│   └── EmailMessage.cs                (moved out of the old Model/SubscribeEntity.cs — it's the IEmailSender payload, not an HTTP-bound DTO)
├── Tanakh.Infrastructure/              (references Domain only)
│   ├── Tanakh.Infrastructure.csproj   (needs explicit Microsoft.Extensions.* package refs — a plain classlib doesn't get the ASP.NET Core shared framework)
│   ├── CacheProvider.cs               (moved out of Controllers/, where it never belonged; namespace now Tanakh.Infrastructure)
│   ├── EmailSender.cs                 (now implements IEmailSender; try/catch scope fixed B-10)
│   ├── Caching/MemoryTanakhCache.cs
│   ├── Options/EmailOptions.cs        (bound via .Bind() only, NOT YET validated — B-09's job)
│   └── Model/                         (raw external-API-mirroring DTOs — Infrastructure concern, not Domain)
│       ├── TanakhContainer.cs         (mirrors Sefaria API shape, ~150 properties, only ~9 ever read)
│       ├── TanakhStructure.cs
│       └── JewishCalendarContainer.cs (mirrors hebcal.com shape)
├── Tanakh.Api/                        (references Domain + Infrastructure — the executable project; ContentRootPath at runtime is this project's output dir)
│   ├── Tanakh.Api.csproj              (Sdk="Microsoft.NET.Sdk.Web"; same UserSecretsId as the old Tanakh.csproj)
│   ├── Program.cs                     (minimal hosting model since B-02; see §4.1)
│   ├── GlobalExceptionHandler.cs      (added B-14; namespace now Tanakh.Api)
│   ├── Controllers/
│   │   ├── TanakhController.cs        (GetChapter 200-vs-404 bug fixed B-03; logic UNCHANGED by B-04, still needs B-16's extraction)
│   │   ├── JewishCalendarController.cs (still has the hardcoded-2024-01-30 debug date — flagged, not fixed, see B-16's Risks note)
│   │   └── SubscribeController.cs     (now depends on IEmailSender, not concrete EmailSender; still returns bare bool via Ok(isSuccessful) — B-13's job)
│   ├── Model/                         (request/response DTOs actually shaped by this API's own contract)
│   │   ├── TanakhContext.cs           (Book — required-enforced, code-constructed)
│   │   └── SubscribeEntity.cs         (SubscribeEntity/UnSubscribe — required-enforced, STJ [FromBody]-bound)
│   ├── Data/
│   │   ├── TanakhData.json            (22MB Sefaria text data — colocated here because CopyToOutputDirectory must land in the executable project's output dir)
│   │   └── TanakhStructure.json
│   ├── Properties/
│   │   ├── PublishProfiles/           (untouched, stale local publish profile pointing at C:\Users\Tomer\Desktop\tomer\Publish — gitignored, was never tracked)
│   │   └── launchSettings.json        (untouched)
│   ├── appsettings.Development.json   (untouched)
│   └── appsettings.json               (untouched)
└── Tanakh.Tests/                       (new in B-04, references all three other projects)
    ├── Tanakh.Tests.csproj            (xUnit + NetArchTest.Rules 1.3.2)
    └── ArchitectureTests.cs           (asserts Tanakh.Domain has zero dependency on Infrastructure/Api/ASP.NET Core/EF Core — verified this actually catches a real violation, not a tautology, before trusting it)
```

`Backend/CredentialsManager.cs` and `Backend/Model/Credentials.cs` were **deleted** in B-10 (replaced by `Options/EmailOptions.cs`, now `Tanakh.Infrastructure/Options/EmailOptions.cs`).

No business logic moved during B-04 — controllers still contain the same logic they did before the split (JSON-to-response mapping, chapter navigation, candle-lighting time math). Extracting that into services is explicitly B-16's job, not B-04's.

### 4.1 Current `Tanakh.Api/Program.cs` (full content, for reference — don't re-read, this is authoritative)

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System.Diagnostics;
using Tanakh.Api;
using Tanakh.Domain;
using Tanakh.Domain.Caching;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Caching;
using Tanakh.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
});
builder.Services.AddSingleton<ITanakhCache, MemoryTanakhCache>();
builder.Services.AddScoped<CacheProvider>();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
```

**Middleware order is semantically load-bearing** — any future edit must preserve: (dev: exception page + OpenAPI/Scalar) OR (non-dev: exception handler + HSTS) → HTTPS redirect → routing → CORS → authorization → endpoints. This exact order is what B-14 verified, and B-04 re-verified unchanged.

### 4.2 Current `Tanakh.Api/Tanakh.Api.csproj` (full content)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>b54476fa-a2b7-4e0a-a3fe-83715f7795c0</UserSecretsId>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tanakh.Domain\Tanakh.Domain.csproj" />
    <ProjectReference Include="..\Tanakh.Infrastructure\Tanakh.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.10" />
    <PackageReference Include="Microsoft.OpenApi" Version="2.11.0" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.16.16" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Data\TanakhData.json" CopyToOutputDirectory="PreserveNewest" />
    <None Update="Data\TanakhStructure.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

`Microsoft.OpenApi` is pinned explicitly to `2.11.0` (not left as a transitive dependency) because `Microsoft.AspNetCore.OpenApi 10.0.10` pulls in `Microsoft.OpenApi 2.0.0` which has a known high-severity NuGet advisory (NU1903) that fails restore outright. `2.11.0` is the same major version (binary-compatible, avoids repeating the 1.x→2.x namespace break from the original Swashbuckle upgrade) with the fix.

**`TreatWarningsAsErrors=true` is unconditional** (not Release-only) on all 4 projects — this matches this B-series spec's literal instruction. Note this differs from an unrelated Phase-1 infra spec (R-02/R-03) which wants Release-only; that spec has not been executed in this repo, so there's no actual conflict yet, but flag it if Phase 1 work ever starts here.

`Tanakh.Infrastructure.csproj` pins `Microsoft.Extensions.Caching.Memory`/`Configuration.Abstractions`/`Hosting.Abstractions`/`Logging.Abstractions`/`Options` all at `10.0.10` (matching the OpenApi package's pinned version) — these come for free via the ASP.NET Core shared framework in a `Sdk="Microsoft.NET.Sdk.Web"` project, but `Tanakh.Infrastructure` is a plain `Sdk="Microsoft.NET.Sdk"` classlib, so they need explicit `PackageReference`s.

---

## 5. Real bugs found and fixed this session (not just annotation/mechanical work)

1. **`EmailSender.SendMessage`** (found during B-10): `MailMessage(from, to)` construction sat *outside* the try/catch guarding `smtpClient.Send()`. Empty `EmailAddress`/`RecipientAddress` (the default state for any environment without secrets configured — i.e. any fresh clone or CI run) threw an unhandled `ArgumentException` instead of the intended graceful `false`. Invisible before only because the original dev machine had a legacy secrets file outside the repo with real values. **Fixed**: moved the whole `SmtpClient`/`MailMessage` construction inside the try. User explicitly approved fixing this in-scope via `AskUserQuestion` before it was done.
2. **`CacheProvider.GetFullTanakhFromCache`** (found during B-03): `JsonConvert.DeserializeObject<TanakhContainer>()` result used with zero null check — malformed/empty `TanakhData.json` would NRE deep in unrelated code. **Fixed**: guard clause throwing `InvalidOperationException` naming the file and the specific missing field.
3. **`CacheProvider.GetTanakhStructureFromCache`** (found during B-03): identical pattern, `.Structures` dereferenced directly on a possibly-null deserialize result. **Fixed** the same way.
4. **`JewishCalendarController.FillJewishCalendar`** (found during B-03): identical unguarded-deserialize pattern for the hebcal.com HTTP response. **Fixed** the same way.
5. **`TanakhController.GetChapter`** (found during B-03 — **the big one**): `TanakhContext context = new TanakhContext();` default-constructed a non-null context at the top of the method; `if (context != null)` was therefore *always true*, making `return NotFound();` dead, unreachable code. Requesting a nonexistent chapter returned **`200 OK` with an empty body instead of `404`**. This was forced to the surface because marking `TanakhContext.ChosenSection`/`BookData` as `required` (the correct fix per the spec's own preference order) makes `new TanakhContext()` with no initializer a flat compile error (CS9035) — there was no way to leave this bug in place and still turn on NRT properly. **Fixed**: `TryGetValue`-based lookup, only construct `TanakhContext` when found, return `NotFound()` otherwise. Verified live: existing chapter → 200, nonexistent chapter → 404 (previously 200).

All five surfaced through direct investigation or forced compile errors — not guessed. Each was verified by actually reproducing the failure condition and confirming the fix (see individual commit messages for exact repro steps: corrupting a JSON file in a build-output copy, hitting real endpoints, checking headers/logs).

---

## 6. Architectural decisions made this session

| Decision | Choice | Why |
|---|---|---|
| B-07 cache backend | Plain `IMemoryCache` behind `ITanakhCache`, not `HybridCache`+Redis | Confirmed via `AskUserQuestion`: hosting target is a single free-tier Render instance (per the separate Phase 1 infra plan for this repo) — no horizontal scaling, no free managed Redis available. `ITanakhCache` abstraction still isolates call sites so a future Redis L2 is a contained change if the deployment target ever changes. |
| Newtonsoft-deserialized DTOs' nullability | All-nullable (`?`), not `required` | **Newtonsoft.Json does not enforce the C# `required` modifier** (confirmed: open Newtonsoft issue #2918, unimplemented as of writing). Marking a Newtonsoft-populated property `required` silences the compiler warning but provides zero runtime guarantee — a missing JSON field still silently becomes null. `required` is reserved for POCOs that are either code-constructed (`TanakhContext`) or bound via System.Text.Json (`SubscribeEntity`/`UnSubscribe`, via ASP.NET Core's default `[FromBody]` binder), where it's genuinely enforced. |
| Which Newtonsoft-DTO fields get runtime validation | Only the ~9 fields actually read by the app (verified via grep, not assumption): `Structure.book/sectionRef/heTitle/heSectionRef/he`, `BaseStructure.section/title/book`, `JewishCalendarContainer.items`, `Item.category` | The other ~140 properties across `TanakhContainer.cs`/`JewishCalendarContainer.cs` exist only because they mirror Sefaria's/hebcal's full API response shape and are never read anywhere in this codebase. Left nullable with no validation — there's no real invariant to enforce for data nobody consumes. **Not deleted** — dead-code removal is a separate, bigger decision explicitly left out of B-03's scope. Worth raising with the user as its own future task if repo cleanliness matters. |
| `Structure.next`/`Structure.prev` | Nullable, unvalidated | Legitimately absent on a book's first/last section — already correctly handled by the pre-existing `string.IsNullOrEmpty` guard in `TanakhController.Get()`. Validating these as "required" would be semantically wrong and would break normal data. |
| Data-directory config key (`TanakhData:DataDirectory`, introduced in B-08) | Left as a raw `IConfiguration["..."]` string-indexed read in `CacheProvider`'s constructor, as an explicitly-flagged stopgap | B-09 (Options pattern) is the task that formalizes this into a real `TanakhDataOptions` class. Doing it properly in B-08 would have been scope creep for a task that was just about eliminating hardcoded paths. |

---

## 7. What's postponed / explicitly out of scope

- **Deleting the ~140 unused Sefaria/hebcal DTO properties** (see §6) — flagged as a finding, not actioned. Ask the user before touching this if it comes up.
- **`Backend/.config/dotnet-tools.json`** declares `dotnet-ef 8.0.7` as a local tool, but there is no EF Core, no database, and no migrations anywhere in this codebase — likely leftover from abandoned scaffolding predating this modernization effort. Not touched by any B-series task; flag if it becomes relevant.
- **`Backend/Tanakh.Api/Properties/PublishProfiles/FolderProfile.pubxml`** (moved with the rest of `Properties/` in B-04) still points at `C:\Users\Tomer\Desktop\tomer\Publish` (the original dev's local path) — stale, unused by any current CI/deploy path, not touched. Note: this file is gitignored (`*.pubxml` in `.gitignore`) and was never actually tracked in git — it only exists on this local machine's disk.
- The Angular upgrade (Frontend has 11 known high-severity vulnerabilities in `@angular/*` packages, confirmed via `npm audit --omit=dev`) is explicitly out of scope for both this B-series spec and the separate Phase 1 infra spec for this repo. Do not touch `Frontend/` under this backlog.
- A parallel **Phase 1 infrastructure spec** exists for this same repo (repo layout, CI/CD, Docker Compose, Render/Neon/Cloudflare deployment) — only Task 0 (discovery) and one ad-hoc .NET 10 upgrade commit happened under that spec before this B-series backlog took over. They are complementary, not conflicting, but haven't been reconciled into one combined plan. If both specs are being executed against this repo, be aware R-01 (repo layout) will eventually want to move `Backend/`'s structure again (e.g. `docs/` conventions) and R-09 (Docker) will want to enhance the `Backend/Dockerfile` this session already added (non-root user, healthcheck, Alpine variant).

---

## 8. Immediate next action for a new session

1. Read this document in full.
2. Verify the environment is still as described in §2 (SDK location, global.json, user-secrets state) — a `dotnet --list-sdks` (with the PATH prefix) and `git log --oneline -12` are enough to confirm nothing has drifted. Note `Backend/Tanakh.csproj` no longer exists — the entry point is now `Backend/Tanakh.Api/Tanakh.Api.csproj`; adjust any hardcoded paths in your own commands accordingly (e.g. `dotnet run` from `Backend/Tanakh.Api/`, not `Backend/`).
3. **17 of 18 tasks are done.** Only **B-15** (API versioning under `/api/v1`) remains — see its entry in §9 before starting. It has the highest real-world-impact risk in the whole backlog: the Angular frontend hardcodes unversioned routes (`https://localhost:44308/Tanakh/...` etc.) today, so this task can break the live app unless handled deliberately (alias the old routes during a deprecation window, or coordinate with a frontend update — ask the user, don't guess, exactly as flagged below).
4. `/health/live` and `/health/ready` exist as of B-17 (`e913f20`) — readiness only checks Tanakh data file presence, deliberately excludes SMTP (see B-17's entry in §9 for why). Grep for `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`/`async void` still returns zero hits as of `d67e71c` — if a fresh session finds any, something has regressed.

---

## 9. Remaining tasks — full detail

### B-18 · Migrate Newtonsoft.Json → System.Text.Json · Wave 3 · P3 · M · **DONE (`cc897a5`)**

Completed this session. Both `JsonConvert.DeserializeObject` call sites (`CacheProvider.cs`, `JewishCalendarController.FillJewishCalendar`) migrated to `JsonSerializer.Deserialize`; `Newtonsoft.Json` PackageReference removed from `Tanakh.csproj`; `TanakhStructure.Structures` renamed to lowercase `structures` to match the JSON key exactly (STJ's case-sensitivity — Newtonsoft matched case-insensitively and would have masked this). Verified via a real before/after curl diff (book listings, a full chapter's text, the 200/404 chapter-lookup boundary, and the live hebcal.com-backed calendar endpoint) — all response bodies byte-identical to the Newtonsoft baseline. Re-ran the B-03 corrupted-`TanakhStructure.json` guard-clause test against a build-output copy — the same named `InvalidOperationException` + clean 500 fires identically under STJ. No `[JsonProperty]`/enum/date-parsing surprises turned up. `dotnet build -warnaserror` clean in both Debug and Release.

---

### B-04 · Split into `Tanakh.Api`, `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Tests` · Wave 4 · P1 · M · **DONE (`53b7423`)**

Completed this session. Both decisions from §10 were resolved via `AskUserQuestion` before starting: (1) proceed with the full 4-project split (not folders-only), (2) split `Model/` by role rather than keeping it together. See §4 for the resulting file tree and current `Program.cs`/`Tanakh.Api.csproj` content.

Key implementation notes for anyone touching this area later:
- `Tanakh.Domain` ended up genuinely thin (as predicted): `ITanakhCache`, a new `IEmailSender` interface (extracted from the concrete `EmailSender` specifically so it could have a Domain-owned contract), and `EmailMessage` (judged to be the `IEmailSender` payload/domain concept, not an HTTP-bound DTO — moved out of the old `Model/SubscribeEntity.cs` file, which is otherwise Api-layer). No domain exceptions were introduced (the B-03 guard clauses' `InvalidOperationException`s were left as-is) — turning those into named exception types was judged to be B-16-adjacent scope (real domain modeling), not required for a structural split, and left as a future option rather than done unprompted.
- The `ITanakhRepository`-for-`CacheProvider` idea floated in this section's original draft was **not** implemented — `CacheProvider` and the raw Sefaria/hebcal DTOs it returns (`TanakhContainer`, `List<BaseStructure>`) all live together in `Tanakh.Infrastructure`, and `Tanakh.Api` references `Tanakh.Infrastructure` directly. A repository contract whose return type is an Infrastructure-owned DTO can't live in `Tanakh.Domain` without breaking the dependency direction, so introducing a real Domain-facing service abstraction here was deferred to B-16, which is where the actual domain-shaped mapping (raw DTO → `Book`/`TanakhContext`) gets extracted out of the controller anyway.
- `Tanakh.Infrastructure` is a plain `Sdk="Microsoft.NET.Sdk"` classlib, so it needed explicit `PackageReference`s for `Microsoft.Extensions.Caching.Memory`/`Configuration.Abstractions`/`Hosting.Abstractions`/`Logging.Abstractions`/`Options` (all pinned `10.0.10`) — these come for free via the ASP.NET Core shared framework in a Web SDK project but not in a plain classlib.
- `Data/*.json` and `appsettings*.json` had to be colocated with `Tanakh.Api` (not left at `Backend/` root) — `CopyToOutputDirectory` only takes effect for the executable project whose output directory becomes `ContentRootPath` at runtime.
- The architecture test (`Tanakh.Tests/ArchitectureTests.cs`, using `NetArchTest.Rules 1.3.2`) was verified to actually catch violations, not just pass trivially: temporarily added a real `Microsoft.AspNetCore.Mvc.ControllerBase` reference to a Domain type, confirmed the test failed and named the exact offending type, then reverted before committing.
- `Backend/Dockerfile` now restores/publishes `Tanakh.Api/Tanakh.Api.csproj` specifically (project-reference graph pulls in Domain/Infrastructure automatically without needing the whole `.sln`); entrypoint is `Tanakh.Api.dll`. Verified with a real `docker build` + `docker run` + curl smoke test.
- Full before/after curl diff against every endpoint (book listings, a full chapter body, the 200/404 chapter-lookup boundary, both Subscribe endpoints, the live hebcal.com-backed calendar endpoint, and the OpenAPI/Scalar dev endpoints) — all byte-identical to pre-split behavior. No business logic changed, only file/project locations and namespaces.

---

### B-09 · Move all configuration to the Options pattern · Wave 4 · P1 · S · **DONE (`c1488e2`)**

**Purpose:** typed, validated configuration that fails at startup rather than mid-request.

**Current state: DONE (commit `c1488e2`).** `TanakhDataOptions` (new, `Backend/Tanakh.Infrastructure/Options/TanakhDataOptions.cs`) replaces `CacheProvider`'s raw `configuration["TanakhData:DataDirectory"]` read — `CacheProvider` now takes `IOptions<TanakhDataOptions>`. Verified end-to-end: pointed `TanakhData:DataDirectory` at an alternate directory, then corrupted that directory's copy of `TanakhStructure.json` and confirmed the resulting 500 named that exact override path (proving the option actually flows through, not silently falling back to the default). Grepped the whole solution afterward for `IConfiguration[`/`.GetSection(`/`.GetValue<` — zero hits outside `Program.cs`.

The fail-fast-vs-graceful-degradation question for `EmailOptions` was put to the user directly: **keep graceful degradation as-is, no startup validation at all.** An all-or-nothing "partially configured is an error" validation was actually implemented and tested first — it immediately broke startup against the real dev secrets state (`Email:Password` is the only one of 5 keys set, and has been all session) — so it was reverted per explicit instruction rather than silently reconciled one way or the other. `EmailOptions` is bound via plain `.Bind()`, nothing more. `README.md` updated to state this explicitly and to document the new `TanakhData:DataDirectory` key, plus fix stale `Tanakh.csproj`/`Tanakh.Options` path references left from B-04.

---

### B-16 · Controllers vs Minimal APIs · Wave 4 · P3 · M · **DONE (`ad122e1`)**

Both sub-decisions were put to the user directly before touching anything: (1) kept **Controllers** (not Minimal APIs) — this app's 5 simple endpoints have no complex binding needs, so there was nothing to gain from the switch and a real, if small, risk of losing the `[ApiController]` auto-400 behavior verified in B-03; (2) kept `TanakhController` as **one class** rather than splitting it by responsibility — its routes are unversioned and the Angular frontend hardcodes calls to `/Tanakh/books/...`, so a multi-class split would need every new controller's route explicitly pinned back to the same prefix for no real benefit on a 3-endpoint controller.

What actually happened: three services extracted, split by which project owns the DTOs they touch (this is also where B-04's deferred "repository contract for `CacheProvider`" question resolved itself naturally):
- `Tanakh.Infrastructure/Services/TanakhStructureService` (`ITanakhStructureService`: `GetAll`/`GetBySection`/`GetByTitle`) — wraps `CacheProvider`'s structure lookups. Lives in Infrastructure because it only touches Infrastructure-owned `BaseStructure`, no Api DTO involved.
- `Tanakh.Infrastructure/Services/JewishCalendarService` (`IJewishCalendarService`: `IsBetweenCandleLightingAndHavdalah`) — the hebcal.com HTTP call + candle-lighting/Havdalah window calculation, moved verbatim out of `JewishCalendarController`. Same reasoning: only touches `JewishCalendarContainer`/`Item`.
- `Tanakh.Api/Services/TanakhTextService` (`ITanakhTextService`: `GetChapter`) — the JSON-to-response mapping and chapter-navigation calculation, moved out of `TanakhController.Get()`/`GetNextSection()`. Lives in **Api**, not Infrastructure, specifically because it's the piece that bridges Infrastructure's raw DTOs (`TanakhContainer`/`BaseStructure`) into Api's own response DTOs (`TanakhContext`/`Book`) — this return type can't live in Domain without breaking the dependency direction, which is exactly the tension B-04 flagged and deferred to this point.

`SubscribeController` was left untouched — it already just maps input into an `EmailMessage` and delegates to `IEmailSender`, satisfying "bind input, call a service, map result" as-is.

Also fixed, per explicit user confirmation while this exact code was being moved (not silently): the hardcoded `DateTime currentDay = new DateTime(2024, 01, 30);` in the candle-lighting check → `DateTime.Now.Date`. This is a real, if small, behavior change beyond a pure refactor — flagged and confirmed before doing it, per this spec's ground rules.

Verified: full solution build clean (Debug + Release, `-warnaserror`), architecture test still passes, every controller method now fits on one screen (25–53 lines per file, one-line method bodies), full endpoint smoke test byte-identical to the pre-B-16 baseline, and the live hebcal.com-backed calendar endpoint still returns 200.

---

### B-11 · Make all I/O asynchronous · Wave 5 · P1 · S · **DONE (`e12c625`)**

Completed this session. `CacheProvider` (`GetFullTanakhFromCacheAsync`/`GetTanakhStructureFromCacheAsync`, using `File.ReadAllTextAsync`), the 3 B-16 services, and `EmailSender` (`SendMessageAsync` via `SmtpClient.SendMailAsync`) are all genuinely `Task`-returning now — every method involved was also renamed with the `Async` suffix, including the controller action methods themselves (`GetBookListAsync`, `GetChapterAsync`, `GetJewishCalendarAsync`, `RegisterNewUserAsync`, `DeleteUserAsync`, etc.) — safe to rename because every route in this app is explicit via `[HttpGet]`/`[Route]` attributes, never derived from the method name.

Added `Microsoft.VisualStudio.Threading.Analyzers 18.7.23` (checked against NuGet for the current stable version, not guessed) to all 3 non-test projects, active as errors via the existing `TreatWarningsAsErrors=true`. It immediately flagged **VSTHRD200** (missing `Async` suffix) on every controller action — fixed by renaming rather than suppressing the rule, since the rename was free here and keeps the analyzer meaningful for whoever touches this next, rather than carrying a standing exception list from day one.

`ITanakhCache`/`MemoryTanakhCache` deliberately left synchronous — `IMemoryCache` access is in-memory dictionary lookups, not real I/O, so there's nothing to make async there.

Verified: full solution build clean (Debug + Release, `-warnaserror`, analyzer active), architecture test still passes, grep for `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`/`async void` returns zero hits anywhere in the solution, full endpoint smoke test byte-identical to the pre-B-11 baseline (confirming the method renames didn't touch any route), the B-03/B-09 corrupted-JSON guard-clause test re-verified against the new async file-read path, and a full Docker build + container run smoke test given the new package references.

---

### B-12 · Add CancellationToken to every endpoint and outbound call · Wave 5 · P2 · S · **DONE (`d67e71c`)**

Completed this session. `CancellationToken` threaded from `HttpContext.RequestAborted` (auto-bound by ASP.NET Core when a controller action declares the parameter) down through `TanakhController`/`JewishCalendarController` → the 3 B-16 services → `CacheProvider` → `File.ReadAllTextAsync`/`HttpClient.GetAsync`/`HttpContent.ReadAsStringAsync`.

The judgment call this doc flagged was put to the user directly: **`SubscribeController`'s email send runs to completion regardless of client disconnect.** `SubscribeController`'s two actions take no `CancellationToken` parameter at all (with a comment explaining why), and `IEmailSender.SendMessageAsync` has no `CancellationToken` parameter on its interface — `EmailSender` internally passes `CancellationToken.None` to `SmtpClient.SendMailAsync` explicitly. Verified `SmtpClient.SendMailAsync(MailMessage, CancellationToken)` actually exists on .NET 10 by compiling a throwaway snippet against it first, rather than trusting the spec's own hedged wording about TFM support.

Verified real cancellation, not just that it compiles: temporarily added a 5-second delay + a direct console log to one action, fired `curl --max-time 1`, confirmed the log line proving `OperationCanceledException` fired almost immediately (long before the artificial delay would complete), then removed the temporary code before committing. Also confirmed ASP.NET Core's own handling of client-disconnect cancellations doesn't surface through `GlobalExceptionHandler` — documented framework behavior (no client left to respond to), not a regression.

---

### B-13 · Standardize errors on ProblemDetails (RFC 9457) · Wave 5 · P2 · S · **DONE (`ec40b93`)**

Completed this session. The Frontend contract this section warned about twice turned out to matter in a specific, non-obvious way: `Frontend/src/app/components/subscribe/subscribe.component.ts` does `this.subscribeSuccessful = response` — it treats the **entire response body** as the boolean, not a field within an object. That made the "success" shape safe to leave alone (any non-null object is truthy in JS regardless of contents, so wrapping `true` wouldn't have broken anything) but made wrapping **`false`** in an object actively dangerous: a 200 with `{ subscribed: false }` would still read as truthy client-side, silently making every failed send look successful. So:

- Success → `Ok(true)`, **untouched**, still a bare bool.
- Email-send failure → `Problem(statusCode: 502, title: "Failed to send notification email.", detail: "...")` via `Problem()`, reusing the `AddProblemDetails()` customization from `Program.cs` (confirmed live: the 502 response carries the same `traceId` extension as `GlobalExceptionHandler`'s output). 502 vs 500 vs "leave it a soft 200" was put to the user directly rather than guessed; 502 was chosen since it's semantically "tried to reach an upstream dependency (SMTP) and it failed."

This works because Angular's `HttpClient` routes any non-2xx response to the error callback regardless of body shape, and the frontend's error handler for these two calls doesn't read the body at all — it just logs it and falls back to `subscribeSuccessful`'s already-correct default (`false`). Net effect: the failure path is now RFC 9457-compliant with zero risk to the live frontend, verified live against the real (partially-configured, always-failing) dev SMTP secrets rather than just reasoned about. The pre-existing `[ApiController]` auto-400 on a missing required field was re-checked too — same ProblemDetails shape, unaffected.

---

### B-17 · Add health checks · Wave 5 · P1 · S · **DONE (`e913f20`)**

Completed this session. `/health/live` excludes every registered check (`Predicate = _ => false`) — pure "is the process up" liveness. `/health/ready` runs one check, `TanakhDataHealthCheck` (new, `Backend/Tanakh.Infrastructure/HealthChecks/`, tagged `"ready"`), which calls a new `CacheProvider.DataFilesExist()` (existence-only, not a full parse — kept cheap deliberately since readiness gets probed frequently).

The SMTP-responsiveness question this section flagged got resolved differently than originally scoped: put to the user directly, and the answer was **don't check SMTP at all**, not just "pick a check style." Reasoning, confirmed rather than assumed: B-09 already decided `EmailOptions` is optional with graceful degradation and no startup validation; the real dev secrets today only have `Email:Password` set, so any TCP-connect-style check against the (empty) `SmtpServer` would always fail — gating readiness on that would make this instance permanently report "not ready" despite serving Tanakh content correctly, directly contradicting B-09's own design. `SubscribeController` already reports its own `502` per-request (since B-13) when email delivery specifically fails, which is the right granularity for that concern — a whole-instance readiness check was the wrong tool for it.

Verified live, not just reasoned about: both endpoints 200 under normal conditions; renamed `TanakhData.json` out from under a *running* instance and confirmed `/health/ready` → 503 while `/health/live` stayed 200 (caught a real gotcha here — `dotnet run` resolves `ContentRootPath` to the project source folder, not the build output directory, so the first attempt silently touched the wrong copy of the file and the health check didn't budge; switched to running `dotnet Tanakh.Api.dll` directly from its own `bin/Debug/net10.0/` output, the same technique used in B-09/B-11's verification, to make the rename actually affect the running instance). Confirmed the response body is plain `"Unhealthy"` text, no paths or exception details. Full Docker build + container run re-verified given the new package reference, health endpoints confirmed working inside the container too.

---

### B-15 · Introduce API versioning · Wave 6 · P3 · S

**Purpose:** move all routes under `/api/v1` before any external client depends on unversioned paths.

**Files expected to change (paths updated post-B-04):** `Backend/Tanakh.Api/Tanakh.Api.csproj` (add `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` if still on Controllers by this point), `Backend/Tanakh.Api/Program.cs`, every controller's `[Route]` attribute (or Minimal API route registrations, depending on B-16's outcome).

**Critical pre-check, spelled out because it's easy to miss:** the spec says *"If any client already depends on the current unversioned paths, keep them as redirects or aliases for a deprecation window — ask before removing them outright."* **The Angular frontend is exactly such a client** — `Frontend/src/app/services/api-call.service.ts` hardcodes `https://localhost:44308/JewishCalendar/...`, `https://localhost:44308/Tanakh/...`, `https://localhost:44308/Subscribe/...` (confirmed during the original Phase 1 discovery pass on this repo — these are unversioned, hardcoded, and also a hardcoded-localhost issue in their own right, unrelated to this task). **This means B-15 will break the live Frontend integration unless either (a) the Frontend is updated in the same change (out of this backlog's stated scope), or (b) old unversioned routes are kept as aliases during a deprecation window.** This is not a hypothetical "ask before removing" — there is a confirmed real client depending on the unversioned paths today. Surface this prominently before starting B-15.

**Implementation steps (once the above is resolved with the user):**
1. Check actual current NuGet versions for `Asp.Versioning.Http`/`Asp.Versioning.Mvc.ApiExplorer` before pinning (per ground rule 3 — don't guess versions).
2. URL-segment versioning: `/api/v1/...`. `DefaultApiVersion = 1.0`, `AssumeDefaultVersionWhenUnspecified = true`.
3. Move every route under `/api/v1` (or add both old + new if aliasing).
4. OpenAPI document generated per version (should mostly fall out of the existing `AddOpenApi()`/`MapOpenApi()` setup from B-06 — verify).

**Verification/testing steps:** every route under `/api/v1`, `/openapi/v1.json` describes the v1 surface, and — critically — **manually load the actual Angular frontend against the modified backend** (per this repo's own stated policy: "For UI or frontend changes, start the dev server and use the feature in a browser before reporting the task as complete" — even though this is a backend task, it has a direct, confirmed frontend impact, so the same bar applies) to confirm nothing broke, or confirm aliasing is in place if the frontend isn't being updated alongside.

**Expected commit message:** `B-15: Introduce API versioning under /api/v1`

**Risks:** highest real-world-impact risk in the whole remaining backlog — this is the one task most likely to break the actual running application for actual users if rushed.

---

## 10. Decision points — present both, wait for the answer

### B-04: project split — **RESOLVED, see B-04's entry in §9**

User chose Option A (full 4-project split) via `AskUserQuestion`, then chose "split by role" for `Model/` placement (also via `AskUserQuestion`) when asked as a follow-up. Both are implemented and committed in `53b7423`. Left here for historical record; nothing further to decide.

### B-16: Controllers vs Minimal APIs — **RESOLVED, see B-16's entry in §9**

User chose to keep Controllers (not Minimal APIs — going against this document's own prior recommendation, which had been contingent on an auto-400 verification that was never actually needed once Controllers were chosen), and separately chose to keep `TanakhController` as one class rather than splitting it by responsibility, due to the Angular frontend's hardcoded `/Tanakh/books/...` routes. Both implemented and committed in `ad122e1`. Left here for historical record; nothing further to decide.

---

## 11. Quick-reference: verification commands used throughout this session

```bash
# Standard build/test cycle (always prefix with the SDK PATH override from §2)
export DOTNET_ROOT="C:\Users\Tomer\dotnet10"; export PATH="C:\Users\Tomer\dotnet10:$PATH"
cd "C:\Users\Tomer\Desktop\tomer\myProjects\Tanakh\Backend"
dotnet build -warnaserror           # or: dotnet build --no-incremental (to force full re-analysis)
dotnet build -c Release -warnaserror

# Start the app in the background for curl testing
ASPNETCORE_ENVIRONMENT="Development" dotnet run --no-build --urls "http://localhost:5299" > /path/to/log.txt 2>&1 &
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5299/Tanakh/books/torah

# Stop it afterward (from a plain Bash call, no PATH prefix needed)
tasklist //FI "IMAGENAME eq dotnet.exe"
powershell.exe -NoProfile -Command "Stop-Process -Id <PID> -Force"

# Production-mode + HSTS verification (needs a non-loopback bind + IP)
powershell.exe -NoProfile -Command "(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { \$_.InterfaceAlias -notmatch 'Loopback' }).IPAddress"
# then, from the Release build's own output directory (see §2's ContentRootPath note):
cd bin/Release/net10.0
ASPNETCORE_ENVIRONMENT="Production" dotnet Tanakh.dll --urls "https://0.0.0.0:5300"
curl -sik https://<lan-ip>:5300/Tanakh/books/torah   # -k skips the dev cert's hostname mismatch

# Vulnerability check
dotnet list package --vulnerable --include-transitive

# Docker verification (always clean bin/obj first)
rm -rf Backend/bin Backend/obj
docker build -t tanakh-verify ./Backend
docker run -d --name tanakh-verify -p 8099:8080 tanakh-verify
curl -s http://localhost:8099/Tanakh/books/Genesis/1
docker rm -f tanakh-verify && docker rmi tanakh-verify
```
