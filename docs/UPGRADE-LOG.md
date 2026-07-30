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

## F-05 · Migrate to the new control flow syntax (2026-07-31)

**Branch:** `feat/f-05-control-flow`

**⚠️ VERIFY finding:** `npx ng generate @angular/core:control-flow` reported "Nothing to be done" and `grep -rn "*ngIf\|*ngFor\|*ngSwitch"` across all templates returned nothing — the mandatory `control-flow-migration` that ran automatically during F-01's 20→21 step already converted every legacy template. This task's schematic step was a no-op; the work here is entirely the manual follow-up the spec calls out.

- **Fixed `track` expressions on 2 of the object-iterating `@for` blocks** where the migration had defaulted to tracking the whole object (`track button`, `track item`) rather than a real field, per the spec's explicit warning about this:
  - `home.component.html`: `tanakhButtons`/`buttons` are arrays of `{text, value, iconClass}` — `value` is the stable unique key (`'torah'`, `'prophets'`, `'settings'`, etc., also used for routing logic in `goTo()`). Changed `track button` → `track button.value`.
  - `booklist.component.html`: `data` holds `BookData[]` (checked `models/BookData.ts`) — `title` is the stable English-book-title key already used by `goTo(item)` for navigation. Changed `track item` → `track item.title`.
  - Left `track time` (`subscribe.component.html`, primitive strings) and `track word` (`entrance.component.html`, primitive strings, verified no duplicate values within any single slice) as-is — tracking a primitive value directly is correct, not the `$index` anti-pattern.
  - **One documented exception remains:** `chapterlist.component.html` still uses `track $index`. This is deliberate, not an oversight — the F-01 bug-fix already established `let chapterNumber = $index` is needed to derive the 1-based chapter number, `chapters` is a static array that's never reordered/filtered, and verse-count values aren't a safe/unique tracking key on their own (two chapters can share a verse count). `$index` is the correct choice here.
