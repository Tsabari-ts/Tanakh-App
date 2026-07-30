# Implementation Spec — Phase 7: Angular 17 → 22 Upgrade + Modern Frontend Architecture

**Project:** Tanach app (Angular + PWA, Hebrew/RTL UI)
**Audience:** Claude Code — execute one task at a time
**Date:** July 2026
**Stated current state:** Angular 17, NgModule-based, `BookService` in-memory singleton, API hardcoded to `https://localhost:44308`

---

## 0. Scope: local-first

**There is no production domain and no hosting yet.** Every task in this spec is written to be **completable and verifiable entirely on localhost**. Nothing here requires a deployed environment.

Work that genuinely cannot be finished without a domain/host is **not** silently skipped — it is implemented up to the point where only a value or a credential is missing, and that missing piece is recorded in `docs/LAUNCH-CHECKLIST.md` (see §9). The goal is: when a domain finally exists, going live is a config change, not a refactor.

**Rules that follow from this:**

- Never invent a domain, hostname, API key, or hosting provider. Use the documented placeholder convention (§5, F-06).
- A production build must **build and run locally** — that is the acceptance bar, not "reachable on the internet."
- Anything blocked on infrastructure gets a `TODO(LAUNCH):` marker in code **and** a row in `docs/LAUNCH-CHECKLIST.md`. Both, always.
- Do not add third-party SDKs (error monitoring, analytics) that require an account. Build the integration point, leave it unwired.

---

## 1. How to use this document (instructions for Claude Code)

> **Read this section before every task.**

1. **One task at a time.** Do not start a new task until the previous task's Definition of Done is fully green.
2. **Atomic commit per task.** Format: `feat(F-06): add environment files and fileReplacements`. Never mix two tasks in one commit.
3. **Branch per task:** `chore/f-01-angular-18`, `feat/f-03-signals`, etc. Never work directly on `main`.
4. **Read the existing code before changing it.** This document was written without repository access. Every item marked `⚠️ VERIFY` is an assumption that must be checked against the real code — and the plan corrected if it is wrong.
5. **No opportunistic refactoring.** If you notice an F-14 problem while doing F-05, write it into `NOTES.md` and move on.
6. **Run the Verification Gate (§4) after every task.** If anything fails, fix it before committing, or `git reset` and retry.
7. **If a task turns out larger than scoped, or touches more than ~20 files, stop and ask.** Do not proceed on an educated guess.
8. **Logging:** after each completed task, append a row to `docs/UPGRADE-LOG.md` — what changed, what broke, what was deferred.

---

## 2. Project context and assumptions

### 2.1 What is known

| Area | State |
|---|---|
| Angular | 17 |
| Architecture | NgModule-based (not standalone) |
| State management | `BookService` — plain mutable fields, not reactive |
| HTTP | `api-call.service.ts` — URL hardcoded in 5 places |
| PWA | Service worker + `PwaInstallService` exist |
| Language | Hebrew only, RTL |
| Content | Biblical text — **static, never changes** |
| Hosting | **None yet.** Local development only. |

### 2.2 Assumptions to verify first ⚠️ VERIFY

Before starting anything, run and record:

```bash
node -v                          # Angular 22 requires Node 22 LTS+
npm -v
npx ng version                   # confirm actual Angular / CLI / Material versions
cat package.json                 # full third-party dependency list
npx ng build                     # does the baseline even build today?
git status                       # must be clean
```

Record results in `docs/UPGRADE-LOG.md` under "Baseline."

**Potential upgrade blockers** — scan `package.json` and check, for every dependency, whether an Angular 22-compatible version exists:

- `@angular/material` / `@angular/cdk` — the most painful one (see F-01)
- Third-party UI libraries (ngx-*, primeng, ng-bootstrap)
- `@ngrx/*` if present
- Anything with a peer dependency pinned to Angular 17

If any library has **no** Angular 22 support, that is a separate decision: replace it, fork it, or drop it. Do not start the upgrade before that question is answered.

### 2.3 Version milestones that matter here

| Version | What happened that affects us |
|---|---|
| **v18** | Material 3 went stable; the old M2 functions were renamed to `mat.m2-*`. Zoneless experimental. |
| **v19** | Standalone became the default — the automatic migration adds `standalone: false` to every legacy declaration. |
| **v20** | Zoneless became stable (as of 20.2). Removal of long-deprecated APIs. |
| **v21** | Zoneless by default in new apps. Vitest as test runner. Signal Forms. |
| **v22** (June 2026) | **`OnPush` became the default change detection strategy.** `resource()` / `httpResource()` / Signal Forms are stable. Hardened sanitization and `platform-server` security. |

> ⚠️ **The single biggest consequence of v22 for this app: `OnPush` by default.** An app that relies on mutating plain objects — exactly what `BookService` does today — **will visibly break**. This is precisely why F-03 (signals) is not a nice-to-have but a hard prerequisite.

---

## 3. Execution order and dependencies

### 3.1 Dependency graph

```
F-06 (environments)  ──┐
                       ├──► do first, before touching the upgrade
F-15 (dead code)     ──┘

F-01 (17→22)
  ├──► F-02 (standalone)
  │      ├──► F-05 (control flow)
  │      ├──► F-09 (lazy routes) ──► F-08 (@defer)
  │      │                       └──► F-10 (budgets)
  │      └──► F-03 (signals)
  │             └──► F-04 (zoneless)
  ├──► F-07 (interceptors) ──► F-16 (ErrorHandler)
  ├──► F-12 (service worker) ──► F-13 (PWA install)
  └──► F-11 (SSR/prerender)  [needs F-09 + F-14]

F-14 (deep link)  ──► needs F-06 + F-09, and shapes F-11
F-17 (i18n)       ──► last, once templates have stabilised
```

### 3.2 Recommended order

| # | Task | Why here |
|---|---|---|
| 1 | **F-06** environments | P0, fully independent, removes hardcoded URLs. Do before touching the upgrade. |
| 2 | **F-15** dead code | Removes noise before automated migrations run — less code to fix. |
| 3 | **F-01** upgrade 17→22 | Foundation for everything else. |
| 4 | **F-02** standalone | Automated migration, do as early as possible. |
| 5 | **F-05** control flow | Schematic, cheap, reduces template noise. |
| 6 | **F-09** lazy routes | Routing infrastructure, needed before F-14. |
| 7 | **F-14** deep link resolver | P0 — unblocks the reminders feature. |
| 8 | **F-03** signals | Most important technically — fixes change detection under OnPush. |
| 9 | **F-04** zoneless | Immediately after F-03. |
| 10 | **F-07** interceptors | |
| 11 | **F-16** ErrorHandler | Completes F-07. |
| 12 | **F-12** service worker | |
| 13 | **F-13** PWA install | |
| 14 | **F-08** `@defer` | Optimisation — only once structure is stable. |
| 15 | **F-10** budgets | Locks in the gains from 8/9/14. |
| 16 | **F-11** SSR/prerender | Large architectural decision, near the end. |
| 17 | **F-17** i18n | Last. |

---

## 4. Ground rules

### Forbidden / required

**Forbidden:**
- ❌ `any` in new code. If there is no type, define one.
- ❌ Manual `subscribe()` in a component without `takeUntilDestroyed()` or `toSignal()`.
- ❌ Business logic in templates. A derived value is a `computed()`.
- ❌ `document.querySelector` or manual DOM manipulation. Use `@ViewChild` / signals.
- ❌ Hebrew strings hardcoded in TS files (see F-17). In templates this is acceptable for now.
- ❌ Changing business logic during a technical migration.
- ❌ Inventing a domain, host, or credential. See §0.

