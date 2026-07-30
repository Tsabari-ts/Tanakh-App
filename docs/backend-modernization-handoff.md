# Backend Modernization (B-01..B-18) — Handoff

**Written:** 2026-07-30
**Repo:** `C:\Users\Tomer\Desktop\tomer\myProjects\Tanakh` (git remote `Tsabari-ts/Tanakh-App`, public, default branch `master`)
**Scope:** 18-task ASP.NET Core modernization spec (B-01..B-18), executed one task per commit, in the wave order below. **9 of 18 tasks are done and committed.** This document is a complete handoff so a fresh session can continue at B-18 with zero re-derivation.

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

Full history: `git log --oneline` from `dba9c4f` through `f286c51`.

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

**Note on ordering vs the spec's recommended wave order:** the spec lists `B-08, B-10, B-14` as Wave 1 and `B-05, B-07, B-06` as Wave 2 — executed in exactly that order. Within B-01/B-02 (Wave 0) and B-03 (Wave 3), also exactly as specified. No reordering happened; the commit list above **is** the wave order, task-by-task.

---

## 4. Current backend state (as of `f286c51`)

```
Backend/
├── .config/dotnet-tools.json       (declares dotnet-ef 8.0.7 — unused, leftover, not yet addressed)
├── .dockerignore                   (added B-08: bin/, obj/, .vs/, *.user)
├── Caching/
│   ├── ITanakhCache.cs             (added B-07, fixed B-03: TryGet<T> with [NotNullWhen(true)] out T?)
│   └── MemoryTanakhCache.cs        (added B-07: IMemoryCache-backed, 12h expiration, Size=1/entry, SizeLimit=100)
├── Controllers/
│   ├── CacheProvider.cs            (not actually a controller — B-04 will move it; has guard-clause validation added B-03)
│   ├── JewishCalendarController.cs (guard clause added B-03, GetJewishCalendar dead-line removed B-03)
│   ├── SubscribeController.cs      (UNTOUCHED since original — still returns bare bool via Ok(isSuccessful); B-13's job)
│   └── TanakhController.cs         (GetChapter 200-vs-404 bug fixed B-03)
├── Data/
│   ├── TanakhData.json             (22MB, Sefaria text data, moved from Properties/ in B-08)
│   └── TanakhStructure.json        (moved from Properties/ in B-08)
├── Dockerfile                      (added B-08: multi-stage, mcr.microsoft.com/dotnet/sdk:10.0 → aspnet:10.0)
├── EmailSender.cs                  (rewritten B-10 for EmailOptions; try/catch scope fixed B-10 — was a real latent bug)
├── GlobalExceptionHandler.cs       (added B-14: IExceptionHandler, generic ProblemDetails + traceId, full log server-side)
├── Model/
│   ├── JewishCalendarContainer.cs  (all-nullable rewrite B-03 — Newtonsoft-deserialized, mirrors hebcal.com shape)
│   ├── SubscribeEntity.cs          (all `required` B-03 — STJ-bound via [FromBody], required IS enforced here)
│   ├── TanakhContainer.cs          (all-nullable rewrite B-03 — Newtonsoft-deserialized, mirrors Sefaria API shape, ~150 properties, only ~9 ever read)
│   ├── TanakhContext.cs            (all `required` except NextChapter/PrevChapter (nullable) B-03 — code-constructed, required IS enforced)
│   └── TanakhStructure.cs          (all-nullable rewrite B-03 — Newtonsoft-deserialized)
├── Options/
│   └── EmailOptions.cs             (added B-10: EmailAddress/Password/RecipientAddress/SmtpServer/SmtpPort; bound via .Bind() only, NOT YET validated — B-09's job)
├── Program.cs                      (minimal hosting model since B-02; see full current content in §4.1 below)
├── Properties/
│   ├── PublishProfiles/            (untouched, stale local publish profile pointing at C:\Users\Tomer\Desktop\tomer\Publish)
│   └── launchSettings.json         (untouched)
├── README.md                       (added B-10: documents Email:* config keys, dev via user-secrets, prod via Email__* env vars)
├── Tanakh.csproj                   (see §4.2 below)
├── Tanakh.sln                      (single project, untouched)
├── appsettings.Development.json    (untouched — just Logging config)
├── appsettings.json                (untouched — just Logging + AllowedHosts)
└── global.json                     (added B-01: pins SDK 10.0.302)
```

`Backend/CredentialsManager.cs` and `Backend/Model/Credentials.cs` were **deleted** in B-10 (replaced by `Options/EmailOptions.cs`).

### 4.1 Current `Program.cs` (full content, for reference — don't re-read, this is authoritative)

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System.Diagnostics;
using Tanakh;
using Tanakh.Caching;
using Tanakh.Controllers;
using Tanakh.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
});
builder.Services.AddSingleton<ITanakhCache, MemoryTanakhCache>();
builder.Services.AddScoped<CacheProvider>();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddTransient<EmailSender>();
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

