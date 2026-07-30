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

### 20 → 21 (2026-07-31)

**⚠️ VERIFY correction:** `ng update`'s suggestion table listed `@angular/material` → `21.3.0-next.0` as "the" update, but checked against the full npm version list: **Material never published a stable `21.3.0`** — its stable line tops out at `21.2.14`, and the next stable release is `22.0.0`. `21.3.0-next.0` is a prerelease that happened to sort as "newest 21.x". Pinned explicitly to `ng update @angular/material@21.2.14` instead of trusting the suggested command, to avoid landing a prerelease package in a real dependency tree. (`@angular/cdk` did not have this issue — it has a full stable `21.0.0`–`21.2.14` line matching Material's.)

- `npx ng update @angular/core@21 @angular/cli@21`: core/cli/etc → 21.2.19.
  - Required migrations: `tsconfig.json` `lib` bumped to a modern target; `main.ts` bootstrap options migrated to the new `bootstrapModule(AppModule, { applicationProviders: [...] })` form.
  - **The `control-flow-migration` (old `*ngIf`/`*ngFor`/`*ngSwitch` → `@if`/`@for`/`@switch`) ran as a *required*, non-skippable migration this time** — not optional, unlike at v20. This converted 8 template files automatically. This is effectively F-05's core schematic work landing two steps early as an unavoidable side effect of the mandatory v21 migration; the manual follow-up F-05 still owns (verifying every `@for` `track` uses a real stable identifier rather than the object itself, adding `@empty` blocks, trimming now-unnecessary `CommonModule` imports) is deferred to F-05's own task slot, not done here.
  - Declined the one remaining optional migration (`router-current-navigation` — no `Router.getCurrentNavigation()` usage in this codebase).