**Required:**
- ✅ Every new component is standalone with `changeDetection: OnPush` declared explicitly (still worth stating after v22, for clarity).
- ✅ Every HTTP call goes through `api-call.service.ts` — no direct `HttpClient` use in components.
- ✅ Every external URL comes from `environment`.
- ✅ RTL support is preserved: `dir="rtl"`, `lang="he"`, and logical CSS properties (`margin-inline-start`, not `margin-left`).
- ✅ Accessibility: every interactive element gets a Hebrew `aria-label`.

### Code conventions

```
src/app/
  core/          # singleton services, interceptors, guards, resolvers, ErrorHandler
  features/      # feature areas, each owning its routes
    books/
    chapter/
    reminders/
  shared/        # shared standalone components / pipes / directives
  models/        # interfaces and types
environments/
```

- File naming: `kebab-case.type.ts` (`book.service.ts`, `chapter.component.ts`)
- Public signals: always `readonly`, exposed via `.asReadonly()` when backed by a `WritableSignal`
- Interfaces: `PascalCase`, no `I` prefix

---

## 5. Verification Gate — run after every task

```bash
# 1. Compilation + types
npx ng build --configuration production

# 2. Lint
npx ng lint

# 3. Tests
npx ng test --watch=false --browsers=ChromeHeadless

# 4. Security
npm audit --omit=dev          # zero High/Critical

# 5. Bundle size — record the number in UPGRADE-LOG
npx ng build --configuration production --stats-json
```

**Serving a production build locally** (needed for F-12, F-13, F-11 — `ng serve` will not do):

```bash
npx ng build --configuration production
npx http-server dist/<app>/browser -p 8080 -c-1
# http://localhost:8080 is a secure context, so service workers and PWA install both work
```

**Manual smoke test (mandatory — the compiler will not catch these):**

| # | Check | Pass criterion |
|---|---|---|
| 1 | Load home page | Loads, RTL correct, no console errors |
| 2 | Navigate: home → book list → book → chapter | Text renders fully, with nikud ⚠️ VERIFY |
| 3 | **Deep link**: copy a chapter URL, open in a new tab | Screen loads with data (this is F-14) |
| 4 | Refresh (F5) on every screen | Does not break |
| 5 | Browser back/forward | State is correct |
| 6 | Offline (DevTools → Network → Offline) | A previously read chapter still loads |
| 7 | Responsive at 360px width | No horizontal overflow |

---

# 6. Task specifications

---

## F-06 · Add environment files + `fileReplacements`

**Priority:** P0 · **Size:** S · **Depends on:** nothing

### The problem
`api-call.service.ts` contains `https://localhost:44308` in 5 places. The URL is baked into the source, so there is no way to point the app anywhere else without editing code.

### Local-first framing
The point of this task **is not** to ship to production — it is to make the API URL a single configurable value. The production config file gets created now with an explicit placeholder, so that when a domain exists, going live is a one-line change.

### Steps

**1. Map every occurrence:**

```bash
grep -rn "localhost:44308" src/ --include="*.ts" --include="*.html" --include="*.json"
grep -rniE "https?://" src/ --include="*.ts" | grep -v "w3.org\|schemas.microsoft"
```

⚠️ The brief says 5 occurrences in `api-call.service.ts` — verify nothing else hardcodes a URL.

**2. Create the files.**

`src/environments/environment.ts` (development — the default, and the only one currently used):

```ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:44308',
  enableServiceWorker: false,
  logLevel: 'debug' as const,
};
```

`src/environments/environment.production.ts`:

```ts
// TODO(LAUNCH): apiUrl is a placeholder — no production domain exists yet.
// Until a real API host is chosen, the production build points at the local API
// so that `ng build --configuration production` remains fully testable on localhost.
// See docs/LAUNCH-CHECKLIST.md, item L-01.
export const environment = {
  production: true,
  apiUrl: 'https://localhost:44308',
  enableServiceWorker: true,
  logLevel: 'error' as const,
};
```

> **Do not invent a domain.** Pointing the production config at the local API is deliberate: it keeps the production build runnable and verifiable today, and the `TODO(LAUNCH)` marker plus the checklist row make sure it is not forgotten.
>
> Note what *does* differ between the two configs: `production`, `enableServiceWorker`, and `logLevel`. Those differences are real and testable now — which means `fileReplacements` is genuinely exercised rather than being dead configuration.

**3. `angular.json` — add under `projects.<name>.architect.build.configurations.production`:**

```json
"fileReplacements": [
  {
    "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.production.ts"
  }
]
```

**4. Replace usage in code:**

```ts
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiCallService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getBooks() {
    return this.http.get<Book[]>(`${this.baseUrl}/api/books`);   // ⚠️ VERIFY endpoint
  }
}
```

**5. Confirm `environment*.ts` is not in `.gitignore`,** and that neither file contains secrets — these are bundled into public client code, so API keys never belong here.

**6. Create `docs/LAUNCH-CHECKLIST.md`** with its first entry (template in §9).

### Definition of Done
- [ ] `grep -rn "localhost:44308" src/` returns nothing outside `src/environments/`
- [ ] `ng build --configuration production` succeeds
- [ ] The production build genuinely uses `environment.production.ts` — verify by temporarily changing `logLevel` and confirming the change appears in `dist/`
- [ ] `ng serve` still works against the local API
- [ ] `docs/LAUNCH-CHECKLIST.md` exists and contains item L-01 (production API URL)

### Rollback
Single `git revert`. Nothing depends on it upstream.

---

## F-15 · Remove dead code

**Priority:** P2 · **Size:** S · **Depends on:** nothing

### Steps

1. Open `ChapterComponent`, locate `LoadLocalStorage()` (around lines 166–169 ⚠️ VERIFY — line numbers drift).
2. Search for callers: `grep -rn "LoadLocalStorage" src/`
3. **Before deleting** — confirm the logic really is implemented in `ReadPermissionComponent`. Read both methods and compare line by line. If there is any functional difference, **do not delete**; note it in `NOTES.md` and report.
4. Delete, then run a broader sweep:

```bash
npx knip                      # or ts-prune — finds unused exports/members
npx ng lint
```

5. Also remove: unused imports, leftover `console.log` calls, zero-length files, components not referenced by any route or template.

### Definition of Done
- [ ] `LoadLocalStorage()` deleted, with the equivalent logic verified present in `ReadPermissionComponent`
- [ ] ESLint clean of `no-unused-vars` / `@typescript-eslint/no-unused-vars`
- [ ] All smoke tests pass — especially reading-position persistence

---

## F-01 · Upgrade Angular 17 → 22, one major at a time

**Priority:** P0 · **Size:** L · **Depends on:** F-06, F-15 (recommended)

> **This is the highest-risk task in the document. Do not take shortcuts.**

### Hard rule
**Do not jump straight to 22.** Angular supports migrating one major version at a time, and the automated schematics only run on the adjacent step. Skipping versions forfeits every automatic migration.

### Preparation

```bash
git checkout -b chore/f-01-angular-upgrade
git status                    # must be clean
node -v                       # Angular 22 requires Node 22 LTS+ ⚠️ VERIFY against angular.dev
npm ci                        # clean install
npx ng build --configuration production   # green baseline
```

Record in `docs/UPGRADE-LOG.md`: baseline bundle size, passing test count, build time.

### The loop — repeat 5 times (18, 19, 20, 21, 22)

For each version `N` in `[18, 19, 20, 21, 22]`:

```bash
# 1. Read the official migration guide before each step
#    https://angular.dev/update-guide  (N-1 → N, complexity: advanced)

# 2. Upgrade core + cli
npx ng update @angular/core@N @angular/cli@N

# 3. Upgrade material/cdk to the same version (if present)
npx ng update @angular/material@N

# 4. Related packages
npx ng update @angular/pwa@N   # ⚠️ VERIFY — package name may differ

# 5. Build
npx ng build --configuration production

# 6. Test
npx ng test --watch=false --browsers=ChromeHeadless

# 7. Full manual smoke test (§5)

# 8. Only if everything is green:
git add -A && git commit -m "chore(F-01): upgrade Angular to vN"
git tag angular-vN
```

**If a step fails:** fix it on that version. **Never move to the next version with a broken build.** If blocked for more than an hour, `git reset --hard` to the previous tag, document the failure, and ask.

### Expected pain points per version

**17 → 18**
- **Angular Material 3.** The hardest part of the whole upgrade. The old M2 functions were renamed: `mat.define-light-theme` → `mat.m2-define-light-theme`, `mat.define-palette` → `mat.m2-define-palette`.
  - **Decision required:** stay on M2 with the new names (fast, conservative), or move to M3 tokens (`mat.define-theme`, role-based system).
  - **Recommendation:** at this stage, stay on M2 and only rename. Moving to M3 is a separate design project, not part of a technical upgrade. Open it as F-01b.
  - Target file: ⚠️ VERIFY — most likely `src/styles.scss` or `src/theme.scss`
- Deprecation warnings for `HttpClientModule` (moving to `provideHttpClient()` in F-07).

**18 → 19**
- Standalone becomes the default. The migration will automatically add `standalone: false` to every existing component. **This is expected and correct** — F-02 reverses it.
- `@angular/material` — deprecations in the `*-overrides` mixins; `--sys-*` → `--mat-sys-*` in custom SCSS.

**19 → 20**
- Removal of long-deprecated APIs. Run `npx ng update` and read every warning carefully.
- Zoneless goes stable (20.2) — do not enable it yet, that is F-04.

**20 → 21**
- New apps are zoneless by default; ours stays on `zone.js` until F-04. Confirm `provideZoneChangeDetection()` or the `zone.js` polyfill is still explicitly configured.
- Test runner: a Karma → Vitest migration may be offered. **Decline.** Do not change test infrastructure mid-upgrade. Log it as future work.

**21 → 22**
- 🔴 **`OnPush` becomes the default change detection strategy.** This is the most dangerous change for this app, because `BookService` is a plain mutable object.
  - **Expected symptom:** screens that do not update after data loads; lists that stay empty until you click something.
  - **Immediate stopgap:** explicitly set `changeDetection: ChangeDetectionStrategy.Default` on the components that broke, each with `// TODO(F-03): remove after signals migration`.
  - **Real fix:** F-03.
- Hardened sanitization on `href` / `xlink:href` — if the app renders links or SVG dynamically, check nothing is being stripped.
- `resource()` / `httpResource()` / Signal Forms are now stable. **Do not adopt them yet**; consider in F-03/F-07.

### Definition of Done
- [ ] `ng version` reports Angular 22.x
- [ ] `ng build --configuration production` succeeds with no errors and no new warnings
- [ ] `npm audit --omit=dev` reports zero High/Critical
- [ ] All tests pass
- [ ] All 7 smoke tests pass
- [ ] 5 commits and 5 tags in history (`angular-v18` … `angular-v22`)
- [ ] `docs/UPGRADE-LOG.md` records, per version, what broke and how it was fixed
- [ ] Every stopgap `ChangeDetectionStrategy.Default` is marked `TODO(F-03)`

---

## F-02 · Convert to standalone components

**Priority:** P1 · **Size:** M · **Depends on:** F-01

### Background
Standalone has been the default since v19. After F-01, every component carries `standalone: false` — an explicit marker that it is legacy.

### Steps — three modes, in order, **with a commit between each**

```bash
# Step 1: convert declarations to standalone
npx ng generate @angular/core:standalone --mode=convert-to-standalone
npx ng build && npx ng test --watch=false
git commit -am "refactor(F-02): convert declarations to standalone"

# Step 2: remove unnecessary NgModules
npx ng generate @angular/core:standalone --mode=prune-ng-modules
npx ng build && npx ng test --watch=false
git commit -am "refactor(F-02): prune unnecessary NgModules"

# Step 3: switch to standalone bootstrapping
npx ng generate @angular/core:standalone --mode=standalone-bootstrap
npx ng build && npx ng test --watch=false
git commit -am "refactor(F-02): switch to standalone bootstrapping"
```

**Run a full manual smoke test between each step.** The migration can break DI silently.

### Manual work remaining after the schematic

**1. `app.config.ts`** — create or clean up:

```ts
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(
      routes,
      withComponentInputBinding(),                       // important for F-14
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
    provideHttpClient(withFetch()),                      // interceptors added in F-07
  ],
};
```

**2. `main.ts`:**

```ts
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig)
  .catch(err => console.error(err));
```

**3. Trim bloated `imports` arrays.** The schematic tends to add `CommonModule` to every component. Once the new control flow lands (F-05) most of those imports are unnecessary — clean up after F-05.

**4. `SharedModule` / `MaterialModule`** — if barrel modules exist, dismantle them. Instead of a `MaterialModule` importing 20 modules, each component imports only what it uses (`MatButtonModule`, `MatIconModule`).

### Definition of Done
- [ ] No `NgModule` remains, except (possibly) routing modules kept deliberately and documented
- [ ] `main.ts` uses `bootstrapApplication`
- [ ] `app.config.ts` is the single source of global providers
- [ ] No `standalone: false` anywhere
- [ ] Bundle size has **not** grown versus baseline (record in the log)
- [ ] Smoke tests pass

---

## F-05 · Migrate to the new control flow syntax

**Priority:** P2 · **Size:** S · **Depends on:** F-02

### Steps

```bash
npx ng generate @angular/core:control-flow
npx ng build && npx ng test --watch=false
```

### Manual work after the schematic

**1. `@for` requires `track` — this is performance-critical.** The schematic defaults to `track $index`, which is poor. Replace with a stable identifier:

```html
<!-- bad -->
@for (chapter of chapters(); track $index) { ... }

<!-- good -->
@for (chapter of chapters(); track chapter.id) { ... }

<!-- for verses, if there is no id: -->
@for (verse of verses(); track verse.number) { ... }
```

⚠️ VERIFY — check the actual identifier field on the `Chapter` / `Verse` models.

**2. Add `@empty`** to every list, replacing separate `*ngIf="!list.length"` blocks:

```html
@for (book of books(); track book.id) {
  <app-book-card [book]="book" />
} @empty {
  <p class="empty-state">לא נמצאו ספרים</p>
}
```

**3. `@if` with `as`** instead of `*ngIf="x as y"`:

```html
@if (currentBook(); as book) {
  <h1>{{ book.name }}</h1>
}
```

**4. Remove `CommonModule`** from the imports of any component no longer using `NgIf`/`NgFor`/`NgSwitch`. (`AsyncPipe`, `DatePipe` etc. still need their specific imports.)

**5. Verify:**
```bash
grep -rn "\*ngIf\|\*ngFor\|\*ngSwitch\|ngSwitchCase" src/ --include="*.html"
```

### Definition of Done
- [ ] The grep above returns nothing
- [ ] Every `@for` uses `track` with a stable identifier (not `$index`), except documented exceptions
- [ ] Every list that can be empty has an `@empty` block
- [ ] `CommonModule` removed where no longer needed
- [ ] Smoke tests pass

---

## F-09 · Set up lazy loading for routes

**Priority:** P1 · **Size:** S · **Depends on:** F-02

### Steps

`app.routes.ts`:

```ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
    title: 'תנ"ך',
  },
  {
    path: 'books',
    loadComponent: () => import('./features/books/book-list.component').then(m => m.BookListComponent),
    title: 'ספרי התנ"ך',
  },
  {
    path: 'books/:bookId',
    loadComponent: () => import('./features/books/chapter-list.component').then(m => m.ChapterListComponent),
    resolve: { book: bookResolver },              // F-14
    title: bookTitleResolver,                     // F-14
  },
  {
    path: 'books/:bookId/chapters/:chapterId',
    loadComponent: () => import('./features/chapter/chapter.component').then(m => m.ChapterComponent),
    resolve: { chapter: chapterResolver },        // F-14
  },
  {
    path: 'reminders',
    loadChildren: () => import('./features/reminders/reminders.routes').then(m => m.REMINDERS_ROUTES),
  },
  { path: '**', loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) },
];
```

⚠️ VERIFY — the existing route structure and parameter names. **Preserve existing URLs** if any have been shared or bookmarked; if they must change, add redirects.

**Preloading** — in `app.config.ts`:

```ts
import { provideRouter, withPreloading, PreloadAllModules } from '@angular/router';

provideRouter(routes, withPreloading(PreloadAllModules))
```

> For an app that is also consumed offline, `PreloadAllModules` makes sense: the initial bundle stays small, but after first load everything is cached. **Record the decision.**

### Definition of Done
- [ ] Every route uses `loadComponent` / `loadChildren`
- [ ] `ls dist/*/browser/chunk-*.js` — each route in its own chunk
- [ ] Initial bundle smaller than before this task (record both numbers)
- [ ] All existing URLs still work, or redirects exist
- [ ] Every route has a `title` (matters for SEO — F-11)

---

## F-14 · Fix the dead-end deep link

**Priority:** P0 · **Size:** M · **Depends on:** F-06, F-09 · **Blocks the reminders feature**

### The problem
`BookService` is an in-memory singleton populated **only** when navigating through `booklist`. Landing directly on a chapter-list URL — from a reminder, a bookmark, a WhatsApp share, a search result — yields an empty screen. This is an architectural flaw, not a local bug.

### Principle
**The URL is the single source of truth.** No screen may assume prior in-memory state. A resolver loads what is needed from the URL parameters.

### Steps

**1. Resolvers (`core/resolvers/book.resolver.ts`):**

```ts
import { inject } from '@angular/core';
import { ResolveFn, Router } from '@angular/router';
import { catchError, EMPTY } from 'rxjs';
import { BookService } from '../services/book.service';

export const bookResolver: ResolveFn<Book> = (route) => {
  const bookService = inject(BookService);
  const router = inject(Router);
  const bookId = route.paramMap.get('bookId')!;

  return bookService.loadBook(bookId).pipe(
    catchError(() => {
      router.navigate(['/books'], { queryParams: { error: 'book-not-found' } });
      return EMPTY;
    }),
  );
};
```

`chapterResolver` follows the same pattern, loading **both** book and chapter — landing directly on a chapter still needs the book context for the title and prev/next navigation.

**2. `BookService` — cache instead of one-shot state:**

```ts
@Injectable({ providedIn: 'root' })
export class BookService {
  private readonly api = inject(ApiCallService);
  private readonly cache = new Map<string, Book>();

  loadBook(bookId: string): Observable<Book> {
    const cached = this.cache.get(bookId);
    if (cached) return of(cached);

    return this.api.getBook(bookId).pipe(
      tap(book => this.cache.set(bookId, book)),
    );
  }
}
```

> Biblical text never changes, so a cache with no TTL is correct here. After F-03, replace the `Map` with a signal.

**3. Components read from the resolver, not from service state.** With `withComponentInputBinding()` (set up in F-02) this is clean:

```ts
@Component({ /* ... */ })
export class ChapterComponent {
  // bound directly from resolve data — no subscribe
  readonly chapter = input.required<Chapter>();
  readonly book = input.required<Book>();
}
```

**4. Find and remove every state assumption:**

```bash
grep -rn "bookService\.\(currentBook\|selectedBook\|books\)" src/ --include="*.ts"
```

Anything reading state set by a previous navigation must move to a resolver or an input.

**5. Handle failures:** unknown ID → friendly Hebrew 404, never a blank screen. Chapter out of range → redirect to the chapter list.

**6. ⚠️ If the API cannot load a single book by ID**, that is a backend dependency. Document it and report; do not silently work around it with "fetch everything and filter" without flagging the cost.

### Definition of Done
- [ ] Pasting **any** app URL into a fresh tab (incognito, empty cache) loads the correct screen with data
- [ ] Tested at minimum: `/books`, `/books/:id`, `/books/:id/chapters/:id`, `/reminders`
- [ ] Refresh (F5) works on every screen
- [ ] Invalid ID produces a friendly Hebrew error and sensible navigation
- [ ] No component depends on state set by a previous navigation
- [ ] Automated test for each resolver

---

## F-03 · Move state management to Signals

**Priority:** P1 · **Size:** M · **Depends on:** F-02 · **Critical after v22 (OnPush by default)**

### The problem
`BookService` holds plain mutable fields. Under `OnPush` — the default since v22 — Angular has no way to know they changed, so the UI does not update.

### Steps

**1. Rewrite `BookService`:**

```ts
import { Injectable, signal, computed, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class BookService {
  private readonly api = inject(ApiCallService);

  // --- private state ---
  private readonly _books = signal<Book[]>([]);
  private readonly _currentBook = signal<Book | null>(null);
  private readonly _currentChapter = signal<Chapter | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  // --- read-only surface ---
  readonly books = this._books.asReadonly();
  readonly currentBook = this._currentBook.asReadonly();
  readonly currentChapter = this._currentChapter.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // --- derived ---
  readonly chapters = computed(() => this._currentBook()?.chapters ?? []);
  readonly chapterCount = computed(() => this.chapters().length);

  readonly hasNextChapter = computed(() => {
    const ch = this._currentChapter();
    return ch ? ch.number < this.chapterCount() : false;
  });

  readonly hasPreviousChapter = computed(() => (this._currentChapter()?.number ?? 1) > 1);

  readonly breadcrumb = computed(() => {
    const b = this._currentBook();
    const c = this._currentChapter();
    if (!b) return [];
    return c ? [b.name, `פרק ${c.number}`] : [b.name];
  });

  setCurrentBook(book: Book | null): void { this._currentBook.set(book); }
  setCurrentChapter(ch: Chapter | null): void { this._currentChapter.set(ch); }
}
```

**2. Conversion rules:**

| Before | After |
|---|---|
| Public mutable field | private `signal()` + public `.asReadonly()` |
| Getter computing from fields | `computed()` |
| `BehaviorSubject` for local state | `signal()` |
| `Observable` from HTTP | keep the Observable; convert with `toSignal()` in the component |
| `subscribe` + assign to field | `toSignal()` |
| localStorage write inside a subscribe | `effect()` |

> **Important distinction:** signals are for **state**. Observables remain the right tool for **events and async streams** (HTTP, WebSocket, DOM events). Do not try to eliminate `HttpClient`'s Observables.

**3. In components:**

```ts
@Component({
  selector: 'app-chapter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <app-spinner />
    } @else if (chapter(); as ch) {
      <h1>{{ bookName() }} · פרק {{ ch.number }}</h1>
      @for (verse of ch.verses; track verse.number) {
        <p class="verse"><span class="verse-num">{{ verse.number }}</span>{{ verse.text }}</p>
      }
    }
  `,
})
export class ChapterComponent {
  private readonly bookService = inject(BookService);