- **Added `@empty` blocks** to the two lists that come from an API call and can legitimately be empty:
  - `booklist.component.html`: `@empty { <p class="empty-state">לא נמצאו ספרים</p> }`.
  - `chapterlist.component.html`: restructured to drop the wrapping `@if (chapters)` (which was a no-op guard — `chapters: number[] = []`, and an empty array is truthy, so the `@if` never actually gated on emptiness) in favor of `@for (...) { } @empty { <p class="empty-state">לא נמצאו פרקים</p> }`, which is a strict improvement.
  - **Noted, not fixed (out of scope for this task):** since neither component has a distinct "loading" state, the `@empty` block will also flash briefly while the initial API response is in flight (an empty/`undefined` iterable looks identical to `@for` whether it's "still loading" or "genuinely no results"). This is the same ambiguity `*ngIf="!list.length"` would have had; not a regression introduced here, just carried forward. A proper loading-state signal is a natural F-03 (signals) concern, not this task's.
  - `home.component.html`'s two `@for` blocks (`tanakhButtons`, `buttons`) were **not** given `@empty` — they're hardcoded literal arrays in the component class, never empty by construction, so `@empty` would be dead code.
- **`CommonModule` sweep**: already clean — `grep -rn "CommonModule" src/app` returns nothing. The standalone-conversion schematic (F-02) had already imported the specific directives each component needs (`NgClass`, etc.) rather than the whole `CommonModule`, so there was nothing left to trim here.
- **`@if...as` sweep**: no `*ngIf="x as y"` patterns existed pre-migration, so nothing to convert to `@if (x; as y)`.
- `ng build --configuration production`: succeeds, same warning set (budgets unaffected by this task, `gematriya` CJS unrelated).
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS, unchanged.
- Final DoD sweep: `grep -rn "*ngIf\|*ngFor\|*ngSwitch"` → empty. ✅ Every `@for` uses a stable identifier except the one documented `$index` exception. ✅ Every API-backed list has `@empty`. ✅ No `CommonModule` left to remove. ✅

---

## F-09 · Set up lazy loading for routes (2026-07-31)

**Branch:** `feat/f-09-lazy-routes`

**⚠️ VERIFY correction:** the spec's example route table includes a `reminders` feature (`loadChildren: () => import('./features/reminders/reminders.routes')`). This frontend has **no reminders UI at all** — the Backend has reminder/subscription infrastructure (per git history: preference center, UTM tracking, admin dashboard for delivery/subscriber ops) but nothing in `Frontend/src/app` references it. Did not invent a route or a `reminders.routes.ts` file for a feature that doesn't exist in this codebase; converted only the 6 routes that actually exist (`entrance`, `home`, `settings`, `books/:section`, `books/:section/:book`, `books/:section/:book/:chapterNumber/:keepReading`).

- Converted every route in `app.routes.ts` from eager `component:` to `loadComponent: () => import(...).then(m => m.XComponent)`, added a Hebrew `title` to each (per spec note, matters for F-11/SEO later).
- Added `withPreloading(PreloadAllModules)` to `provideRouter(...)` in `app.config.ts`, per the spec's explicit recommendation for an app that's also used offline (small initial bundle, everything preloaded and cached shortly after).
- Route paths and parameter names are byte-for-byte unchanged — no redirects needed, nothing to break for any bookmarked/shared URL.
- `ng build --configuration production`: succeeds. **Initial bundle dropped from 669.29 kB to 363.84 kB raw / 71.47 kB transfer** — a ~46% reduction, and the initial-bundle budget warning (which had been present since before F-01) is now gone entirely, under the 500kB threshold for the first time in this log. Each route now emits its own named lazy chunk (`chapter-component`, `entrance-component`, `chapterlist-component`, `settings-component`, `home-component`, `booklist-component`, plus 2 shared vendor-ish chunks) — 8 chunk files total, confirmed via `ls dist/tanakh/browser/chunk-*.js`.
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS, unchanged.
- DoD sweep: every route uses `loadComponent`. ✅ Chunk-per-route confirmed. ✅ Initial bundle smaller (recorded both numbers above). ✅ All existing URLs unchanged (no redirects needed since nothing moved). ✅ Every route has a `title`. ✅

---

## F-14 · Fix the dead-end deep link (2026-07-31) — scope substantially revised, with user sign-off

**Branch:** `feat/f-14-deep-link-error-handling` (misnamed relative to final scope — kept for branch-per-task history, not renamed)

**⚠️ VERIFY finding — the spec's core premise does not hold in this codebase.** The spec describes `BookService` as an in-memory singleton populated only via prior `booklist` navigation, causing a chapter-list/chapter URL opened in a fresh tab to render blank. Checked the actual code before writing a single resolver:
- `BooklistComponent`, `ChapterlistComponent`, and `ChapterComponent` each independently call the API straight from `ActivatedRoute.params` inside their own `ngOnInit()`. None of them read state set by a *different* component or a *previous* navigation.
- `BookService` (`services/book.service.ts`) is a 20-line class holding one field (`chapter: number[]`); it is set and read only within the same synchronous callback inside `ChapterlistComponent.ngOnInit()` — confirmed via `grep -rn "bookService" src/` returning exactly one file.
- Checked the actual backend (`Backend/Tanakh.Api/Controllers/TanakhController.cs`): `GET /Tanakh/books/main/{book}` (single book by title) and `GET /Tanakh/books/{book}/{chapter}` (single chapter's text) both already exist and take no dependency on prior requests — this directly answers the spec's own open decision #3 ("does the API support fetching a single book/chapter by ID?") with **yes**, and confirms the frontend already uses exactly these endpoints today.
- There is **no `reminders` route or feature in this frontend at all** (backend-only so far, per git history — preference center, UTM tracking, admin dashboard). The spec's "F-14 unblocks the reminders feature" justification doesn't apply here.

**Surfaced this to the user before proceeding** (a plan-invalidating finding, not just a "larger than scoped" one) and got explicit direction: skip the resolver/cache architecture (it would solve a problem that doesn't exist here) and instead fix the one legitimate gap that *does* match the DoD — **no friendly error handling on a failed fetch**. Previously, an invalid book/section/chapter, or any API failure, just did `console.log(error)` and left the component's data field at its initial empty value; for `BooklistComponent`/`ChapterlistComponent` this now happens to render the F-05 `@empty` message (misleadingly labeled "not found" even for a genuine network error), and for `ChapterComponent` it rendered a fully blank page (no `@empty`-equivalent existed there — it's not a list).

**Changes:**
- Added `loadError = false` to all three components; set to `true` in both the `data.error` branch and the HTTP error callback of each fetch.
- `BooklistComponent` / `ChapterlistComponent`: template now branches `@if (loadError) { <error message> } @else { <existing @for/@empty content> }`, so a genuine fetch failure is now distinguishable from "the section/book legitimately has nothing in it."
- `ChapterComponent`: added a proper error branch with a "חזרה לדף הבית" (back to home) button, replacing what was a silently blank page (no title, no scroll controls, nothing) on any chapter-load failure.
  - **Found and fixed a second latent bug while wiring this up**: `returnToHomePage()` existed but was never called from any template (confirmed via grep) and navigated to `/homepage` — not a real route (the actual route is `/home`). Since I was about to make this method live for the first time (wiring it to the new error state's back button), fixing its route target was required for the new code to actually work, not tangential cleanup.
  - **Fixed a template nesting bug introduced by my own first-pass edit**: initially placed the `@else` block's closing `}` after the `centered-container` div's closing tag instead of before it, which would have left that div permanently unclosed whenever `loadError` was true. Caught by re-reading the diff structure before running the build; the build's clean pass then confirmed the corrected nesting compiles.
- Left the `@empty` message wording as-is in booklist/chapterlist (a pre-existing F-05 ambiguity between "loading" and "genuinely empty" that isn't new — see F-05's log entry); adding a real loading-state distinction is a natural F-03 (signals) concern, not this one.
- Did **not** attempt a full live end-to-end test (spin up the .NET backend + Postgres + apply migrations) — the Tanakh content endpoints don't need the database, but `Program.cs` registers `AddDbContextPool` unconditionally at startup, and setting up a throwaway Postgres instance just to click through a URL is disproportionate to what static analysis (reading both the Angular components and the actual controller) already answered unambiguously. Flagging this limitation rather than claiming a live-browser verification that didn't happen.
- `ng build --configuration production`: succeeds, bundle unchanged from F-09 (363.84 kB).
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS, unchanged.

**DoD status relative to the original spec (context: scope was revised, so not every line applies):**
- [N/A] Resolver-per-route architecture — not built, by direction, because the bug it would fix doesn't exist here.
- [x] Invalid ID / failed fetch produces a friendly Hebrew message rather than a blank screen, on all three affected screens.
- [x] No component depends on state set by a previous navigation (was already true, verified rather than assumed).
- [N/A] Automated resolver tests — no resolvers were written.

---

## F-03 · Move state management to Signals (2026-07-31)

**Branch:** `feat/f-03-signals`

**Approach:** converted only template-bound state that is mutated *outside* a template-originated event handler within the same component (HTTP `.subscribe()` callbacks, `setInterval`/`setTimeout` callbacks, cross-component field writes) — this is precisely the class of mutation that `OnPush` cannot see. Left plain fields alone where mutation only ever happens (a) once at construction/field-init time (before first render, always safe under any CD strategy), or (b) synchronously inside a DOM-event-triggered call chain (Angular always checks an `OnPush` component after an event originating from within it). Converting those to signals too would have been safe but pure churn with no OnPush-safety benefit — matches the ground rule against opportunistic scope growth.

**`BookService`:** `chapter: number[]` (plain field) → private `signal<number[]>([])` + public `.asReadonly()`. Kept the exact same `setBookData()`/`getBookChapter()` method signatures so the one call site (`ChapterlistComponent`) needed no interface changes, only that its internal storage is now signal-backed.

**Per component** (all 11 declared components), removed `ChangeDetectionStrategy.Eager` / `TODO(F-03)` in favor of `ChangeDetectionStrategy.OnPush`, and:

| Component | Converted to signal | Left as plain field (justified) |
|---|---|---|
| `ScrollToTopButtonComponent` | — | Everything — uses `Renderer2` direct DOM manipulation for its visibility toggle, never Angular template binding for that state; OnPush is a no-op change here. |
| `WelcomeModalComponent` | — | No mutable template-bound state at all. |
| `ReadPermissionComponent` | `isButtonDisabled`, `isSavedInProgress`, `isSavedSuccessful`, `progressValue` (all mutated inside `setInterval`/`setTimeout`) | `userHasConfirmedReading` — set once in the constructor from dialog data, never reassigned. |
| `SettingsComponent` | `subscribeButton` (mutated in a dialog's async `subscriptionStatusChange` callback) | `emailAddress`, `isPwaInstalled`, icon/button label strings — all constructor-time only. |
| `HomeComponent` | — | `tanakhButtons`/`buttons` are hardcoded literal arrays, `showButtons` is dead/unused; nothing here is mutated post-construction. |
| `AppComponent` | `showButton` — **the one genuinely architecturally interesting case**: this field is mutated from 5 *other* components' constructors via direct DI injection of `AppComponent` (`chapterlist`, `home`, `booklist`, `chapter`, `settings` each do `this.appComponent.showButton = ...`), not from any event inside `AppComponent` itself. This is exactly the cross-component-mutation pattern most likely to silently break under `OnPush`. Made it a plain public `signal(false)` (**not** `.asReadonly()`, deliberately — the other 5 components need `.set()` access, and wrapping it read-only would have broken their writes; this is a documented exception to the "public signals are readonly" convention, driven by the app's existing architecture of direct cross-component field access rather than a service-mediated store). Updated all 5 writer call sites to `.set(...)`. | `title`, `returnIcon` — static. |
| `EntranceComponent` | `isLoading`, `isHolidayOrShabat`, `shownWords` (all mutated inside the `getHolidays()` subscribe or the `showNextWord()` `setTimeout` recursion) | `words`, `currentIndex` — internal counters, never template-bound. |
| `SubscribeComponent` | `isButtonDisabled`, `isRequestInProgress`, `isRequestSuccessful`, `progressValue`, `serverResponse` (mutated inside nested `setTimeout`/`setInterval` chains after the subscribe API call) | `emailValue`/`displayNameValue`/`timeValue`/`skipShabbatHolidays`/`consentGiven` — deliberately **not** converted; these are bound via `[(ngModel)]` two-way binding, which already triggers change detection correctly under `OnPush` through Angular Forms' own `ControlValueAccessor` mechanism (DOM `input`/`change` events are genuine template-originated events). Converting them to signals would require splitting the banana-in-a-box syntax into `[ngModel]`/`(ngModelChange)` pairs for no OnPush-safety benefit — pure churn. `subscribeSuccessful`, `userHasSubscribed` — internal/constructor-time only, not template-bound. |
| `BooklistComponent` | `data`, `loadError` (both introduced/mutated in F-14, inside the `getBookList()` subscribe) | `section` — not template-bound. |
| `ChapterlistComponent` | `chapters`, `loadError` (mutated inside the `getBookByTitle()` subscribe) | `section`, `book` — not template-bound (used only to build API calls / navigation targets). |
| `ChapterComponent` | `title`, `data`, `loadError` (mutated inside `getVerses()` subscribes, both the initial load and `GetNextChapter()`) | `section`/`chapter`/`book`/`keepReading`/`nextChapter` — not template-bound. `isScrolling`/`isScrollingDown`/`isScrollingUp`/`clicks`/`speed`/etc. — scroll-button internal state, never read in the template (the buttons trigger imperative `contentContainer.nativeElement.scrollTop` mutation directly, bypassing Angular's rendering entirely, same reasoning as `ScrollToTopButtonComponent`). |

**`takeUntilDestroyed()` sweep:** every remaining `.subscribe()` on a **long-lived** observable (`ActivatedRoute.params`/`queryParams` — never complete on their own) or one that could plausibly outlive its subscribing component (`MatDialogRef` component-instance `EventEmitter` outputs) now pipes through `takeUntilDestroyed(this.destroyRef)`, with `DestroyRef` injected via the constructor and passed explicitly (required since several of these subscriptions are created lazily inside methods called from click handlers, not during field initialization/construction, where `takeUntilDestroyed()`'s no-argument form would throw — it needs an injection context or an explicit `DestroyRef`). Also added it to the HTTP `.subscribe()` calls themselves — technically these observables self-complete after one emission so it isn't strictly required for leak-prevention, but it guards against a real class of bug: an HTTP response arriving *after* the component (and its `@ViewChild` refs like `contentContainer`) has been destroyed, e.g. from a fast route navigation.

**One documented, deliberate exception:** `DialogService.openWelcomeDialog()`'s `dialogRef.afterClosed().subscribe(...)` was left untouched. This lives in a `providedIn: 'root'` singleton service, not a component — there is no meaningful "destroy" scope to tie it to (the service outlives the whole app), and `MatDialogRef.afterClosed()` is a one-shot observable that reliably completes after exactly one emission regardless. Adding `takeUntilDestroyed()` here would be inert, not defensive.

**Verification:**
- `ng build --configuration production` after every component (11 checkpoints, not just one at the end): succeeds throughout, no template/compile errors from any signal-call-syntax mismatch. Final bundle: 364.03 kB raw / 71.50 kB transfer — essentially unchanged from F-09/F-14 (signals don't add meaningful bundle weight).
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS at every checkpoint — identical to the F-14 baseline throughout. No regression introduced by the signals migration.
- Smoke test: served the production build, confirmed `/` loads with correct `<html dir="rtl" lang="he">`.
- DoD sweep: `grep -rl "BehaviorSubject"` → empty. `grep -rl "TODO(F-03)"` → empty. `grep -rl "ChangeDetectionStrategy.Eager"` → empty (all 11 now `OnPush`). No `effect()` used anywhere in this change, so "no effect() writes to a signal" is trivially satisfied. Every remaining `.subscribe()` either pipes `takeUntilDestroyed()` or has a documented reason not to (the one `DialogService` case above).
- **Not done, honestly flagged:** a full interactive click-through of every screen (the spec's own "careful smoke test... pay particular attention to prev/next chapter navigation") was not performed — this environment has no headed browser. Build success + unchanged test baseline + careful manual review of every mutation site against its template usage is the verification that was actually possible here; it is not a substitute for someone clicking through the real app before this ships, and that gap is being stated plainly rather than glossed over.

---

## F-04 · Enable zoneless change detection (2026-07-31)

**Branch:** `feat/f-04-zoneless`

Started only after F-03 was fully complete and merged, per the spec's hard dependency. This turned out much lower-risk than the spec anticipated precisely *because* F-03 already converted every template-bound field mutated inside an async callback (`.subscribe()`, `setTimeout`, `setInterval`) to a signal — signals are inherently zoneless-safe (they notify Angular's reactivity graph directly, independent of `NgZone`/zone.js monkey-patching), so the async-mutation risk class the spec warns about was already eliminated before this task started.

**Pre-flight sweep** (per spec step 4): `grep -rn "setTimeout|setInterval|addEventListener|requestAnimationFrame|NgZone|new Promise"` found 16 hits across `pwa-install.service.ts`, `chapter.component.ts`, `read-permission.component.ts`, `subscribe.component.ts`, `scroll-to-top-button.component.ts`, `entrance.component.ts`. Checked every one against its template:
- The `setInterval`/`setTimeout` callbacks in `read-permission`/`subscribe`/`entrance` all mutate state already converted to signals in F-03 — already zoneless-safe, nothing to change.
- `chapter.component.ts`'s scroll-button `setInterval`s and `scroll-to-top-button.component.ts`'s `addEventListener('scroll', ...)` mutate only imperative-DOM / non-template-bound state (`Renderer2.setStyle`, direct `scrollTop` assignment) — never read through Angular's template binding at all, so zoneless doesn't affect them either way.
- `pwa-install.service.ts`'s `addEventListener('beforeinstallprompt', ...)` mutates `deferredPrompt`/`isPwaInstalled`, neither of which is read by any template (confirmed via grep — `SettingsComponent` has its own separate, unrelated `isPwaInstalled` field read directly from `localStorage`). Left untouched; F-13 rebuilds this service properly per the spec's own plan.
- No `NgZone`, `ChangeDetectorRef`, or `runOutsideAngular` usage anywhere in `src/` (confirmed via grep) — nothing to remove.
- Third-party dependency check: only `gematriya` (pure synchronous function, no async/zone coupling) and `@angular/material`/`@angular/cdk` (officially zoneless-compatible since Angular 18, maintained by the Angular team itself) — no risk found.

**Changes:**
- `app.config.ts`: added `provideZonelessChangeDetection()` to the providers array.
- `angular.json`: removed the `"polyfills": ["zone.js"]` entry from the `build` architect target, and `"polyfills": ["zone.js", "zone.js/testing"]` from the `test` target.
- `package.json`: `npm uninstall zone.js`. It remains present in `node_modules` only as `@angular/core`'s own optional peer dependency (confirmed via `npm ls zone.js`) — expected and inert, since nothing in this app's polyfills or source imports it anymore.
- **All 14 `.spec.ts` files** (11 component specs, `app.component.spec.ts`, 3 service specs) updated to add `provideZonelessChangeDetection()` to their `TestBed.configureTestingModule({...})` providers — there is no shared/global test-setup file in this project (no `test.ts`; the karma builder handles `TestBed` initialization implicitly), so this had to be done per-file rather than centrally. All 14 follow one of two mechanically consistent patterns (`imports: [XComponent]` for components, `TestBed.configureTestingModule({})` for services), which kept the risk of this being 14 separate edits low.

**Verification:**
- `ng build --configuration production`: succeeds. **`polyfills.js` chunk is gone from the output entirely** (previously 34.59 kB raw). Initial bundle: 329.38 kB raw / 60.17 kB transfer, down from 364.03 kB / 71.50 kB — a **~35 kB reduction**, matching the spec's own "~30KB gzipped" estimate closely.
- `grep -rn "zone.js\|NgZone" src/`: empty. ✅
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 7 SUCCESS — identical to the F-03 baseline, same specific failures (pre-existing DI-setup gaps unrelated to this change). No new failures introduced by removing zone.js. ✅
- `npm audit --omit=dev`: 0 vulnerabilities. ✅
- Smoke test: served the production build, `/` loads with correct `<html dir="rtl" lang="he">`.
- **Not done, per explicit agreement with the user before starting this task:** the spec's own "exhaustive manual pass over every screen and interaction... specifically checked: async data loading, forms, navigation, spinners, timers, animations" — this is the change the spec itself flags as "most likely to introduce silent UI bugs," and it genuinely requires a human clicking through the real app in a real browser, which this environment cannot do. The user explicitly asked to proceed with implementation now and do that verification pass separately afterward. **This task should not be considered fully done until that manual pass happens** — flagging this prominently rather than claiming a false completion.

---

## F-07 · `HttpClient` with functional interceptors (2026-07-31)

**Branch:** `feat/f-07-interceptors`

- Created `NotificationService` (didn't exist — spec's "create if missing"), signal-based per the spec's template, with `showError()`/`showInfo()`/`dismiss()` and a 6-second auto-dismiss.
- Created `core/interceptors/error.interceptor.ts` and `core/interceptors/retry.interceptor.ts` following the spec's templates closely — this is also the first use of the `src/app/core/` directory the spec's §4 code conventions describe; it didn't exist before this task.
  - `errorInterceptor`: maps HTTP status → Hebrew message (same table as the spec), shows it via `NotificationService`, `console.error`s the failure for dev visibility, re-throws so callers can still react.
  - `retryInterceptor`: GET-only, retries twice with exponential backoff (500ms × 2^n), skips 4xx entirely (not worth retrying), matching the spec exactly.
- `app.config.ts`: replaced `withInterceptorsFromDi()` with `withInterceptors([retryInterceptor, errorInterceptor])` (order matters — retry runs first so a request can exhaust its retries before the error interceptor reports the final failure). `withInterceptorsFromDi()` was itself always a no-op in this codebase — there were zero class-based `HTTP_INTERCEPTORS` before this task, confirmed via grep, so nothing was silently dropped by removing it.
- Wired the notification into `app.component.html`/`.ts`: an RTL toast with `role="alert"` `aria-live="assertive"`, styled with logical CSS properties (`inset-inline`, not `left`/`right` — the existing `app.component.css` uses physical properties throughout, which is unrelated pre-existing code left for F-17's sweep, but new CSS written in this task follows the RTL-logical-properties rule from the start).
- **Removed the now-redundant `console.log(error)` calls in HTTP-level error callbacks** across `booklist`, `chapterlist`, `chapter` (×3: initial load, `GetNextChapter`, `reportReadingProgress`), `subscribe`, and `entrance` components — these were deliberately preserved through F-15/F-01/F-02/F-03/F-04 specifically *because* F-07 was where they'd get a real replacement (documented explicitly in the F-15 log entry). The interceptor's own `console.error` plus the visible toast now covers this. **Left every `console.log(data.error)` untouched** — those handle a distinct case (a 200 OK response whose JSON body signals a business-level error), which an HTTP interceptor structurally cannot see; conflating the two would have been a mistake, not a cleanup.
- **Interceptor tests using `HttpTestingController`**, per the DoD: `error.interceptor.spec.ts` (3 tests: known-status Hebrew message, unmapped-status fallback message, error still propagates to the caller) and `retry.interceptor.spec.ts` (3 tests: GET retries twice on 5xx then gives up, 4xx never retries, non-GET never retries).
  - **Caught a real problem while writing these**: my first draft of the retry-timing test used `fakeAsync`/`tick()`, Angular's standard tool for testing `setTimeout`/`timer()`-based code — but `fakeAsync` depends on zone.js's testing bundle (`zone.js/testing`), which F-04 removed from this project entirely a few tasks ago. Rewrote it to use real timers with `async`/`await` and a 10-second Jasmine timeout instead (the test genuinely waits ~3.2 real seconds for the exponential backoff) — slower, but it doesn't depend on infrastructure this project no longer has. Worth remembering for F-17 and any future test: zoneless removes `fakeAsync`/`tick()` as an option.
- `ng build --configuration production`: succeeds, 331.71 kB raw / 60.92 kB transfer (a few kB more than F-04's 329.38 kB — the interceptors/notification service add a small, expected amount of code), same pre-existing warning set.
- `ng test --watch=false --browsers=ChromeHeadless`: **23 total tests now (was 17) — 10 FAILED / 13 SUCCESS.** All 6 new interceptor tests pass; the 10 failures are the same pre-existing gap as every prior checkpoint, unchanged.
- Smoke test: served the production build, confirmed `/` loads with correct `<html dir="rtl" lang="he">`.
- **Local testing of the actual failure UX** (stopping the local API, wrong `apiUrl`, DevTools offline) was **not performed** — this environment has no running local backend instance and no browser to observe the toast rendering live. The interceptor's logic itself is covered by the `HttpTestingController` tests above, which is real but partial verification; seeing the toast actually render in a browser is still owed, consistent with the verification gaps already flagged for F-03/F-04.

---

## F-16 · Global error boundary + custom `ErrorHandler` (2026-07-31)

**Branch:** `feat/f-16-error-handler`

- Created `services/error-state.service.ts` — signal-based, a discriminated union (`{kind: 'none'} | {kind: 'fatal'} | {kind: 'reload'; message}`) rather than separate booleans, so the two error UI states can't both be true at once by construction.
- Created `core/global-error-handler.ts` following the spec's template closely: detects chunk-load failures by message pattern (new deploy while a tab is open — a reload fixes it) and routes those to a distinct "update available" prompt rather than the generic fatal screen; everything else shows the fatal screen. `report()` is an explicit no-op stub in non-production and a `TODO(LAUNCH)`-marked placeholder in production — no monitoring SDK installed, matching §0's rule against adding third-party accounts-required dependencies.
- Created `shared/error-screen/` (first use of the `shared/` directory the spec's conventions describe) — Hebrew heading, short explanation, two actions (retry / back to home) for the fatal case, a single reload action for the chunk-load case. **Deliberately uses a hard `document.location.href` navigation for "back to home" and `document.location.reload()` for retry, not the Angular `Router`** — the spec explicitly requires this screen to stay "functional even if the router itself has failed," and injecting `Router` here would defeat that guarantee if the router is precisely what's broken. No error object, message, or stack trace is ever rendered to the user — the fatal screen shows only a fixed, generic Hebrew string; the reload-prompt screen shows only the fixed string this codebase itself constructs (`'גרסה חדשה זמינה...'`), never anything from the caught error.
- `app.config.ts`: registered `provideBrowserGlobalErrorListeners()` (Angular's built-in `window.onerror`/`unhandledrejection` → `ErrorHandler` wiring — confirmed it exists and builds cleanly on v22, not just claimed) and `{ provide: ErrorHandler, useClass: GlobalErrorHandler }`.
- `app.component.ts`/`.html`: renders `<app-error-screen>` in place of `<router-outlet>` when `errorState.state().kind !== 'none'` — matches the spec's explicit "renders the error screen in place of router-outlet" wording (not just overlaid alongside it), so routed content stops being evaluated/rendered once a fatal error state is active.
- Added `docs/LAUNCH-CHECKLIST.md` item **L-04** for the unwired monitoring hook, matching the `TODO(LAUNCH)` marker in `global-error-handler.ts`. `grep -rn "TODO(LAUNCH)" Frontend/src/` now returns 2 hits (L-01 from F-06, L-04 from this task), both with a matching checklist row — satisfies §9's "every hit must correspond to a checklist row" rule.
- **Unit tests for `GlobalErrorHandler`** (3 tests, since the spec's own manual-testing DoD items — "throw an artificial error in a component," "`Promise.reject('test')`" — require a real running app in a real browser to observe, which isn't available here): an ordinary `Error` routes to the fatal state; a message matching the chunk-load pattern (`"Loading chunk 3 failed."`) routes to the reload-prompt state instead; a non-`Error` thrown value (a plain string) is handled without the handler itself throwing.
- `ng build --configuration production`: succeeds, 335.31 kB raw / 61.63 kB transfer (small, expected increase from F-04's 329.38 kB / F-07's 331.71 kB for the new error-handling code).
- `ng test --watch=false --browsers=ChromeHeadless`: **26 total tests now (was 23) — 10 FAILED / 16 SUCCESS.** All 3 new tests pass; same pre-existing 10 failures, unchanged.
- Smoke test: served the production build, confirmed `/` loads with correct `<html dir="rtl" lang="he">`.
- **Not performed, same category of gap as F-03/F-04/F-07:** actually triggering `provideBrowserGlobalErrorListeners()`'s live `window.onerror`/`unhandledrejection` wiring end-to-end (e.g. typing `throw new Error('test')` into a real browser console against the running app) was not done — this is Angular's own tested built-in wiring, not custom code, but seeing the friendly screen actually render in a browser instead of a white page is still owed.

---

## F-12 · Update and verify the service worker (2026-07-31)

**Branch:** `feat/f-12-service-worker`

- **`ngsw-config.json`**: added `dataGroups` — there were none before this task, only `assetGroups` (app shell + static assets), meaning the actual Tanakh API responses had no offline caching strategy at all.
  - `tanakh-content` (`performance` strategy, 365-day max age): matches `https://localhost:44308/Tanakh/books/**`, covering all three read endpoints this app actually calls (`getVerses`, `getBookList`, `getBookByTitle`) — cache-first is correct because, per the spec's own framing, biblical text never changes.
  - `dynamic-api` (`freshness` strategy, 1-hour max age): matches `https://localhost:44308/JewishCalendar/**`, the one endpoint whose answer genuinely changes daily (holiday/Shabbat check).
  - Deliberately did **not** include `/api/v1/**` (subscriptions, reading-progress) in either group — those are POST endpoints, not cacheable reads; the Angular service worker's `dataGroups` caching applies to GET requests, so including them would have been inert at best and misleading at worst.
  - **⚠️ VERIFY / known limitation, recorded rather than silently absorbed:** the URL patterns hardcode `https://localhost:44308` because that's the actual value of `environment.apiUrl` in both `environment.ts` and `environment.production.ts` right now (per F-06) — `ngsw-config.json` is a static JSON file processed at build time, not TypeScript, so it can't reference `environment.apiUrl` directly. When a real production API domain is chosen (`LAUNCH-CHECKLIST.md` item L-01), this file needs updating too, not just the environment files. Extended L-01's "Where" column to mention this file so it isn't missed later.
  - Checked for self-hosted Hebrew fonts needing `assetGroups` entries, per the spec's explicit warning — none exist; this app only uses Google Fonts CDN (Roboto, Material Icons, both cross-origin and not self-hosted), so that concern doesn't apply here.
- Created `core/app-update.service.ts` (`AppUpdateService`) following the spec's template: watches `SwUpdate.versionUpdates` for `VERSION_READY`, exposes `updateAvailable` as a signal, checks for updates every 6 hours, reloads on `unrecoverable`.
  - **Found and fixed a real DI regression while testing this**: `AppUpdateService` originally did `inject(SwUpdate)` (required) as a field initializer. `SwUpdate` is only provided when `provideServiceWorker()` is in the injector tree — which the *full* `app.config.ts` has, but almost none of the 26 existing component/service test specs do (they each configure a minimal `TestBed.configureTestingModule` with just the component under test). Since `AppComponent` now injects `AppUpdateService`, and five other components (`Booklist`, `Chapterlist`, `Chapter`, `Settings`, `Home`) inject `AppComponent` directly via constructor DI, this one field initializer transitively broke DI resolution everywhere `AppComponent` gets constructed — test count went from 10 FAILED/16 SUCCESS to **12 FAILED/14 SUCCESS**, a real regression, not just a "known pre-existing gap." Fixed by injecting `SwUpdate` as `{ optional: true }` and null-guarding every use — this is also just better service design (a service wrapping a possibly-unregistered platform feature should degrade gracefully rather than hard-crash anything that touches it), not merely a test workaround. Verified the fix brought the suite back to the 10/16 baseline exactly, then added 2 new tests for `AppUpdateService` confirming it doesn't throw when `SwUpdate` is absent.
- Wired `provideAppInitializer(() => inject(AppUpdateService).init())` into `app.config.ts` (the modern replacement for `APP_INITIALIZER`; F-01's log already confirmed nothing in this app needed the `provide-initializer` migration since no `APP_INITIALIZER` existed — this is the first thing in the app to use it).
- Wired the update banner into `app.component.html`/`.ts` — RTL, `role="status"`, single "רענן עכשיו" (refresh now) action, styled with logical CSS properties like the toast from F-07.
- `ng build --configuration production`: succeeds. 336.87 kB raw / 61.91 kB transfer (small expected growth from F-16's 335.31 kB). One **new** component-CSS budget warning (`app.component.css`, 2.14 kB vs 2.00 kB) from the added toast/banner styles — same category as the pre-existing `subscribe`/`entrance` warnings, deliberately not chased now since **F-10 is explicitly the task that re-tunes these budget thresholds** based on the app's real, current size.
- Confirmed `ngsw.json` in the build output actually contains both new data groups (`tanakh-content`, `dynamic-api`) — not just that `ngsw-config.json` parses, but that the CLI's config-to-manifest step picked it up.
- `ng test --watch=false --browsers=ChromeHeadless`: 28 total tests now (was 26) — 10 FAILED / 18 SUCCESS, both new `AppUpdateService` tests pass, baseline unchanged otherwise.
- Smoke test: served the production build; confirmed `/`, `/ngsw-worker.js`, and `/ngsw.json` all return 200.
- **Not performed — requires a real browser with DevTools, unavailable here:** offline chapter reload after a first visit, DevTools → Application → Service Workers (registered/active) and → Cache Storage (groups populated), and the full local-rebuild-and-observe-the-update-banner flow the spec describes. The service worker registration, config, and update-detection *logic* are in place and build correctly, but seeing them actually work in a browser is still owed — same category of gap as every task since F-03.

---

## F-13 · Fix `PwaInstallService` and the install experience (2026-07-31)

**Branch:** `feat/f-13-pwa-install`

**⚠️ VERIFY finding, flagged rather than fixed:** checked the actual icon assets referenced by `manifest.webmanifest` (`src/assets/icons/icon-*.png`, 8 files). They are the **unmodified default `@angular/pwa` schematic icons** — the Angular logo shield, never replaced with real Tanakh branding. Confirmed by viewing `icon-192x192.png` directly. This is a real gap for the actual install experience (a user's home screen would show the Angular logo, not anything representing "תנ\"ך"), but generating real brand artwork is a design decision, not something to fabricate here — doing so myself would mean inventing a visual identity for the project without the owner's input, which is out of bounds the same way inventing a domain name would be. **Not fixed; not silently left unmentioned either.** Did not add a `LAUNCH-CHECKLIST.md` row for this since it isn't blocked on infrastructure/accounts (§9's actual scope) — it's an open design task, worth tracking separately if the user wants it tracked at all.

- **`pwa-install.service.ts`**: rewritten per the spec's template — `canInstall`, `isIos`, `isStandalone` signals; `beforeinstallprompt`/`appinstalled` listeners moved from the constructor into an explicit `init()` method (matching the same pattern `AppUpdateService` established in F-12), wired via `provideAppInitializer(() => inject(PwaInstallService).init())` in `app.config.ts`. Declared a local `BeforeInstallPromptEvent` interface since TypeScript's DOM lib doesn't include this non-standard, Chromium-only event type. Kept the method name `installPWA()` (the spec's example calls it `install()`) since the existing call site already used that name — no reason to rename just to match an illustrative template.
  - Replaced the old `isPwaInstalled` (a plain field set once from `localStorage.getItem('pwaInstalled')`, only ever updated on a successful install, never re-verified) with the signal-based `isStandalone` detection the spec recommends (`display-mode: standalone` media query + iOS's `navigator.standalone`) — this actually reflects live reality on every load, including e.g. a user who installed the app, later uninstalled it, then revisits the site in a normal tab (the old field would have kept saying "already installed" forever; the new one won't).
  - Dropped `checkServiceWorkerStatus()` — confirmed via grep it was never called from anywhere, dead code that predates this task, and the `isStandalone` signal replaces what it was trying to approximate anyway.
- **`SettingsComponent`**: previously had its **own separate, disconnected** `isPwaInstalled` field also reading `localStorage` directly (not the service's) — the install button's label was static, computed once at construction, and clicking it on iOS (where `beforeinstallprompt` never fires) silently did nothing with no feedback. Rewired to read `pwaInstall.canInstall`/`isIos`/`isStandalone` directly, made `downloadAppButton` a `computed()` so the label updates reactively as install state actually changes (a real OnPush-safety concern per F-03's reasoning — this state can change after construction), disabled the button once `isStandalone()`, and added an iOS-only manual hint paragraph ("לחצו על כפתור השיתוף... הוספה למסך הבית") shown only when `isIos() && !isStandalone()`, per the spec's explicit guidance for the platform with no install prompt.
- **`manifest.webmanifest`**: added `description`, `dir: "rtl"`, `lang: "he"`, `categories`, `orientation: "portrait-primary"`. Changed `name`/`short_name` from the placeholder `"Tanakh"` to `"תנ\"ך"`. **`theme_color`/`background_color` changed from `#1976d2`/`#fafafa`** (Material blue / light gray — the literal `@angular/pwa` schematic defaults, not this app's actual palette) **to `#333333`/`#1a1a1a`**, matching the dark chrome color already used throughout the real app (title bar, footer, toasts, error screen) — per the spec's explicit instruction to take this from existing brand styles rather than inventing one. Left the icon list itself untouched (see the flagged gap above).
- **`index.html`**: added `<meta name="theme-color" content="#333333">`, `<link rel="apple-touch-icon">`, `apple-mobile-web-app-status-bar-style`, `apple-mobile-web-app-title` — all missing before this task. **Also fixed `<title>Web</title>` → `<title>תנ"ך</title>`** — a real, obvious pre-existing bug (the browser tab showed the literal placeholder text "Web") found and fixed while touching this exact section for PWA-metadata reasons, directly relevant to the install experience (the install prompt and home-screen label both draw from page/app title context).
- `ng build --configuration production`: succeeds, 337.80 kB raw / 62.06 kB transfer (negligible growth from F-12).
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 18 SUCCESS, unchanged.
- Smoke test: served the production build, confirmed `manifest.webmanifest` serves with all the new fields correctly populated.
- **Not performed, same category of gap as every task since F-03:** the spec's actual DoD items here are almost entirely live-device checks — a full Lighthouse PWA audit, an Android Chrome install-prompt test via `chrome://inspect` port forwarding, verifying maskable icons aren't cropped on [maskable.app](https://maskable.app), and iOS device testing (which the spec itself already defers to `LAUNCH-CHECKLIST.md` item L-03 for lack of a secure-context LAN setup). None of these are possible in this headless, browser-less environment. The code paths (signal state, computed label, conditional iOS hint, manifest fields) are implemented and build correctly; seeing them actually work on a phone is still owed.

---

## F-08 · Add `@defer` for heavy components (2026-07-31) — no code changes, closed as not-applicable

**Branch:** `feat/f-08-defer`

**⚠️ VERIFY finding:** the spec's own candidate table (advanced search, settings/preferences opened lazily, a reminders dialog, commentary/footnotes, an audio player, charts/statistics) is a list of *illustrative examples for a generic app*, not this one — none of those features exist in this codebase. Checked every actual component selector declared in `src/app/components/`:

| Component | Why it's not a `@defer` candidate |
|---|---|
| `EntranceComponent`, `HomeComponent`, `BooklistComponent`, `ChapterlistComponent`, `ChapterComponent`, `SettingsComponent` | Each is already its own route, already lazy-loaded via `loadComponent` since F-09 — that's the code-splitting boundary that matters for these, and `@defer` inside a route-level component would only split *within* an already-small (2.7–10 kB) chunk. |
| `ScrollToTopButtonComponent` | The only inline (non-route, non-dialog) child component in the app, used inside `ChapterComponent`'s template. It's small (a handful of CSS rules and one click handler) and is part of the immediate reading UI, not below-the-fold or non-critical content — deferring it would go against the spec's own explicit rule to never defer first-paint-adjacent content. |
| `WelcomeModalComponent`, `SubscribeComponent`, `ReadPermissionComponent` | Opened programmatically via `MatDialog.open(ComponentType)` from `DialogService`, never declared inline in any template with a `<app-x>` tag — there is nothing in a template for `@defer` (a template control-flow block) to wrap. `@defer` structurally cannot apply to dynamically-opened dialog content. |

**Related but genuinely different observation, not implemented here:** those three dialog components are listed directly in `app.config.ts`'s `providers` array (`WelcomeModalComponent, SubscribeComponent, ReadPermissionComponent`), which means they're eagerly bundled into the main initial chunk rather than split out, even though they're only ever needed when a dialog actually opens. The idiomatic fix for *that* is dynamic `import()` immediately before each `dialog.open(...)` call in `dialog.service.ts` — a legitimate, related code-splitting idea, but it is not `@defer` (a template syntax), and touching it would reopen the exact `app.config.ts` providers question F-02's log already flagged and deliberately left alone (those three components being listed as top-level providers looks like vestigial pre-Ivy `entryComponents` boilerplate, unrelated to F-08's scope). Noted here for whoever eventually does a bundle-size pass, not actioned.

**No code changes made.** Per rule 7 ("if a task turns out ... different [from the plan], stop and ask" was already exercised for F-14's premise mismatch; here the honest outcome is simpler — there is nothing to build, and forcing a `@defer` block around something that doesn't warrant one would be exactly the kind of change the ground rules warn against). Every DoD item is vacuously satisfied by having deferred nothing: no SEO-critical content is inside a `@defer` block (none exists), no layout shift from lazy content (none exists), bundle size is unaffected (confirmed no build changes were needed).

---

## F-10 · Set strict budgets in `angular.json` (2026-07-31)

**Branch:** `feat/f-10-budgets`

Per the spec's explicit instruction to measure before locking anything in, and given F-08/F-09 optimization is already done (F-09's lazy-loading landed a ~46% initial-bundle reduction; F-08 confirmed there's nothing further to split), took the "optimize first, then lock in the real target" path rather than "current + 5%."

**Measured real current usage** (fresh production build, no changes):
- `initial`: 337.80 kB raw / 62.06 kB transfer
- `allScript` (every JS chunk, initial + all lazy): computed by hand from the build output — 234.01 (main) + 273.69 + 9.95 + 7.08 + 5.22 + 4.31 + 3.80 + 2.84 + 0.731 (all lazy chunks) ≈ **541.63 kB raw**
- `anyComponentStyle`: max was `entrance.component.css` at 2.53 kB (also `app.component.css` 2.14 kB, `subscribe.component.css` 2.29 kB — all three were already over the *old* 2 kB threshold, generating warnings since before this task)
- `bundle` (styles.css + Material's prebuilt theme, combined initial CSS chunk): 103.79 kB raw

**Set, with real headroom against the numbers above, not arbitrary round numbers:**
```json
{ "type": "initial", "maximumWarning": "350kb", "maximumError": "500kb" }
{ "type": "allScript", "maximumWarning": "800kb", "maximumError": "1mb" }
{ "type": "anyComponentStyle", "maximumWarning": "4kb", "maximumError": "8kb" }
{ "type": "bundle", "name": "styles", "maximumWarning": "110kb", "maximumError": "150kb" }
```
- `initial`/`allScript`/`anyComponentStyle` match the spec's own suggested example values exactly — checked they weren't just copied blindly, they genuinely fit current usage with real (if not huge, for `initial`) margin.
- `bundle`/`styles`: the spec's suggested `100kB` warning would have **immediately failed** — current usage (103.79 kB) already exceeds it. Adjusted to `110kB` to give ~6kB of real headroom instead of quietly inflating past what's honest. The 103.79 kB figure is dominated by `@angular/material/prebuilt-themes/indigo-pink.css` (the whole-theme prebuilt CSS file, not a custom-tokens theme) — shrinking this further would mean moving to M3 design tokens with a custom theme, which is F-01's open decision #1, already noted there as moot until someone actually wants to invest in a redesign. Not attempted here.
- Result: **all three pre-existing component-CSS budget warnings, and the previous close-to-the-edge initial-budget situation, are gone** — confirmed via a clean rebuild with zero budget warnings (only the unrelated, pre-existing `gematriya` CJS notice remains).
- **Verified the enforcement mechanism itself actually works**, per the DoD ("artificially adding a heavy library makes the build fail, then revert"): temporarily dropped `initial`'s `maximumError` to `300kb` (below the real 337.80 kB), rebuilt, confirmed a genuine `[ERROR] bundle initial exceeded maximum budget` and "Application bundle generation failed" — not just a warning — then reverted the threshold back to `500kb` and rebuilt clean. This is a slightly different mechanism than "add a heavy library" but tests the identical code path (the budget checker's error-vs-pass boundary), with less risk of leaving stray dependencies behind.
- Added `"verify": "ng test --watch=false --browsers=ChromeHeadless && ng build --configuration production && npm audit --omit=dev --audit-level=high"` to `package.json`. **Deliberately omitted `ng lint`** from the spec's suggested script — this repo has no lint tooling configured at all (documented in the Baseline section of this log, unchanged since F-06); including it verbatim would make `npm run verify` fail immediately and unconditionally for everyone, which is worse than omitting a check that doesn't exist yet.
- Checked for existing CI (`.github/workflows/`) before adding anything — found `backend-ci.yml` and `backend-backup.yml`, **no frontend workflow**. Per the spec's explicit instruction ("if not, record CI setup as launch checklist item L-05; do not set one up now"), did not create a new GitHub Actions workflow. Added **L-05** to `docs/LAUNCH-CHECKLIST.md`.
- **Ran `npm run verify` end-to-end and confirmed its actual exit behavior**, not just that the script parses: it correctly exits non-zero (1) and correctly stops at the `ng test` step via `&&` short-circuiting — it never reaches `ng build`/`npm audit` in this repo's current state, because the **pre-existing** 10 failing tests (documented since the Baseline, unrelated to F-10 or any task in this log) make the first step fail. This is the *correct* behavior for a verify gate — it's supposed to fail when tests fail — but it does mean `npm run verify` can't be used as a green/red CI gate until that separate, already-tracked gap is addressed. Not fixed here; it predates this entire modernization effort and touching test infrastructure is explicitly out of scope per the ground rules ("Decline. Do not change test infrastructure mid-upgrade" — F-01's log).
- `ng build --configuration production`: succeeds, clean, zero budget warnings.
- `ng test --watch=false --browsers=ChromeHeadless`: 10 FAILED / 18 SUCCESS, unchanged (F-10 touches build config only, no source changes).
- Values and rationale recorded above, per the DoD's explicit requirement that this be documented in `UPGRADE-LOG.md` rather than just committed silently.

---