- **Found and fixed a real, pre-existing latent bug surfaced by the migration** (not introduced by it): `chapterlist.component.html` used `*ngFor="let versesNumber of chapters; 'index + 1' as chapterNumber"`. That `'index + 1' as chapterNumber` is invalid NgFor microsyntax — `'index + 1'` is a quoted string literal, not a reference to NgFor's `index` export, so `chapterNumber` was **always `undefined`** at runtime on every Angular version before this one (17 through 20 all built and "worked" only because the old template compiler didn't type-check structural-directive microsyntax aliases strictly enough to catch it). The automated `@for` codemod dropped the (non-functional) alias entirely and left bare `chapterNumber` references in the button's `(click)` and `{{ }}` bindings, which the new, stricter `@for` compiler correctly flagged as `TS2339: Property 'chapterNumber' does not exist` — turning a silent runtime bug into a hard build error.
  - Fix: `@for (versesNumber of chapters; track $index; let chapterNumber = $index)`, then `chapterNumber + 1` at both use sites (`goToChapter(chapterNumber + 1)`, `getChapterName(chapterNumber + 1)`). The `+ 1` is unambiguous from context: `chapters: number[]` holds each chapter's verse count (`ChapterlistComponent.chapters = this.bookService.getBookChapter()`), so array index `i` corresponds to 1-based chapter number `i + 1`; `getChapterName()` renders it via `gematriya()` (Hebrew numeral) and `goToChapter()` navigates to `/books/:section/:book/:chapterNumber/:keepReading`. This is exactly the kind of change the ground rules say to flag rather than silently ship: it's a **behavior change** (the "go to chapter" button and chapter-number label were effectively broken — navigating to an `undefined` chapter route and rendering an empty/NaN gematria label — and now work correctly), even though the fix itself was unambiguous given the surrounding code. Also changed `track versesNumber` → `track $index` at the same spot while already touching the line, since verse-count values aren't a safe tracking key (two chapters can have the same verse count) — a minimal, justified touch-up, not scope creep, done only because the line was already being edited to fix the compile error.
  - Checked all other 7 migrated templates for the same class of dropped-alias bug (diffed each against pre-migration, grepped for `as ` / `let X =` patterns) — none found; `chapterlist.component.html` was the only file using this non-standard microsyntax. The clean production build after the fix is itself strong evidence no other template has a similar break (Angular's strict template type-checking would have caught it the same way).
- `ng build --configuration production`: succeeds after the fix, 688.41 kB raw / 151.87 kB transfer, same warning set.
- `ng test --watch=false --browsers=ChromeHeadless`: **10 FAILED / 7 SUCCESS** — an *improvement* over the 12/5 baseline (2 more tests now pass), plausibly a side effect of the chapterlist fix. No regressions.
- `npm audit --omit=dev`: 0 vulnerabilities.
- Committed as a single `chore(F-01): upgrade Angular to v21`.

### 21 → 22 (2026-07-31) — final step, `OnPush` becomes the default

- `npx ng update @angular/core@22 @angular/cli@22`: core/cli/etc → 22.1.0/22.1.2, `typescript` → 6.0.3.
  - **`OnPush`-by-default stopgap handled automatically, and better than the spec anticipated.** The spec expected to have to manually add `changeDetection: ChangeDetectionStrategy.Default` with a hand-written `TODO(F-03)` comment to every component that broke. Instead, Angular's own `ng update` migration ("Adds `ChangeDetectionStrategy.Eager` to all components") did this automatically and blanket-applied it to all 11 non-standalone components before any of them could actually break — `Eager` is Angular 22's real, first-class compatibility value for "preserve the pre-v22 default (non-OnPush) behavior," not a hack. Manually added `// TODO(F-03): remove after signals migration` to all 11 occurrences afterward (via a scripted sed pass across the 11 files) so F-03 can find and clear every one, per the spec's DoD requirement.
  - Other required migrations: `withXhr` added to the `provideHttpClient()` call in `app.module.ts`; `nullishCoalescingNotNullable`/`optionalChainNotNullable` extended diagnostics disabled in `tsconfig.app.json`/`tsconfig.spec.json` (both no-op for this codebase — neither pattern in use, this only suppresses stricter checks introduced in the same release); `canMatch` third-argument and duplicate-outputs/optional-chaining migrations all ran with no changes (none of those patterns exist here).
  - **Declined both optional migrations**: `migrate-karma-to-vitest` (spec explicitly says decline — don't change test infra mid-upgrade) and `use-application-builder` (already on it since before this upgrade started).
  - ⚠️ VERIFY: checked for the v22 hardened-sanitization concern on dynamic `[href]`/`[xlink:href]`/`bypassSecurityTrust*` — none exist in this codebase (grep returned no matches), so that risk doesn't apply here.
- `npx ng update @angular/material@22`: material/cdk → 22.1.0 — this time a genuine stable target (unlike the v21 step), no manual changes.
- `ng build --configuration production`: succeeds, 708.56 kB raw / 157.03 kB transfer, same warning set as every prior step (initial budget, 2 component-CSS budgets, gematriya CJS — none of these are new, all pre-date the upgrade and are F-10's job).
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS, unchanged from v21. No regression.
- `npm audit --omit=dev`: **0 vulnerabilities.** ✅ (Full audit including devDependencies shows 21, entirely in Karma/socket.io-adapter test tooling that never ships to production — out of scope for this task's DoD, which is explicitly `--omit=dev`.)
- `npx ng version`: confirms Angular 22.1.0 across every `@angular/*` package. ✅
- Committed as a single `chore(F-01): upgrade Angular to v22`.

### F-01 summary

All 5 version-step commits (`angular-v18` … `angular-v22`) are tagged on `chore/f-01-angular-upgrade`. Every Definition of Done item is satisfied:
- `ng version` reports 22.x. ✅
- Production build succeeds with no errors, same pre-existing warning set throughout (bundle/CSS budgets are F-10's job; `gematriya` CJS is a third-party package concern, not urgent). ✅
- `npm audit --omit=dev`: 0 vulnerabilities (started at 11 high). ✅
- Tests: went from 12 FAILED/5 SUCCESS baseline to 10 FAILED/7 SUCCESS — improved, no regressions introduced by the upgrade itself. The remaining 10 failures are a **pre-existing gap** (specs instantiate components like `WelcomeModalComponent`/`SettingsComponent`/`SubscribeComponent` without providing `MatDialogRef`/`ActivatedRoute`/`AppComponent` etc.) — not caused by, or fixed by, this upgrade; flagged as a known gap, deliberately not opportunistically fixed here (not in scope for F-01).
- Manual smoke testing was necessarily partial in this environment (no headed browser available) — verified via build + curl-level checks at each step (page loads, correct `dir="rtl" lang="he"`, no compile errors). A full interactive pass (click-through nav, offline, responsive, back/forward) is still owed once F-02/F-14 land and a real browser session can be used; noted here rather than claimed as done.
- Every `TODO(F-03)` stopgap (`ChangeDetectionStrategy.Eager`, 11 occurrences) is in place and grep-able. ✅
- **One real, pre-existing bug found and fixed** (not scope creep — required to get the mandatory v21 control-flow migration to compile): the chapterlist "go to chapter" button and chapter-number label were silently broken (always navigating/rendering with `undefined`) due to invalid legacy NgFor microsyntax. Fixed with the obviously-intended `$index + 1` semantics. Flagged prominently since it's a user-visible behavior change, even though the fix itself was unambiguous.
- **Real infrastructure blocker surfaced and resolved with sign-off:** Node 20.11.0 was too old for Angular CLI 20+ (not just 22, as the spec assumed) — installed `nvm-windows` + Node 22.23.2 after asking the user to choose the approach, since it's a machine-wide change beyond this repo. Old Node 20 install left untouched.
- **Material never has a stable release exactly matching every Angular core minor** — verified against real npm version lists at each step rather than trusting `ng update`'s suggested command blindly (would have pulled a `21.3.0-next.0` prerelease at the 21 step had it not been checked).

**Not yet done, by design (belongs to later tasks in the sequence):** standalone conversion (F-02), the remaining manual `@for`/`@empty`/`CommonModule` cleanup from the control-flow migration (F-05), removing the `ChangeDetectionStrategy.Eager` stopgaps in favor of real signals (F-03), zoneless (F-04), bundle-budget tuning (F-10).

---

## F-02 · Convert to standalone components (2026-07-31)

**Branch:** `feat/f-02-standalone`

- Ran the 3-mode schematic in order, with a build+test checkpoint and commit after each:
  1. `--mode=convert-to-standalone`: 11 components + `app.module.ts` + 11 spec files updated.
  2. `--mode=prune-ng-modules`: "Nothing to be done" — this app only ever had a single root `AppModule`, no purely-declarative feature modules to prune.
  3. `--mode=standalone-bootstrap`: deleted `app.module.ts`; `main.ts` rewritten to `bootstrapApplication()` with everything (routes, legacy modules via `importProvidersFrom`) inlined.
  - **Found:** the standalone-conversion schematics silently strip trailing same-line comments when they rewrite a file's AST — all 11 `// TODO(F-03)` tags added during F-01 were gone after step 1. Re-applied them via a scripted sed pass (same pattern as F-01), verified 11/11 present again.
- **Manual follow-up** (per spec, done after the schematic):
  - Created `app.config.ts` as the single source of global providers and `app.routes.ts` holding the (still-eager, non-lazy) route table extracted from `main.ts` — lazy `loadComponent` conversion is F-09's job, not this one.
  - Replaced `importProvidersFrom(BrowserModule, ...)` with idiomatic standalone providers: dropped `BrowserModule` (redundant under `bootstrapApplication`), `BrowserAnimationsModule` → `provideAnimations()`, `AppRoutingModule` → `provideRouter(routes, withComponentInputBinding())` (the `withComponentInputBinding()` is added now per the spec's explicit note that F-14 needs it later — inert until then), `ServiceWorkerModule.register(...)` → `provideServiceWorker(...)`, kept `provideHttpClient(withXhr(), withInterceptorsFromDi())` as-is (F-07 replaces `withInterceptorsFromDi()` with real functional interceptors later).
  - **Verified `MatDialogModule`/`MatIconModule` were safe to drop globally**, not just assumed: `MatDialog` is `providedIn: 'root'` (checked `dialog.service.ts` — injected via plain constructor DI, no module needed), and the `convert-to-standalone` schematic had *already* given each dialog component (`welcome-modal`, `subscribe`, `read-permission`) its own fine-grained imports of the specific standalone directives it uses (`MatDialogTitle`, `MatIcon`, `CdkScrollable`, etc.) rather than the whole NgModule — better than the spec's own example expected.
  - Kept `WelcomeModalComponent, SubscribeComponent, ReadPermissionComponent` listed directly as `providers` in `app.config.ts`, matching what was already present (oddly) in the original `app.module.ts` before any of this migration started. This predates F-02 and looks like vestigial pre-Ivy `entryComponents`-era boilerplate (components don't need explicit providing to be dialog content under Ivy), but removing it is opportunistic cleanup outside F-02's scope — left untouched, noted here for whoever eventually does a provider-list audit.
  - Found and deleted `app-routing.module.ts` — orphaned by this task's own changes (nothing references `AppRoutingModule` once `app.routes.ts` + `provideRouter` replaced it); this is dead code created by F-02 itself, not pre-existing, so removing it is in scope (not an F-15 violation).
  - No `SharedModule`/`MaterialModule` barrel existed to dismantle (confirmed via `find`).
- `ng build --configuration production`: succeeds. **Bundle shrank meaningfully — 669.09 kB raw / 148.72 kB transfer, down from 708.56 kB / 157.03 kB at the end of F-01** (~39 kB less), from dropping the unnecessary blanket `BrowserModule`/`MatDialogModule`/`MatIconModule` imports. Satisfies the DoD requirement that bundle size not grow — it shrank instead.
- `ng build --configuration development`: also succeeds (2.45 MB, expected for unminified dev output) — confirms `ng serve`'s config path still works.
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS throughout every sub-step, unchanged from the F-01 baseline.
- Smoke test: served the production build, `/` returns 200 with correct `<html dir="rtl" lang="he">`.
- Final DoD sweep: `grep -rl "NgModule"` → empty. `grep -rl "standalone: false"` → empty. `grep -rl "TODO(F-03)"` → 11/11 present. No barrel modules. ✅ All satisfied.

---
