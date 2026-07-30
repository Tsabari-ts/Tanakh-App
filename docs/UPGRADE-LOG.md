# Upgrade Log — Phase 7 Frontend Modernization

Tracks what changed, what broke, and what was deferred for each task in `docs/TANACH-APP-FRONTEND-SPEC.md`.

## Baseline (2026-07-31)

| Item | Value |
|---|---|
| Node | v20.11.0 |
| npm | 10.3.0 |
| Angular CLI | 17.0.10 |
| Angular | 17.0.9 |
| @angular/material / cdk | 17.1.0 |
| rxjs | 7.8.1 |
| Other deps of note | `gematriya` (CommonJS, causes optimization-bailout warning), no `@ngrx/*`, no third-party UI kit beyond Angular Material |
| `git status` | clean |
| `ng build --configuration production` | **succeeds** with warnings only: initial bundle 623.03 kB (budget 500 kB), `subscribe`/`entrance` component CSS over 2kB budget, `gematriya` not ESM |
| `ng lint` | **not configured** — no ESLint/TSLint schematic installed, no lint architect target in angular.json. Spec assumes this exists; it doesn't. Noted as a gap, not fixed opportunistically (out of scope for F-06). |
| `ng test` | not run yet as part of baseline (will run per-task) |
| `npm audit --omit=dev` | 11 high severity, all stemming from `@angular/core <=19.2.25` (XSS/sanitization advisories) — expected to clear once F-01 lands |
| Initial bundle (prod) | main.js 511.58 kB raw / 121.77 kB transfer, styles 78.43 kB / 7.48 kB, polyfills 33.01 kB / 10.68 kB → **623.03 kB raw / 139.92 kB transfer total** |

### ⚠️ VERIFY corrections found during baseline check

- Spec says `api-call.service.ts` hardcodes `https://localhost:44308` in **5 places**. Actual count: **6** (`getHolidays`, `getVerses`, `getBookList`, `getBookByTitle`, `subscribe`, `updateReadingProgress`).
- No `src/environments/` folder exists yet at all (confirmed — F-06 creates it from scratch).
- `ServiceWorkerModule.register()` in `app.module.ts` currently gates on `!isDevMode()`, not an environment flag.
- No lint tooling present (see above).
- **Node version blocker for F-01, step 21→22:** current Node is v20.11.0; Angular 22 requires Node 22 LTS+. No nvm/volta/fnm found on this machine, only a single system Node install. This will need to be resolved (install Node 22, ideally via a version manager) before the final upgrade step — flagged for when F-01 reaches that point, not blocking now.
- `npx ng test --watch=false --browsers=ChromeHeadless` on unmodified `master`: **12 FAILED, 5 SUCCESS** (all `NullInjectorError`s — components like `WelcomeModalComponent`, `SettingsComponent`, `SubscribeComponent`, `ReadPermissionComponent` instantiated directly in specs without providing `MatDialogRef`/`AppComponent`/etc.). This is a pre-existing gap, not something F-06 introduced — confirmed by running the same command on `master` before any changes. Carried forward as a known baseline; will need attention independent of this spec (not opportunistic-refactored away per rule 5).

---

## F-06 · Add environment files + fileReplacements (2026-07-31)

**Branch:** `chore/f-06-environments`

**⚠️ VERIFY corrections** (see Baseline section above for full detail): actual hardcoded-URL count was 6, not 5; no `src/environments/` existed; `ServiceWorkerModule.register()` gated on `!isDevMode()` rather than an environment flag.

**Changes:**
- Created `Frontend/src/environments/environment.ts` (dev) and `environment.production.ts` (prod), per spec template, with `TODO(LAUNCH)` marker on the prod `apiUrl` placeholder.
- Added `fileReplacements` under the `production` build configuration in `angular.json`.
- Rewrote `api-call.service.ts` to read `environment.apiUrl` as `baseUrl` instead of hardcoding `https://localhost:44308` in each of the 6 methods.
- Switched `ServiceWorkerModule.register()` in `app.module.ts` from `enabled: !isDevMode()` to `enabled: environment.enableServiceWorker`, so the `enableServiceWorker` field is genuinely exercised by `fileReplacements` rather than being dead config (small deviation from the literal spec text, within F-06's stated intent — "those differences are real and testable now").
- Created `docs/LAUNCH-CHECKLIST.md` with item L-01.

**Verification:**
- `grep -rn "localhost:44308" src/` → only hits inside `src/environments/`. ✅
- `ng build --configuration production` → succeeds (same warnings as baseline: bundle budget, 2 component CSS budgets, gematriya CJS). ✅
- Confirmed `fileReplacements` is real, not just present: production build embeds `logLevel:"error"` / `enableServiceWorker:!0`; development build embeds `logLevel: "debug"` / `enableServiceWorker: false`. ✅
- `ng test --watch=false --browsers=ChromeHeadless` → 12 FAILED / 5 SUCCESS, identical to the `master` baseline (verified by running the same command on `master`). No regression. Pre-existing gap noted above, left untouched (not in scope for F-06).
- `ng lint` → not run; no lint tooling configured in this repo (see Baseline). Not introduced as part of F-06 (would be scope creep / opportunistic).
- `npm audit --omit=dev` → unchanged from baseline (11 high, all pre-existing Angular 17 advisories); F-06 touches no dependencies.

**Deferred / follow-up:** none. Task is self-contained.

---