  readonly chapter = this.bookService.currentChapter;
  readonly loading = this.bookService.loading;
  readonly bookName = computed(() => this.bookService.currentBook()?.name ?? '');
}
```

**4. `effect()` is for side effects only** — e.g. persisting reading position:

```ts
constructor() {
  effect(() => {
    const ch = this.currentChapter();
    if (ch) localStorage.setItem('lastRead', JSON.stringify({ bookId: ch.bookId, chapterId: ch.id }));
  });
}
```

⚠️ **Never use `effect()` to update another signal** — that is what `computed()` is for. An effect writing to a signal is a classic code smell.

**5. Clear the `TODO(F-03)` markers** — every stopgap `ChangeDetectionStrategy.Default` added in F-01 goes back to `OnPush`.

**6. Sweep:**
```bash
grep -rn "BehaviorSubject\|\.next(\|\.subscribe(" src/ --include="*.ts"
```
Every remaining `subscribe` needs a documented justification plus `takeUntilDestroyed()`.

### Definition of Done
- [ ] No `BehaviorSubject` used for local state
- [ ] All state exposed via signals; no public mutable fields
- [ ] Every derived value is a `computed()`, not a template expression or getter
- [ ] All components explicitly `OnPush`; zero `TODO(F-03)` left in the codebase
- [ ] No `effect()` writes to a signal
- [ ] Every remaining `subscribe` is wrapped in `takeUntilDestroyed()`
- [ ] **Careful smoke test** — every screen updates; pay particular attention to prev/next chapter navigation

---

## F-04 · Enable zoneless change detection

**Priority:** P1 · **Size:** M · **Depends on:** F-03 (hard requirement — do not start before it is fully complete)

### Background
Zoneless has been stable since v20.2 and is the default for new apps since v21. Dropping `zone.js` saves ~30KB gzipped and removes monkey-patching of every async browser API.

### Steps

**1. `app.config.ts`:**

```ts
import { provideZonelessChangeDetection } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    // ... rest of providers
  ],
};
```

**2. Remove `zone.js`:**
- `angular.json` → `polyfills`: remove `"zone.js"` (and `"zone.js/testing"` from `test.polyfills`)
- `package.json`: `npm uninstall zone.js`

**3. Update tests:**

```ts
TestBed.configureTestingModule({
  providers: [provideZonelessChangeDetection()],
});
```

**4. Find patterns that break without zone.js:**

```bash
grep -rn "setTimeout\|setInterval\|addEventListener\|requestAnimationFrame\|NgZone\|new Promise" src/ --include="*.ts"
```

For each hit: does it mutate state? If so, that state must be a signal, or the UI will not update.

| Problem pattern | Fix |
|---|---|
| `setTimeout(() => this.x = 1)` | `setTimeout(() => this.x.set(1))` |
| Manual `element.addEventListener` | `@HostListener` / template binding / `fromEvent` + `toSignal` |
| `NgZone.run()` / `runOutsideAngular()` | Remove — meaningless now |
| Third-party library callback | Call `signal.set()` inside the callback |
| `ChangeDetectorRef.detectChanges()` | Remove; move to signals |

**5. Check third-party libraries** that depend on zone.js (older UI libraries, animation libraries). ⚠️ VERIFY every dependency in `package.json`.

### Definition of Done
- [ ] `zone.js` absent from `package.json` and from `polyfills`
- [ ] `grep -rn "zone.js\|NgZone" src/` returns nothing
- [ ] Bundle ~30KB smaller (measure and record)
- [ ] All tests pass with `provideZonelessChangeDetection()`
- [ ] **Exhaustive manual pass over every screen and interaction** — this is the change most likely to introduce silent UI bugs
- [ ] Specifically checked: async data loading, forms, navigation, spinners, timers, animations

---

## F-07 · `HttpClient` with functional interceptors

**Priority:** P1 · **Size:** S · **Depends on:** F-01, F-02

### Steps

**1. `core/interceptors/error.interceptor.ts`:**

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

const MESSAGES: Record<number, string> = {
  0:   'אין חיבור לאינטרנט. בדוק את החיבור ונסה שוב.',
  400: 'הבקשה אינה תקינה.',
  401: 'יש להתחבר מחדש.',
  403: 'אין לך הרשאה לצפות בתוכן הזה.',
  404: 'התוכן המבוקש לא נמצא.',
  429: 'יותר מדי בקשות. נסה שוב בעוד רגע.',
  500: 'תקלה בשרת. אנחנו כבר על זה.',
  503: 'השירות אינו זמין כרגע. נסה שוב בעוד מספר דקות.',
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      notifications.showError(MESSAGES[err.status] ?? 'אירעה שגיאה בלתי צפויה. נסה שוב.');
      return throwError(() => err);
    }),
  );
};
```

