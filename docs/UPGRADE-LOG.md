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

## F-15 · Remove dead code (2026-07-31)

**Branch:** `chore/f-15-dead-code`

**⚠️ VERIFY correction:** spec pointed at lines 166–169 for `LoadLocalStorage()`; actual location was lines 189–192 (line numbers drift, as the spec itself warned).

**Findings:**
- `ChapterComponent.LoadLocalStorage()` had zero callers anywhere in `src/` (grep confirmed only the definition, no references in `.ts` or `.html`). Compared line-by-line against `ReadPermissionComponent.saveSectionToLocalStorage()`: functionally equivalent (`localStorage['HasStorage']`/`localStorage.setItem(hasStorage, ...)` and a `section + " " + <next>` composed `SectionRef` string), and the latter is genuinely wired into the live flow (`ChapterComponent.finishedReading()` → `DialogService.openReadPermissionDialog()` → `ReadPermissionComponent.saveSectionToLocalStorage()`). Confirmed safe to delete per the spec's line-by-line comparison requirement.
- No `npx knip` / `npx ts-prune` run — neither is installed in this repo and installing either would be a new dependency, out of scope for a dead-code-removal task. Substituted manual sweeps (grep for symbol usage per component, per `console.log`) instead.
- No lint tooling exists (see Baseline) so "ESLint clean of no-unused-vars" from the DoD is not applicable as literally stated; verified via `ng build` (no unused-import compiler errors — `noUnusedLocals` isn't enabled in `tsconfig`, so this is a weaker guarantee than real lint, noted as a gap, not fixed here).
- All 10 components declared in `app.module.ts` confirmed reachable (via route config or `DialogService`) — none dead.
- Two zero-length files found: `scroll-to-top-button.component.css` (empty stylesheet, still referenced via `styleUrl` — normal, not dead) and `src/assets/.gitkeep` (intentional, keeps an otherwise-empty dir tracked). Neither removed.

**Changes:**
- Deleted `ChapterComponent.LoadLocalStorage()`.
- Removed 8 leftover debug-only `console.log` calls with no diagnostic value (`"return"`, `'is shabes'`, `"you already installed the app"`, install-prompt outcome messages, raw `this.data` dumps, `'Book:'/'Chapter:'` prints) across `app.component.ts`, `entrance.component.ts`, `settings.component.ts`, `pwa-install.service.ts`, `booklist.component.ts`, `chapter.component.ts`.
- **Deliberately left in place:** `console.log(error)` / `console.log(data.error)` calls inside HTTP `.subscribe()` error callbacks (in `chapter.component.ts`, `booklist.component.ts`, `chapterlist.component.ts`, `subscribe.component.ts`). These are the app's only current error visibility — removing them now would be a functional regression with no replacement, not a dead-code cleanup. F-07/F-16 replace this pattern properly with interceptors + `ErrorHandler`; touching it now would be exactly the kind of opportunistic scope-creep rule 5 warns against.

**Verification:**
- `ng build --configuration production` → succeeds, same warnings, bundle marginally smaller (622.66 kB vs 623.09 kB baseline). ✅
- `ng test --watch=false --browsers=ChromeHeadless` → 12 FAILED / 5 SUCCESS, identical to baseline. No regression. ✅
- Manual grep confirms zero remaining callers of `LoadLocalStorage` and zero remaining debug-only `console.log`s. ✅

---

## F-01 · Upgrade Angular 17 → 22 (2026-07-31 onward)

**Branch:** `chore/f-01-angular-upgrade`

Preparation: `git status` clean, `npm ci` clean install, baseline `ng build --configuration production` green (see Baseline section). `node -v` = v20.11.0, no version manager installed — flagged as a blocker for the 21→22 step specifically, not for 17→21.

### 17 → 18 (2026-07-31)

- `npx ng update @angular/core@18 @angular/cli@18`: core/cli/animations/common/compiler/forms/platform-browser(-dynamic)/router/service-worker → 18.2.14, `@angular-devkit/build-angular`/`@angular/cli` → 18.2.21, `typescript` → 5.5.4, `zone.js` → 0.14.10.
  - Automated migration replaced `HttpClientModule` with `provideHttpClient(withInterceptorsFromDi())` in `app.module.ts` — expected, matches the direction F-07 formalizes later.
  - "Migrate to new build system" optional migration: **not needed**, `angular.json` was already on the `@angular-devkit/build-angular:application` (esbuild) builder before this upgrade started.
- `npx ng update @angular/material@18`: material/cdk → 18.2.14. **No manual SCSS changes required** — ⚠️ VERIFY correction: the spec's biggest anticipated 17→18 pain point (M2→M3 Sass function renames, `mat.define-light-theme` → `mat.m2-define-light-theme`) does not apply to this app. It has no custom Material theme file; it only imports the prebuilt `@angular/material/prebuilt-themes/indigo-pink.css`, so there is no `mat.define-*`/`mat.get-*` call anywhere in the codebase (confirmed via grep). Open decision #1 (stay on M2 vs move to M3 tokens) is therefore moot for now — nothing to decide until a custom theme is introduced.
- `ng build --configuration production`: succeeds. Bundle grew slightly (666.45 kB raw / 148.30 kB transfer vs 622.66 kB / 139.81 kB baseline after F-15) — same 3 warning types as before (initial budget, 2 component-CSS budgets, `gematriya` CJS). Not investigated further; budget tuning is F-10's job, later in the sequence.
- `ng test --watch=false --browsers=ChromeHeadless`: 12 FAILED / 5 SUCCESS — identical to the pre-upgrade baseline, no new failures.
- Smoke test: served `dist/tanakh/browser` via `http-server` on `localhost:8080`. `/` returns 200 with correct `<html dir="rtl" lang="he">` and loads. Deep route `/books/main` returns 404 — this is `http-server` not doing SPA fallback (no `-P` catch-all flag used), not an app regression; it's the same class of issue F-14 already exists to fix architecturally. Full interactive browser testing (click-through navigation, offline, responsive) was **not** performed — this environment has no headed browser available; build/test/curl-level verification substitutes for it at each intermediate version step. A full manual pass will be done once the upgrade sequence (and F-14) land.
- `npm audit --omit=dev`: not yet re-checked at this intermediate step; will check after the full 17→22 sequence completes, since intermediate versions are stepping stones, not a shipped state.
- Committed as a single `chore(F-01): upgrade Angular to v18` (core/cli and material updates squashed together, since `ng update` required a clean tree between the two commands but they represent one logical version step per the spec).

### 18 → 19 (2026-07-31)

- `npx ng update @angular/core@19 @angular/cli@19`: core/cli/etc → 19.2.25/19.2.27, `zone.js` → 0.15.1.
  - As documented in the spec: automated migration added `standalone: false` to all 11 non-standalone declarations (`app.component.ts` and 10 components). **Expected and correct** — F-02 reverses this.
  - `@angular/ssr` import-path migration and workspace-config-deprecation migration both ran with "No changes made" (app doesn't use SSR yet, no deprecated angular.json options present).
  - Declined the two *optional* migrations offered (`use-application-builder` — already on it; `provide-initializer` for `APP_INITIALIZER`/etc — app doesn't use those tokens, nothing to migrate).
- `npx ng update @angular/material@19`: material/cdk → 19.2.19. No manual changes (same reason as v18 — no custom theme SCSS).
- `ng build --configuration production`: succeeds, 663.21 kB raw / 148.71 kB transfer, same warning set.
- `ng test --watch=false --browsers=ChromeHeadless`: 12 FAILED / 5 SUCCESS, unchanged.
- Committed as a single `chore(F-01): upgrade Angular to v19`.

### 19 → 20 (2026-07-31) — Node upgrade required mid-sequence

**⚠️ VERIFY correction, real blocker (not anticipated by the spec):** the spec's Node-22-requirement warning was attached only to the final 21→22 step. In practice, `npx ng update @angular/core@20` refused to run: *"The Angular CLI requires a minimum Node.js version of v20.19 or v22.12"* — this machine had v20.11.0. The blocker landed two steps earlier than the spec expected.

Resolved by installing `nvm-windows` via `winget install --id CoreyButler.NVMforWindows` (per explicit user choice — asked first since this changes machine-wide state, not just the repo) and `nvm install 22` → Node v22.23.2. `nvm use 22.23.2` updates the machine PATH permanently (new terminal sessions pick it up automatically); the existing Bash tool session's cached environment needed `export PATH="/c/nvm4w/nodejs:$PATH"` prepended per command for the rest of this session. The old Node 20.11.0 install at `C:\Program Files\nodejs` was left untouched, so nothing else on this machine is affected. `node_modules` was reinstalled clean (`rm -rf node_modules && npm install`) under Node 22 before continuing.

- `npx ng update @angular/core@20 @angular/cli@20`: core/cli/etc → 20.3.27/20.3.32, `typescript` → 5.9.3.
  - Automated (required) migrations: workspace generation defaults preserved in `angular.json`; `tsconfig.json` `moduleResolution` → `bundler`; SSR-related import migrations ran with no changes (app doesn't use SSR yet).
  - **Declined both optional migrations offered:** `use-application-builder` (already on it) and, importantly, `control-flow-migration` (converts templates to `@if`/`@for` block syntax) — this is explicitly F-05's job later in the sequence, run only after F-02 (standalone) per the dependency graph. Running it now would jump ahead of the plan. Also declined `router-current-navigation` (no usage of the deprecated `Router.getCurrentNavigation()` in this codebase).
- `npx ng update @angular/material@20`: material/cdk → 20.2.14. No source changes (only touched stale files under `dist/`, which is gitignored build output from earlier smoke-testing — not committed).
- `ng build --configuration production`: succeeds, 683.88 kB raw / 150.63 kB transfer, same warning set (budgets, gematriya CJS).
- `ng test --watch=false --browsers=ChromeHeadless`: 12 FAILED / 5 SUCCESS, same count as baseline (error format changed cosmetically to Angular's newer `NG0201` style, same underlying pre-existing DI-setup gap in the specs).
- `npm audit --omit=dev`: **0 vulnerabilities** (down from 11 high at baseline) — the Angular 17 sanitization/XSS advisories are resolved as of this version.
- Committed as a single `chore(F-01): upgrade Angular to v20`.

---