**Middleware order is semantically load-bearing** — any future edit must preserve: (dev: exception page + OpenAPI/Scalar) OR (non-dev: exception handler + HSTS) → HTTPS redirect → routing → CORS → authorization → endpoints. This exact order is what B-14 verified.

### 4.2 Current `Tanakh.csproj` (full content)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <UserSecretsId>b54476fa-a2b7-4e0a-a3fe-83715f7795c0</UserSecretsId>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.10" />
    <PackageReference Include="Microsoft.OpenApi" Version="2.11.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.16.16" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Data\TanakhData.json" CopyToOutputDirectory="PreserveNewest" />
    <None Update="Data\TanakhStructure.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

`Microsoft.OpenApi` is pinned explicitly to `2.11.0` (not left as a transitive dependency) because `Microsoft.AspNetCore.OpenApi 10.0.10` pulls in `Microsoft.OpenApi 2.0.0` which has a known high-severity NuGet advisory (NU1903) that fails restore outright. `2.11.0` is the same major version (binary-compatible, avoids repeating the 1.x→2.x namespace break from the original Swashbuckle upgrade) with the fix.

**`TreatWarningsAsErrors=true` is unconditional** (not Release-only) — this matches this B-series spec's literal instruction. Note this differs from an unrelated Phase-1 infra spec (R-02/R-03) which wants Release-only; that spec has not been executed in this repo, so there's no actual conflict yet, but flag it if Phase 1 work ever starts here.

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
- **`Backend/Properties/PublishProfiles/FolderProfile.pubxml`** still points at `C:\Users\Tomer\Desktop\tomer\Publish` (the original dev's local path) — stale, unused by any current CI/deploy path, not touched.
- The Angular upgrade (Frontend has 11 known high-severity vulnerabilities in `@angular/*` packages, confirmed via `npm audit --omit=dev`) is explicitly out of scope for both this B-series spec and the separate Phase 1 infra spec for this repo. Do not touch `Frontend/` under this backlog.
- A parallel **Phase 1 infrastructure spec** exists for this same repo (repo layout, CI/CD, Docker Compose, Render/Neon/Cloudflare deployment) — only Task 0 (discovery) and one ad-hoc .NET 10 upgrade commit happened under that spec before this B-series backlog took over. They are complementary, not conflicting, but haven't been reconciled into one combined plan. If both specs are being executed against this repo, be aware R-01 (repo layout) will eventually want to move `Backend/`'s structure again (e.g. `docs/` conventions) and R-09 (Docker) will want to enhance the `Backend/Dockerfile` this session already added (non-root user, healthcheck, Alpine variant).

---

## 8. Immediate next action for a new session

1. Read this document in full.
2. Verify the environment is still as described in §2 (SDK location, global.json, user-secrets state) — a `dotnet --list-sdks` (with the PATH prefix) and `git log --oneline -12` are enough to confirm nothing has drifted.
3. Start **B-18** (next task in wave order — see full spec below). It has no decision point, so no need to check in before starting, but per this spec's ground rules, still stop and ask if anything unexpected turns up (e.g. `[JsonProperty]` attributes not accounted for below, or a second test project convention already in place).
4. When you reach **B-04**, stop and present the project-split decision (§10) before moving any files.
5. When you reach **B-16**, stop and present the Controllers-vs-Minimal-APIs decision (§10) before restructuring endpoints.

---

## 9. Remaining tasks — full detail

### B-18 · Migrate Newtonsoft.Json → System.Text.Json · Wave 3 · P3 · M

**Purpose:** STJ is faster and ships with the framework; remove the last third-party JSON dependency.

**Files expected to change:**
- `Backend/Tanakh.csproj` — remove `Newtonsoft.Json` PackageReference.
- `Backend/Controllers/CacheProvider.cs` — both `JsonConvert.DeserializeObject<T>(...)` calls → `JsonSerializer.Deserialize<T>(...)`.
- `Backend/Controllers/JewishCalendarController.cs` — same, in `FillJewishCalendar`.
- Possibly `Backend/Program.cs` — if `[FromBody]` behavior needs any STJ options tweaks (see risk below), configure via `builder.Services.AddControllers().AddJsonOptions(...)` or `builder.Services.ConfigureHttpJsonOptions(...)`.
- No `[JsonProperty]`/`[JsonConverter]` attributes exist anywhere in the current `Model/` classes (confirmed by grep during B-03's work) — nothing to translate to `[JsonPropertyName]`.

**Current state:** `CacheProvider.cs` deserializes `TanakhData.json` → `TanakhContainer` and `TanakhStructure.json` → `TanakhStructure` via `Newtonsoft.Json.JsonConvert.DeserializeObject<T>`. `JewishCalendarController.FillJewishCalendar` deserializes the hebcal.com HTTP response the same way. `SubscribeEntity`/`UnSubscribe` request bodies already go through **System.Text.Json** today (ASP.NET Core's default `[FromBody]` binder — `AddNewtonsoftJson()` is never called in `Program.cs`), so those are *already* on STJ and don't need touching for B-18 itself — B-18 is really about the two `JsonConvert.DeserializeObject` call sites plus the package removal.

**Critical, concrete risk verified this session:** `Backend/Data/TanakhStructure.json`'s top-level JSON key is **lowercase** `structures`, while the C# property is `TanakhStructure.Structures` (capital S). Newtonsoft matches case-insensitively by default; **STJ is case-sensitive by default** and will silently leave `Structures` null (not throw) unless you either (a) rename the C# property to `structures` (lowercase, matching JSON exactly — the spec's own stated preference: "fix the property names to match the JSON exactly (prefer the latter)"), or (b) pass `JsonSerializerOptions { PropertyNameCaseInsensitive = true }` to `JsonSerializer.Deserialize`. **Recommendation: option (a)** — rename `TanakhStructure.Structures` to `TanakhStructure.structures` (matches this project's existing convention: every other property in `Model/TanakhContainer.cs` and `Model/JewishCalendarContainer.cs` is already lowercase/camelCase to match their source JSON verbatim; `Structures` was the one outlier). Before doing this, dump the actual top-level keys of `Data/TanakhData.json` too (it's 22MB — use `node -e "console.log(Object.keys(JSON.parse(require('fs').readFileSync('Data/TanakhData.json','utf8'))))"` or similar, don't try to open it in an editor) to confirm `TanakhContainer.structures` casing matches (it already does — `structures` lowercase, confirmed this session, see §5 bug #2's investigation).

**Other STJ behavioral differences to check deliberately (per spec):**
- Trailing commas / comments: unlikely to be present in these auto-generated data files, but verify `JsonSerializerOptions` doesn't need `AllowTrailingCommas`/`ReadCommentHandling` — check by just trying `JsonSerializer.Deserialize` against the real files first before adding options preemptively.
- Missing fields → `default` (same as Newtonsoft's null-for-reference-types behavior) — this is exactly what B-03's guard clauses in `CacheProvider`/`FillJewishCalendar` already defend against, so no new risk here, just confirm the guard clauses still fire correctly under STJ (they check for `null`, which STJ also produces for missing fields on nullable reference-typed properties).
- No enums are serialized as strings anywhere in this codebase (confirmed by grep — no `enum` types exist in `Model/`) — `JsonStringEnumConverter` is not needed.
- `DateTime` handling: `JewishCalendarContainer.date`/`Item.date` are `DateTime` (not `DateTimeOffset`). STJ's default `DateTime` parsing is ISO-8601-only and stricter than Newtonsoft's. hebcal.com's API returns ISO-8601 dates (standard), so this is _expected_ to be fine, but **verify by actually calling `GET /JewishCalendar/getJewishCalendar` against the real hebcal.com endpoint after the migration** — this is a live external API, no mocking exists, so this is a real integration test, not a mock.

**"Before/after" data-loading test the spec asks for:** the spec wants a test that "loads TanakhData.json and TanakhStructure.json and asserts on specific known values... Run it against Newtonsoft first to capture the baseline, then against STJ." **There is no test project in the solution yet** (B-04, which comes *after* B-18 in wave order, is what creates `Tanakh.Tests`). Options, in order of preference:
1. Do this as a **manual, scripted verification** instead of an automated test: before touching any code, run the app on the current Newtonsoft-based code and capture known values via curl (e.g. total book count from `GET /Tanakh/books/torah`, a specific verse's text from `GET /Tanakh/books/Genesis/1`, chapter counts for a few books) into a scratch file. After migrating to STJ, re-run the same curls and diff. This satisfies the spirit of the requirement without a premature test project.
2. Alternatively, ask the user whether to pull B-04's `Tanakh.Tests` project creation forward to happen first, just for this one test — this would be a deviation from the spec's stated wave order, so **ask before doing this**, don't silently reorder.
Recommendation: do (1). It's lower-risk, doesn't reorder the spec, and still gives a real before/after comparison.

**Verification/testing steps:**
1. `dotnet build -warnaserror` clean (Debug and Release).
2. Capture baseline curl outputs (Newtonsoft) per the before/after plan above.
3. Migrate the two `DeserializeObject` call sites + rename `Structures`→`structures`.
4. Remove `Newtonsoft.Json` PackageReference, confirm `dotnet build` still succeeds with no missing-reference errors (i.e. nothing else in the codebase references `Newtonsoft` — grep to confirm zero hits after removal).
5. Re-run the same curls, diff against baseline — must match exactly.
6. Re-run the B-03 guard-clause verification (corrupt a copy of `TanakhStructure.json` in a build-output directory, confirm the same clean 500 + named exception behavior still works under STJ).
7. Hit the live hebcal.com-backed endpoint (`GET /JewishCalendar/getJewishCalendar`) for real — this is the one call site with genuine external-API date-parsing risk.

**Expected commit message:** `B-18: Migrate Newtonsoft.Json to System.Text.Json`

**Risks:** the lowercase-`structures` case-sensitivity trap (documented above, already diagnosed — don't rediscover it the hard way). Otherwise low risk; this is a small, contained codebase with only 2 real deserialization call sites.

---

### B-04 · Split into `Tanakh.Api`, `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Tests` · Wave 4 · P1 · M · **DECISION NEEDED**

See §10 for the decision to present before starting.

**Purpose:** establish clean layering; `CacheProvider` currently sits in `Controllers/` despite not being a controller.

**Files expected to change:** essentially everything — this is a full project restructure. New files: `Tanakh.Domain.csproj`, `Tanakh.Infrastructure.csproj`, `Tanakh.Api.csproj` (renamed from today's `Tanakh.csproj`?), `Tanakh.Tests.csproj`, updated `Tanakh.sln`. Every existing `.cs` file moves to one of the three non-test projects.

**Current state:** single project `Backend/Tanakh.csproj`, everything in one assembly, namespace `Tanakh` (with `Tanakh.Controllers`, `Tanakh.Model`, `Tanakh.Options`, `Tanakh.Caching` as sub-namespaces).

**Proposed layering (per spec, confirm with user before executing):**

| Project | Gets | Notes |
|---|---|---|
| `Tanakh.Domain` | `Model/Book`, `Model/TanakhContext` (rename? these are really response DTOs, not domain entities — worth discussing with user whether `Model/` as a whole belongs in Domain or is actually an API contract that belongs in `Tanakh.Api`), `ITanakhCache`, a new `ITanakhRepository`/similar contract for what `CacheProvider` does, `IEmailSender` contract, `IJewishCalendar` contract, domain exceptions (the `InvalidOperationException`s from B-03's guard clauses are candidates to become named domain exceptions here, e.g. `TanakhDataCorruptException`) | **Zero PackageReferences for ASP.NET Core/EF Core/anything web** — base class library only |
| `Tanakh.Infrastructure` | `CacheProvider` (moved out of `Controllers/`, probably renamed), `MemoryTanakhCache`, `EmailSender`, the hebcal HTTP client logic from `JewishCalendarController.FillJewishCalendar`, the JSON data loader | References `Tanakh.Domain` |
| `Tanakh.Api` | `Program.cs`, `Controllers/*Controller.cs` (or Minimal API endpoints, depending on B-16), `GlobalExceptionHandler.cs`, `Options/EmailOptions.cs` (or does this belong in Infrastructure? — worth asking), `appsettings*.json`, `Data/*.json` | References `Tanakh.Domain` + `Tanakh.Infrastructure` |
| `Tanakh.Tests` | New. Unit tests for Domain, integration tests via `WebApplicationFactory<Program>` (the `public partial class Program;` marker added in B-02 exists specifically for this) | References all |

**Also required:** an architecture test (NetArchTest or equivalent) asserting `Tanakh.Domain` never gains a forbidden reference — add this to `Tanakh.Tests` as part of this same task.

**Verification/testing steps:**
1. Move types one layer at a time, `dotnet build` after each move (never leave the tree non-compiling, per this spec's ground rule 1).
2. Full solution build + the architecture test passes.
3. Full runtime smoke test (every endpoint) — same curls used throughout this session.
4. Docker build still works (`Backend/Dockerfile` will need its COPY paths updated for the new multi-project structure — this is a real, concrete follow-on change, not optional).

**Expected commit message:** `B-04: Split into Tanakh.Domain/Infrastructure/Api/Tests`

**Risks:** this is the highest-blast-radius task in the remaining backlog — touches every file. The `Dockerfile` from B-08 and the `.csproj`/`global.json` structure will all need coordinated updates. Recommend doing this as one deliberate, careful pass rather than incrementally, given the "never leave the tree non-compiling" rule is hard to satisfy with partial multi-project moves — commit only when the full solution builds and every endpoint is verified again.

---

### B-09 · Move all configuration to the Options pattern · Wave 4 · P1 · S

**Purpose:** typed, validated configuration that fails at startup rather than mid-request.

**Files expected to change:** `Backend/Options/EmailOptions.cs` (add validation), new `Backend/Options/TanakhDataOptions.cs`, `Backend/Program.cs`, `Backend/Controllers/CacheProvider.cs` (remove the raw `configuration["TanakhData:DataDirectory"]` read from B-08).

**Current state:**
- `EmailOptions` exists (`Options/EmailOptions.cs`) but is bound with only `.Bind(...)` — **no `.ValidateDataAnnotations().ValidateOnStart()`, no `[Required]` attributes on its properties.** A missing `Email__Password` env var in Production today does NOT fail startup — it silently produces an empty-string default, and `EmailSender` gracefully returns `false` (per B-10's bug fix) rather than crashing. This task should decide: should Email config actually be required-at-startup (fail fast), or is graceful-degradation-to-no-email-sent the intended behavior for this app? **This is worth a quick confirmation with the user** — the spec's own B-09 definition of done ("deleting a required key... causes fail at startup") assumes fail-fast is wanted, but B-10 deliberately built graceful degradation. Reconcile before implementing.
- `CacheProvider`'s constructor does `configuration["TanakhData:DataDirectory"] ?? Path.Combine(environment.ContentRootPath, "Data")` — a raw string-indexed `IConfiguration` read, explicitly flagged in B-08's commit message as "a stopgap ahead of the proper Options class B-09 introduces."

**Implementation steps:**
1. Create `TanakhDataOptions` with a `DataDirectory` property (nullable/optional — it has a legitimate default, doesn't need `[Required]`).
2. Decide (ask user) whether `EmailOptions` should be `ValidateOnStart`-required or stay optional/graceful. If required: add `[Required]`/`[EmailAddress]` attributes, `.ValidateDataAnnotations().ValidateOnStart()` in `Program.cs`. If staying optional: document explicitly in `Backend/README.md` why (referencing B-10's graceful-degradation design), and add a lighter validation (e.g. all-or-nothing: either all 5 Email keys are set, or none are — a partially-configured state is probably a real misconfiguration worth failing loudly on).
3. Update `CacheProvider` to take `IOptions<TanakhDataOptions>` instead of raw `IConfiguration`.
4. Grep the whole solution for remaining `IConfiguration[` / `.GetSection(` / `.GetValue<` outside `Program.cs` — should be zero after this task.

**Verification/testing steps:**
1. `dotnet build -warnaserror` clean.
2. If `EmailOptions` becomes `ValidateOnStart`-required: delete the `Email:Password` user secret (⚠️ **back it up first** — `dotnet user-secrets list` to capture the real value before removing, restore it after the test), confirm the app fails to start with a clear message naming the missing key, then restore the secret.
3. Confirm `TanakhData:DataDirectory` override still works (this was tested in B-08 — re-verify after the refactor).
4. Full endpoint smoke test.

**Expected commit message:** `B-09: Move configuration to the Options pattern`

**Risks:** the fail-fast-vs-graceful-degradation question for Email config is a real design decision, not mechanical — don't guess, ask.

---

### B-16 · Controllers vs Minimal APIs · Wave 4 · P3 · M · **DECISION NEEDED**

See §10 for the decision to present before starting.

**Purpose:** the architectural choice is secondary; the non-negotiable part is **no business logic inside endpoint methods**.

**Files expected to change:** all of `Controllers/` (or their Minimal-API-endpoint replacements), likely new `Services/` or similar in whichever project layer B-04 established.

**Current state:** `TanakhController` has 3 endpoints plus 2 private helper methods (`Get()`, `GetNextSection()`) that contain real business logic (JSON-to-response-model mapping, chapter navigation calculation) directly in the controller. `JewishCalendarController` has 1 endpoint with real business logic (candle-lighting/Havdalah time-window calculation) directly in the action method. `SubscribeController` has 2 endpoints, comparatively thin already (just builds an email and delegates to `EmailSender`).

**Implementation steps (regardless of Controllers vs Minimal APIs decision):**
1. Extract `TanakhController.Get()`'s JSON-mapping logic and `GetNextSection()`'s navigation logic into a service (in Domain/Infrastructure per B-04's layering).
2. Extract `JewishCalendarController`'s candle-lighting/Havdalah window calculation into a service.
3. Split `TanakhController` by responsibility per the spec: text retrieval, search/list, structure/navigation — currently all three are jammed into one controller.
4. Endpoint methods should do exactly 3 things: bind input, call a service, map the result to a response — verify every remaining endpoint method fits on one screen (spec's literal definition of done).

**Verification/testing steps:** full endpoint smoke test after extraction — behavior must be identical, this is a pure refactor.

**Expected commit message:** `B-16: Extract business logic from controllers into services` (+ mention Minimal APIs migration in the message if that's the chosen option)

**Risks:** `JewishCalendarController`'s date logic has a hardcoded `DateTime currentDay = new DateTime(2024, 01, 30);` with the real `DateTime.Now.Date` line commented out directly below it — this looks like debug code left in from development, unrelated to any B-series task. **Flag this to the user when you reach B-16** (or sooner) — it's a pre-existing bug (the "is it candle-lighting time" check is currently pinned to Jan 30 2024 forever, making the whole endpoint always evaluate against a fixed historical date) that's out of scope for any task so far but is exactly the kind of thing worth a one-line mention rather than silent fixing, per this spec's ground rules about not silently expanding scope.

---

### B-11 · Make all I/O asynchronous · Wave 5 · P1 · S

**Purpose:** eliminate sync-over-async; thread-pool starvation risk.

**Files expected to change:** `Backend/Controllers/JewishCalendarController.cs` (the known offender), `Backend/Controllers/CacheProvider.cs` (file reads), `Backend/Controllers/TanakhController.cs` (action method signatures need `async Task<IActionResult>`), `Backend/EmailSender.cs` (`SmtpClient.Send` → `SendMailAsync`), `Backend/Controllers/SubscribeController.cs` (action signatures).

**Current state:** `JewishCalendarController.GetJewishCalendar()` does `FillJewishCalendar().GetAwaiter().GetResult()` — confirmed still present as of this handoff (B-03 touched this file but did not address the sync-over-async call, since that's explicitly B-11's job, not B-03's). `CacheProvider` uses `StreamReader.ReadToEnd()` (sync) — should become `File.ReadAllTextAsync(...)`. `EmailSender.SendMessage` uses `SmtpClient.Send()` (sync, and `SmtpClient` itself is legacy/obsolete in modern .NET — consider whether this task should also flag `SmtpClient` obsolescence, though replacing it entirely is arguably beyond "make it async," worth a quick note to the user).

**Implementation steps:**
1. `CacheProvider.GetFullTanakhFromCache`/`GetTanakhStructureFromCache` → `async Task<TanakhContainer>`/`async Task<List<BaseStructure>>`, using `File.ReadAllTextAsync`.
2. `JewishCalendarController.FillJewishCalendar` already returns `Task<JewishCalendarContainer>` — just remove the `.GetAwaiter().GetResult()` call site in `GetJewishCalendar()` and make that method `async Task<IActionResult>`.
3. `TanakhController`'s action methods and private `Get()`/`GetNextSection()` helpers all need to become async all the way up, since they call `CacheProvider`.
4. `EmailSender.SendMessage` → `SendMessageAsync` using `SmtpClient.SendMailAsync(...)`; `SubscribeController`'s two action methods become async.
5. Add the threading analyzer as the spec asks (`Microsoft.VisualStudio.Threading.Analyzers` or built-in equivalent rules) as errors, to prevent regressions.

**Verification/testing steps:**
1. Grep for `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void` — must return zero hits (excluding `Program.cs` top-level statements, which are inherently synchronous by design and not part of this rule).
2. `dotnet build -warnaserror` clean with the new analyzer enabled.
3. Full endpoint smoke test — response bodies must be byte-identical to before.

**Expected commit message:** `B-11: Make all I/O asynchronous`

**Risks:** low — mechanical `async`/`await` propagation. The threading analyzer package name/exact rule IDs should be verified against NuGet before adding (per this spec's ground rule 3: don't invent versions or flags, check first).

---

### B-12 · Add CancellationToken to every endpoint and outbound call · Wave 5 · P2 · S

**Purpose:** disconnecting a client should abort server-side work.

**Files expected to change:** all controller action methods (post-B-11, they're all `async` already), `CacheProvider`'s now-async methods, `EmailSender`, `JewishCalendarController`'s `HttpClient` call.

**Depends on B-11 landing first** (can't thread a `CancellationToken` through sync methods meaningfully).

**Implementation steps:**
1. Add `CancellationToken cancellationToken` parameter to every action method — ASP.NET Core binds `HttpContext.RequestAborted` automatically, no extra wiring needed.
2. Thread it through every service method, `File.ReadAllTextAsync(path, cancellationToken)`, `HttpClient.GetAsync(url, cancellationToken)`, `SmtpClient.SendMailAsync` (note: `SmtpClient` has no native `CancellationToken` overload in some TFMs — verify against .NET 10's actual `SmtpClient` API surface before assuming; if unsupported, flag it rather than faking cancellation support).
3. Explicitly identify anything that must NOT be cancelled (the spec calls out audit-record-style writes) — in this codebase, there isn't an obvious candidate (no audit logging exists), but double-check `EmailSender`'s notification email arguably should complete even if the HTTP client disconnects (the user still wants to be notified of the subscribe/unsubscribe request even if the browser tab closes) — **flag this specific judgment call to the user**: should `SubscribeController`'s email send use `RequestAborted` (cancels with the request) or a separate, non-cancellable lifetime (e.g. `CancellationToken.None`, fire-and-forget with proper background-task handling)? This is exactly the kind of case the spec asks you to flag rather than decide silently.

**Verification/testing steps:** since this is hard to verify via curl alone, the spec wants proof via "a log line or a test" that a disconnected client aborts server work — e.g. add a temporary artificial delay + log statement, start a request, kill the curl process mid-flight (`curl --max-time 0.5` against an endpoint with an injected delay), confirm a `TaskCanceledException`/`OperationCanceledException` is logged, then remove the artificial delay.

**Expected commit message:** `B-12: Add CancellationToken to endpoints and outbound calls`

---

### B-13 · Standardize errors on ProblemDetails (RFC 9457) · Wave 5 · P2 · S

**Purpose:** one error shape for the whole API. `SubscribeController` currently returns a bare `bool`.

**Files expected to change:** `Backend/Controllers/SubscribeController.cs` (the main target), `Backend/Program.cs` (already has `AddProblemDetails()` from B-14 — just needs to be leveraged, not re-added).

**Current state (verbatim, unchanged since original):**
```csharp
[HttpPost("RegisterUser")]
public IActionResult RegisterNewUser([FromBody] SubscribeEntity subscribeEntity)
{
    bool isSuccessful = false;
    EmailMessage emailMessage = new EmailMessage { Subject = "...", Body = $"...{subscribeEntity.UserName}..." };
    isSuccessful = emailSender.SendMessage(emailMessage);
    return Ok(isSuccessful);
}
```
Both `RegisterNewUser` and `DeleteUser` follow this exact bare-bool-in-a-200 pattern.

**Implementation steps:**
1. Success → `200`/`201` with a meaningful body (not just `true`) — e.g. `{ "message": "Subscription request received" }` or similar; decide the exact shape with the user if it's user-facing (the Angular frontend currently reads this response — check `Frontend/src/app/services/api-call.service.ts` and whatever component calls `RegisterUser`/`DeleteUser` to see what shape it expects today, **since changing the response shape is a breaking change for the frontend** — this is worth flagging explicitly, since Frontend is out of scope for this backlog but consumes this exact contract).
2. `EmailSender.SendMessage` returning `false` today (SMTP failure) → what should the API return? The spec says duplicate subscription → 409, validation failure → 400 (already handled automatically by `required` + `[ApiController]` per B-03's verified behavior), but there's no real "duplicate" concept in this app currently (no persistence layer — every `RegisterUser` call just sends an email, there's no subscriber list to check against). **This mapping needs the user's input**: is an SMTP-send failure a `502 Bad Gateway` (upstream dependency failed), a `500`, or should it stay a "soft" `200` with a body indicating delivery status? Don't guess — ask.
3. `builder.Services.AddProblemDetails()` already exists in `Program.cs` from B-14 with a `traceId`-stamping customization — reuse it, don't duplicate.

**Verification/testing steps:**
1. Every error response is `application/problem+json` with `type`/`title`/`status`/`detail`/`traceId`.
2. No endpoint returns a bare primitive.
3. Confirm the B-14 exception handler still emits the same shape (regression check).
4. **Check the Frontend integration** — even though `Frontend/` is out of scope to *modify*, changing `SubscribeController`'s response shape without knowing what the Angular code expects risks silently breaking the live app. At minimum, read `Frontend/src/app/services/api-call.service.ts` and the component that calls it before changing the shape.

**Expected commit message:** `B-13: Standardize errors on ProblemDetails`

**Risks:** the biggest risk in this task is the Frontend contract — flagged above, twice, on purpose.

---

### B-17 · Add health checks · Wave 5 · P1 · S

**Purpose:** give a load balancer/orchestrator something to probe. Directly relevant to the Phase 1 infra spec's Render deployment (cold-start tolerance).

**Files expected to change:** `Backend/Program.cs`, possibly a new `Backend/HealthChecks/` folder with custom `IHealthCheck` implementations.

**Implementation steps:**
1. `/health/live` — trivial, no dependency checks: `app.MapHealthChecks("/health/live", new() { Predicate = _ => false });`
2. `/health/ready` — checks: **no database exists in this app** (confirmed repeatedly across this whole modernization effort — this is a static-JSON-file + SMTP-email app, not a DB-backed one), so "database reachable" from the spec's generic template doesn't apply. Substitute: (a) `TanakhData.json`/`TanakhStructure.json` present and parseable — a lightweight custom `IHealthCheck` that checks file existence (not a full parse, to keep it cheap) is enough; (b) email provider responsive — **this is genuinely tricky without sending a real test email or a fake SMTP handshake; recommend a short-timeout TCP-connect check to the configured SMTP host/port rather than a full auth handshake, and confirm this approach with the user before implementing**, since "responsive" for SMTP is ambiguous and a wrong implementation could either be too strict (false negative on every deploy) or meaningless (true even when broken).
3. Tag-based split per the spec's exact code sample.

**Verification/testing steps:**
1. Both endpoints return 200 when healthy.
2. `/health/ready` returns 503 when unhealthy — since there's no DB to "stop" for testing, simulate by temporarily renaming `Data/TanakhData.json` in a running instance (same technique used throughout B-03/B-08 testing) and confirming `/health/ready` goes to 503 while `/health/live` stays 200.
3. Confirm response bodies don't leak file paths, connection details, or exception info (spec's explicit requirement) — check this carefully, it's an easy thing to get wrong with default ASP.NET Core health check UI output.

**Expected commit message:** `B-17: Add health checks`

**Risks:** the SMTP-responsiveness check design — flagged above, ask before implementing a specific approach.

---

### B-15 · Introduce API versioning · Wave 6 · P3 · S

**Purpose:** move all routes under `/api/v1` before any external client depends on unversioned paths.

**Files expected to change:** `Backend/Tanakh.csproj` (add `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` if still on Controllers by this point), `Backend/Program.cs`, every controller's `[Route]` attribute (or Minimal API route registrations, depending on B-16's outcome).

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

### B-04: project split

**Option A — proceed with the spec's exact 4-project split** (`Tanakh.Domain`/`Tanakh.Infrastructure`/`Tanakh.Api`/`Tanakh.Tests`), as detailed in §9 above.
- **Pros:** matches the spec exactly; clean dependency-inversion boundary; sets up B-16's service-extraction work with a natural home for services; architecture test enforces the boundary going forward.
- **Cons:** for an app this small (5 controllers' worth of logic, no database, ~15 source files total), 4 projects is arguably heavier machinery than the problem needs; touches every file at once (highest blast radius of any remaining task); the `Model/` classes are genuinely ambiguous about which layer they belong to (see the open question flagged in §9 — are they domain entities or API contracts?).
- **My recommendation:** proceed with the split as specified, but **resolve the `Model/` placement question explicitly with the user first** — I'd lean toward: request/response DTOs (`TanakhContext`, `Book`) belong conceptually closer to `Tanakh.Api` (they're literally shaped by what the API returns), while `TanakhContainer`/`JewishCalendarContainer`/`TanakhStructure` (the raw external-API-mirroring shapes) belong in `Tanakh.Infrastructure` (they're deserialization DTOs for infrastructure concerns — reading a JSON file, calling hebcal.com — not domain concepts at all). This leaves `Tanakh.Domain` fairly thin (mostly the `ITanakhCache`/`IEmailSender`/repository-style contracts + maybe a genuinely domain-shaped `Chapter`/`Verse` if B-16's service extraction produces one) — which is fine and normal for an app this size.

**Option B — skip or defer the physical project split, keep logical separation via folders/namespaces only.**
- **Pros:** much lower risk, avoids touching every file at once, still gets meaningful benefit if B-16's service extraction happens first (folders like `Services/`, `Contracts/` inside the single project).
- **Cons:** doesn't satisfy the spec's literal definition of done (a real architecture test requires real project boundaries — you can't enforce "Domain has zero ASP.NET Core references" with folders alone, only with separate assemblies) — this would be a deviation from the given spec, not just an implementation-detail choice.
- **Not recommended** unless the user explicitly wants to descope this task.

**Ask the user: proceed with Option A (with the Model/ placement resolved as above), or something else?**

### B-16: Controllers vs Minimal APIs

**Option A — Minimal APIs with endpoint groups.**
- **Pros:** less ceremony, better raw throughput, natural fit with the service-layer extraction this task requires anyway, more idiomatic for a small greenfield-feeling API surface, aligns with .NET's current general direction.
- **Cons:** less familiar tooling for model binding/filters compared to Controllers (though for this app's simple `[FromBody]` binding needs, this is a non-issue); would mean re-touching every route again right after B-15 versions them (ordering consideration: doing B-16 before B-15 avoids double-touching routes).
- **My recommendation:** Minimal APIs — this app has no complex model binding, no filters beyond what's already centralized in `Program.cs` (CORS, exception handling), and 5 simple endpoints. Minimal APIs fit cleanly and match where ASP.NET Core is heading for APIs like this.

**Option B — keep Controllers, just clean them up** (extract business logic, split `TanakhController` by responsibility, keep `[ApiController]`/`ControllerBase`).
- **Pros:** smaller diff, more familiar to most .NET developers, zero risk of losing any Controller-specific behavior (e.g. automatic 400 on `required`-violation, which was directly verified working in B-03 and is a real, load-bearing behavior now — **verify this exact behavior still works identically under Minimal APIs before committing to Option A**, since Minimal API model binding has historically had some differences from Controller binding for validation-error auto-400 behavior).
- **Cons:** doesn't reduce ceremony; the spec frames Controllers as the "safer, more familiar" choice, implying Minimal APIs is the direction they'd prefer if the tradeoffs are acceptable.
- **Recommendation stands for Option A, but only after confirming the `required`-property auto-400 behavior (verified in B-03 for Controllers) carries over identically to Minimal API endpoint parameter binding** — this is a concrete, checkable thing to verify before committing to the choice, not just a preference call.

**Ask the user: Minimal APIs (my recommendation, pending the auto-400 verification above), or keep Controllers?**

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