**2. `core/interceptors/retry.interceptor.ts`:**

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { retry, timer } from 'rxjs';

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  // reads only — never POST/PUT/DELETE (risk of duplicate side effects)
  if (req.method !== 'GET') return next(req);

  return next(req).pipe(
    retry({
      count: 2,
      delay: (error, retryCount) => {
        if (error.status >= 400 && error.status < 500) throw error;   // retrying is pointless
        return timer(Math.pow(2, retryCount) * 500);                   // exponential backoff
      },
    }),
  );
};
```

**3. `app.config.ts`:**

```ts
provideHttpClient(
  withFetch(),
  withInterceptors([retryInterceptor, errorInterceptor]),   // order matters: retry before error
)
```

**4. `NotificationService`** — create if missing. Signal-based, not Subject-based:

```ts
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _current = signal<{ text: string; type: 'error' | 'info' } | null>(null);
  readonly current = this._current.asReadonly();

  showError(text: string) {
    this._current.set({ text, type: 'error' });
    setTimeout(() => this._current.set(null), 6000);
  }
  dismiss() { this._current.set(null); }
}
```

Render it in `app.component.html` as an RTL toast with `role="alert"` and `aria-live="assertive"`.

**5. Remove duplicated error handling** from components — after this task, handling is centralised.

### Local testing
Simulate failures without any deployment: stop the local API, or set `apiUrl` to an unused port, or use DevTools → Network → Offline. All three exercise different paths (connection refused, timeout, status 0).

### Definition of Done
- [ ] `HttpClientModule` imported nowhere
- [ ] An API failure (local API stopped, or wrong `apiUrl`) shows a Hebrew message — not a blank screen and not a console-only error
- [ ] Failing GET requests retry twice on 5xx; POST/PUT/DELETE never retry
- [ ] 4xx errors are not retried
- [ ] Interceptor tests using `HttpTestingController`

---

## F-16 · Global error boundary + custom `ErrorHandler`

**Priority:** P1 · **Size:** S · **Depends on:** F-07

### Steps

**1. `core/global-error-handler.ts`:**

```ts
import { ErrorHandler, Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly errorState = inject(ErrorStateService);

  handleError(error: unknown): void {
    if (!environment.production) console.error('[GlobalErrorHandler]', error);

    // chunk load failure (new deploy while the user has the app open) — a reload fixes it
    const msg = (error as Error)?.message ?? '';
    if (/ChunkLoadError|Loading chunk .* failed|dynamically imported module/i.test(msg)) {
      this.errorState.showReloadPrompt('גרסה חדשה זמינה. רענן את הדף כדי להמשיך.');
      return;
    }

    this.errorState.showFatal();
    this.report(error);
  }

  private report(error: unknown): void {
    if (!environment.production) return;
    // TODO(LAUNCH): wire to an error monitoring service (Sentry / App Insights / none).
    // Deliberately unwired — requires an account and a hosted environment.
    // See docs/LAUNCH-CHECKLIST.md, item L-04.
  }
}
```

> **Do not install a monitoring SDK now.** It needs an account, adds bundle weight, and cannot be meaningfully tested locally. Build the seam; leave it empty.

**2. Register in `app.config.ts`:**

```ts
{ provide: ErrorHandler, useClass: GlobalErrorHandler },
provideBrowserGlobalErrorListeners(),   // catches unhandled rejections and window.onerror
```

**3. Friendly error screen** — `shared/error-screen.component.ts`:
- Hebrew heading, short explanation, a "Try again" button (reloads) and a "Back to home" button
- **No stack trace shown to users in production builds**
- RTL, accessible, and functional even if the router itself has failed

**4. Wiring:** `ErrorStateService` is signal-based; `app.component` renders the error screen in place of `router-outlet` when the flag is set.

### Definition of Done
- [ ] Throwing an artificial error (`throw new Error('test')` in a component) shows the friendly screen, not a white page
- [ ] `Promise.reject('test')` is also caught
- [ ] `ChunkLoadError` shows the reload prompt rather than the generic error screen
- [ ] In a production build, no stack trace is visible to the user
- [ ] The monitoring hook exists, is clearly marked `TODO(LAUNCH)`, and is listed in the launch checklist

---

## F-12 · Update and verify the service worker (PWA)

**Priority:** P1 · **Size:** M · **Depends on:** F-01

### Principle
Biblical text **never changes** → `performance` strategy (cache-first, no network round trip).
Dynamic metadata (reminders, user preferences) → `freshness` (network-first with fallback).

### Steps

**1. `ngsw-config.json`:**

```json
{
  "$schema": "./node_modules/@angular/service-worker/config/schema.json",
  "index": "/index.html",
  "assetGroups": [
    {
      "name": "app",
      "installMode": "prefetch",
      "resources": {
        "files": ["/favicon.ico", "/index.html", "/manifest.webmanifest", "/*.css", "/*.js"]
      }
    },
    {
      "name": "assets",
      "installMode": "lazy",
      "updateMode": "prefetch",
      "resources": {
        "files": ["/assets/**", "/*.(svg|png|jpg|webp|woff2)"]
      }
    }
  ],
  "dataGroups": [
    {
      "name": "tanach-content",
      "urls": ["/api/books/**", "/api/chapters/**"],
      "cacheConfig": {
        "strategy": "performance",
        "maxSize": 1000,
        "maxAge": "365d",
        "timeout": "5s"
      }
    },
    {
      "name": "dynamic-api",
      "urls": ["/api/reminders/**", "/api/user/**"],
      "cacheConfig": {
        "strategy": "freshness",
        "maxSize": 50,
        "maxAge": "1h",
        "timeout": "3s"
      }
    }
  ]
}
```

⚠️ VERIFY — match the URL patterns to the real API. Hebrew fonts, if self-hosted, **must** be in `assetGroups` with `prefetch`; otherwise offline text is unreadable.

**2. Version update mechanism — `core/services/app-update.service.ts`:**

```ts
import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AppUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  readonly updateAvailable = signal(false);

  init(): void {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
      .subscribe(() => this.updateAvailable.set(true));

    // check for updates every 6 hours
    setInterval(() => void this.swUpdate.checkForUpdate(), 6 * 60 * 60 * 1000);
  }

  applyUpdate(): void {
    void this.swUpdate.activateUpdate().then(() => document.location.reload());
  }
}
```

Template (RTL, non-blocking):

```html
@if (updateService.updateAvailable()) {
  <div class="update-banner" role="status">
    <span>יש גרסה חדשה של האפליקציה</span>
    <button type="button" (click)="updateService.applyUpdate()">רענן עכשיו</button>
  </div>
}
```

**3. Handle the `unrecoverable` state:** `swUpdate.unrecoverable.subscribe(() => location.reload())`.

**4. `app.config.ts`:**

```ts
provideServiceWorker('ngsw-worker.js', {
  enabled: environment.enableServiceWorker,     // from F-06
  registrationStrategy: 'registerWhenStable:30000',
}),
```

### Local testing — requires a production build, not `ng serve`

```bash
npx ng build --configuration production
npx http-server dist/<app>/browser -p 8080 -c-1
```

`localhost` counts as a secure context, so service workers register normally — no HTTPS certificate or domain needed.

**Testing the update flow locally** (no deployment required):
1. Build and serve, load `http://localhost:8080`, confirm the SW is active.
2. Change something visible (e.g. a heading), rebuild into the same `dist`.
3. Keep the tab open and trigger `swUpdate.checkForUpdate()` from the console, or reload once.
4. The update banner should appear; clicking it should load the new version.

### Definition of Done
- [ ] A chapter read once loads while offline (DevTools → Network → Offline)
- [ ] Fonts and CSS available offline — Hebrew text renders correctly
- [ ] The local rebuild flow above produces the update banner, and the button loads the new build
- [ ] DevTools → Application → Service Workers: registered and active
- [ ] DevTools → Application → Cache Storage: cache groups populated as expected

---

## F-13 · Fix `PwaInstallService` and the install experience

**Priority:** P2 · **Size:** S · **Depends on:** F-12

### `manifest.webmanifest` — target state

```json
{
  "name": "תנ\"ך",
  "short_name": "תנ\"ך",
  "description": "קריאת התנ\"ך המלא, גם ללא חיבור לאינטרנט",
  "start_url": "/?source=pwa",
  "scope": "/",
  "display": "standalone",
  "orientation": "portrait-primary",
  "background_color": "#ffffff",
  "theme_color": "#REPLACE",
  "dir": "rtl",
  "lang": "he",
  "categories": ["books", "education", "lifestyle"],
  "icons": [
    { "src": "assets/icons/icon-192.png", "sizes": "192x192", "type": "image/png", "purpose": "any" },
    { "src": "assets/icons/icon-512.png", "sizes": "512x512", "type": "image/png", "purpose": "any" },
    { "src": "assets/icons/maskable-192.png", "sizes": "192x192", "type": "image/png", "purpose": "maskable" },
    { "src": "assets/icons/maskable-512.png", "sizes": "512x512", "type": "image/png", "purpose": "maskable" }
  ]
}
```

⚠️ `theme_color` — take it from the existing brand styles, do not invent one. Maskable icons need roughly 20% padding around the artwork, otherwise Android crops them.

Note `start_url` and `scope` are relative — they work on localhost and on any future domain without modification.

### `index.html`

```html
<html lang="he" dir="rtl">
<head>
  <meta name="theme-color" content="#REPLACE">
  <link rel="manifest" href="manifest.webmanifest">
  <!-- iOS -->
  <link rel="apple-touch-icon" href="assets/icons/icon-192.png">
  <meta name="apple-mobile-web-app-capable" content="yes">
  <meta name="apple-mobile-web-app-status-bar-style" content="default">
  <meta name="apple-mobile-web-app-title" content="תנ&quot;ך">
```

### `PwaInstallService`

```ts
@Injectable({ providedIn: 'root' })
export class PwaInstallService {
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  readonly canInstall = signal(false);
  readonly isIos = signal(/iphone|ipad|ipod/i.test(navigator.userAgent));
  readonly isStandalone = signal(
    window.matchMedia('(display-mode: standalone)').matches ||
    (navigator as any).standalone === true,
  );

  init(): void {
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      this.deferredPrompt = e as BeforeInstallPromptEvent;
      this.canInstall.set(true);
    });
    window.addEventListener('appinstalled', () => {
      this.canInstall.set(false);
      this.deferredPrompt = null;
    });
  }

  async install(): Promise<void> {
    if (!this.deferredPrompt) return;
    await this.deferredPrompt.prompt();
    await this.deferredPrompt.userChoice;
    this.deferredPrompt = null;
    this.canInstall.set(false);
  }
}
```

**iOS** has no `beforeinstallprompt`. Show a manual hint — "Share → Add to Home Screen" — with the share icon. Only render it when `isIos() && !isStandalone()`.

### Local testing without a domain
- **Desktop Chrome:** serve the production build on `localhost:8080` and run the Lighthouse PWA audit; install works from localhost.
- **Android device:** connect over USB and use Chrome DevTools port forwarding (`chrome://inspect` → Port forwarding → `8080` → `localhost:8080`). The phone then treats `localhost:8080` as a secure origin, so the real install prompt appears.
- **iOS:** Safari on iOS cannot use port forwarding. Add-to-Home-Screen testing over a LAN IP will not be a secure context, so full verification is deferred — record it as launch checklist item L-03 and test the code path via device simulation in DevTools.

### Definition of Done
- [ ] Lighthouse PWA audit passes fully against the local production build
- [ ] Android Chrome (via port forwarding): install prompt appears and installation works
- [ ] The installed app opens standalone, RTL, with no address bar
- [ ] Maskable icons are not cropped (check with maskable.app)
- [ ] iOS manual hint renders under the right conditions; end-to-end iOS verification recorded as L-03

---

## F-08 · Add `@defer` for heavy components

**Priority:** P2 · **Size:** S · **Depends on:** F-05, F-09

### Candidates (⚠️ VERIFY against the code)

| Component | Suggested trigger |
|---|---|
| Advanced search / search results | `on interaction` |
| Settings / preferences | `on interaction` |
| Reminders dialog | `on interaction` |
| Commentary / footnotes | `on viewport` |
| Audio player / narration | `on interaction` |
| Charts / statistics | `on viewport` |

### Pattern

```html
@defer (on viewport; prefetch on idle) {
  <app-commentary [chapterId]="chapterId()" />
} @placeholder (minimum 300ms) {
  <div class="commentary-skeleton" aria-hidden="true"></div>
} @loading (after 150ms; minimum 400ms) {
  <app-spinner />
} @error {
  <p>לא ניתן לטעון את הפירוש. <button (click)="retry()">נסה שוב</button></p>
}
```

**Rules:**
- ❌ Never wrap first-paint content in `@defer` — least of all the chapter text itself
- ✅ Always provide a `@placeholder` roughly the height of the final content, to avoid layout shift
- ✅ Always provide `@error` with a retry affordance
- ⚠️ Deferred content is **not** rendered during SSR/prerender — weigh this against F-11 before deferring anything SEO-relevant

### Definition of Done
- [ ] Initial bundle measurably smaller (record before/after)
- [ ] No visible layout shift when deferred content loads (Lighthouse CLS < 0.1)
- [ ] Every `@defer` block has `@placeholder` and `@error`
- [ ] No SEO-critical content inside `@defer`

---

## F-10 · Set strict budgets in `angular.json`

**Priority:** P2 · **Size:** S · **Depends on:** F-08, F-09

### Configuration

```json
"budgets": [
  { "type": "initial",          "maximumWarning": "350kB", "maximumError": "500kB" },
  { "type": "allScript",        "maximumWarning": "800kB", "maximumError": "1mb"   },
  { "type": "anyComponentStyle","maximumWarning": "4kB",   "maximumError": "8kB"   },
  { "type": "bundle", "name": "styles", "maximumWarning": "100kB", "maximumError": "150kB" }
]
```

> ⚠️ **Measure before locking anything in.** If the current bundle is already 480kB, thresholds of 350/500 fail immediately. Two valid paths:
> 1. Set thresholds at current size + 5% and tighten incrementally, **or**
> 2. Optimise first (F-08/F-09), then lock in the target.
>
> Pick one, document the choice, and never quietly raise thresholds later.

### Enforcement without CI

There is no hosting yet and possibly no CI. Budgets are still enforced at build time, so they work locally today. Wire them into an npm script so the check is one command:

```json
"scripts": {
  "verify": "ng lint && ng test --watch=false --browsers=ChromeHeadless && ng build --configuration production && npm audit --omit=dev --audit-level=high"
}
```

Optionally add a pre-push git hook (husky or a plain `.git/hooks/pre-push`) running `npm run verify`. That gives the same guarantee as CI without any infrastructure.

⚠️ VERIFY — check whether a CI pipeline already exists. If it does, add `npm run verify` to it. If not, record CI setup as launch checklist item L-05; do not set one up now.

### Definition of Done
- [ ] Budgets configured based on a real measurement, with the reasoning recorded
- [ ] `ng build --configuration production` passes with the chosen values
- [ ] `npm run verify` exists and runs the full gate
- [ ] Artificially adding a heavy library makes the build fail (verify this actually works, then revert)
- [ ] Values and rationale recorded in `docs/UPGRADE-LOG.md`

---

## F-11 · Evaluate SSR/SSG (`@angular/ssr`)

**Priority:** P2 · **Size:** L · **Depends on:** F-09, F-14

> **This task starts with a decision, not with code.** Write `docs/adr/001-ssr-decision.md` and get approval before implementing.

### Inputs to the decision

**In favour of prerendering (SSG):**
- Biblical text is entirely static — an ideal candidate
- **SEO is the strongest argument.** People search for verses on Google. Without server-side rendering, chapter pages are nearly invisible to crawlers
- First-load performance — substantially better FCP/LCP
- Accessibility — screen readers get content before hydration
- Social sharing — Open Graph previews containing the verse

**Costs:**
- **Scale:** the Tanach is 39 books, ~929 chapters. Prerendering everything means ~929 HTML files. Longer builds, larger `dist`. Manageable, but it needs planning
- Requires a Node server or a host that supports static output with clean URLs
- Any code touching `window` / `document` / `localStorage` breaks during server rendering — needs `afterNextRender()` or `isPlatformBrowser` guards
- SSR and the service worker interact in ways that need attention
- `@defer` content (F-08) is not prerendered

**Timing note given no hosting:** the *implementation* is fully doable and testable locally — `ng build` produces the HTML files, and you can serve and inspect them with `http-server`. Only the hosting choice is deferred. That said, this is a large task whose main payoff (SEO) cannot be realised until the app is public, so it is reasonable to write the ADR now and defer implementation. **Decide explicitly rather than drifting.**

### Recommendation
**Full SSG (prerender), not dynamic SSR.** The content is static, there are no logged-in users on reading pages, and there is no reason to run Node at request time. Prerendering delivers all the SEO and speed benefit at zero runtime operational cost — and it produces plain static files, which keeps the eventual hosting decision maximally open and cheap.

A hybrid path: prerender content pages; leave dynamic pages (reminders, settings) as CSR.

### Implementation (only after approval)

```bash
npx ng add @angular/ssr
```

**1. Generate the prerender route list** — a build script that pulls the Tanach structure from the API:

```ts
// scripts/generate-routes.ts
// output: routes.txt, one line per /books/:bookId and /books/:bookId/chapters/:chapterId
```

Then `angular.json` → `prerender.routesFile: "routes.txt"`.

**2. Guard browser-only access:**

```bash
grep -rn "window\.\|document\.\|localStorage\|sessionStorage\|navigator\." src/ --include="*.ts"
```

For each occurrence:

```ts
import { afterNextRender, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

// option A — preferred
afterNextRender(() => {
  const last = localStorage.getItem('lastRead');
});

// option B
private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
```

**3. Per-chapter meta tags** (`Title` + `Meta`) — title, description containing the first verse, Open Graph, `og:locale=he_IL`, canonical URL.

⚠️ Canonical and `og:url` need an absolute origin, which does not exist yet. Read it from `environment.siteUrl` (add the field, default it to `http://localhost:8080`) and record it as launch checklist item L-02. Do not hardcode a guessed domain.

**4. `sitemap.xml`** — generated by the same script. Critical for SEO. Same origin caveat as above.

**5. Valid `robots.txt`.**

**6. Structured data** — JSON-LD `Book` / `Article` per chapter.

### Definition of Done
- [ ] `docs/adr/001-ssr-decision.md` exists with a reasoned decision and explicit approval
- [ ] If **no** — decision and rationale documented, task closed
- [ ] If **yes**: `ng build` emits HTML per chapter; serving `dist` locally and requesting a chapter URL returns full text in the HTML
- [ ] `view-source:` shows verse content, not just `<app-root></app-root>`
- [ ] No `window is not defined` errors during build
- [ ] `sitemap.xml` contains every chapter
- [ ] Lighthouse SEO = 100 against the local production build
- [ ] Hydration completes with no console errors
- [ ] Absolute-URL dependencies read from `environment.siteUrl` and recorded as L-02

---

## F-17 · Set up i18n even though the app is Hebrew-only

**Priority:** P3 · **Size:** M · **Depends on:** everything else (templates need to have stabilised)

### Steps

```bash
npx ng add @angular/localize
```

**1. Mark strings** — add `i18n` to every static text node in templates:

```html
<h1 i18n="@@app.title">תנ"ך</h1>
<button i18n="@@chapter.next" type="button">הפרק הבא</button>
<p i18n="@@books.empty">לא נמצאו ספרים</p>
```

Strings in TS use `$localize`:

```ts
const message = $localize`:@@error.network:אין חיבור לאינטרנט`;
```

**ID convention:** `@@<feature>.<element>` — stable, independent of the text itself.

**2. Extract:**

```bash
npx ng extract-i18n --output-path src/locale --format xlf2
```

**3. `angular.json`:**

```json
"i18n": {
  "sourceLocale": { "code": "he", "baseHref": "/", "subPath": "" },
  "locales": {
    "en": { "translation": "src/locale/messages.en.xlf", "baseHref": "/en/" }
  }
}
```

**4. RTL/LTR:** `<html lang="he" dir="rtl">` must become locale-driven at build time. CSS must use logical properties:

```css
/* no  */ margin-left: 8px;  padding-right: 4px;  text-align: right;  left: 0;
/* yes */ margin-inline-start: 8px;  padding-inline-end: 4px;  text-align: start;  inset-inline-start: 0;
```

```bash
grep -rnE "(margin|padding)-(left|right)|text-align:\s*(left|right)|(^|[^-])\b(left|right):" src/ --include="*.scss" --include="*.css"
```

**5. Numbers and dates:** `DatePipe` / `DecimalPipe` pick up the locale automatically. Note that Hebrew chapter letters (א׳, ב׳, ג׳) are **content, not translatable UI strings** — document the distinction.

**6. ⚠️ Do not translate the biblical text through i18n.** Scripture comes from the API; language translations are an entirely separate content feature.

### Definition of Done
- [ ] `ng extract-i18n` produces a file containing every UI string
- [ ] No Hebrew UI string hardcoded in a TS file
- [ ] `dir` / `lang` set from the active locale
- [ ] No physical `left`/`right` CSS, apart from documented exceptions
- [ ] The Hebrew build still works and looks identical
- [ ] A dummy English translation produces a valid LTR build

---

## 7. Ready-made prompts for Claude Code

Run one at a time, in the order from §3.2:

```
Read docs/TANACH-APP-FRONTEND-SPEC.md, sections 0-5, plus the F-06 spec.
Do F-06 only. First verify every item marked ⚠️ VERIFY against the actual code
and report any mismatch between the document and reality before changing anything.
When done, run the Verification Gate from section 5, update docs/UPGRADE-LOG.md, and stop.
```

```
Read the spec plus F-01. This is the highest-risk task in the document.
Do exactly one version step: 17 → 18. Do not continue to 19.
Stop after the commit and tag, and report what broke and how you fixed it.
```

**General template:**
```
Read docs/TANACH-APP-FRONTEND-SPEC.md, sections 0-5, plus the <ID> spec.
Do <ID> only. Verify the ⚠️ VERIFY items first.
Satisfy every Definition of Done item. Run the Verification Gate.
Nothing may depend on a deployed environment — if you hit something that does,
add a TODO(LAUNCH) marker and a row in docs/LAUNCH-CHECKLIST.md instead of guessing.
Update UPGRADE-LOG.md, commit in the agreed format, and stop for review.
```

---

## 8. Open decisions requiring the owner

| # | Decision | Task | Why it blocks |
|---|---|---|---|
| 1 | Material: stay on M2, or move to M3 tokens? | F-01 | Changes the scope of the upgrade |
| 2 | Any third-party library without Angular 22 support? | F-01 | Could block entirely |
| 3 | Does the API support fetching a single book/chapter by ID? | F-14 | Backend dependency |
| 4 | `PreloadAllModules` or selective preloading? | F-09 | UX vs. bandwidth |
| 5 | SSG: implement now, or write the ADR and defer? | F-11 | Large effort, payoff only after launch |
| 6 | Budget thresholds: current size or target size? | F-10 | |
| 7 | Karma → Vitest: when? | after F-01 | Test infrastructure |

Deliberately **not** on this list, because §0 resolves them: production domain, hosting provider, error monitoring vendor. Those are launch checklist items, not blockers.

---

## 9. `docs/LAUNCH-CHECKLIST.md` — deferred until there is a domain/host

Create this file during F-06 and add to it as tasks progress. Every entry must name the exact file and line to change, so going live is mechanical.

| ID | Item | Where | Added by |
|---|---|---|---|
| L-01 | Set the real production API URL | `src/environments/environment.production.ts` → `apiUrl` | F-06 |
| L-02 | Set the site origin for canonical/OG URLs and sitemap | `environment.siteUrl` | F-11 |
| L-03 | Verify PWA install end-to-end on a real iOS device over HTTPS | — | F-13 |
| L-04 | Choose and wire an error monitoring service, or decide against one | `core/global-error-handler.ts` → `report()` | F-16 |
| L-05 | Set up CI running `npm run verify` | repo | F-10 |
| L-06 | Choose hosting; if SSG (F-11), confirm it supports clean URLs and per-route HTML | — | F-11 |
| L-07 | Confirm CORS on the production API allows the production origin | backend | F-06 |
| L-08 | Set `theme_color` / `background_color` from final brand values | `manifest.webmanifest`, `index.html` | F-13 |

**Verification before launch:** `grep -rn "TODO(LAUNCH)" src/` — every hit must correspond to a checklist row, and every row must be closed.

---

## 10. Sources

- [Angular Update Guide](https://angular.dev/update-guide) — the official per-version migration guide
- [Zoneless · Angular](https://angular.dev/guide/zoneless)
- [provideZonelessChangeDetection · Angular API](https://angular.dev/api/core/provideZonelessChangeDetection)
- [Standalone migration · Angular](https://angular.dev/reference/migrations/standalone)
- [Angular 22: The Most Important New Features at a Glance · ANGULARarchitects](https://www.angulararchitects.io/en/blog/angular-22-the-most-important-new-features-at-a-glance/)
- [Angular 22: Key Features and Changes · angular.love](https://angular.love/angular-22-key-features-and-changes)
- [Angular Material 18 SASS API changes (M2/M3)](https://gist.github.com/shhdharmen/435f11430bcc4eb6ef9bdb768917d513)
- [Angular v21 goes zoneless by default · push-based.io](https://push-based.io/article/angular-v21-goes-zoneless-by-default-what-changes-why-its-faster-and-how-to)
