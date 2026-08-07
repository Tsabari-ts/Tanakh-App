# מיפוי טכני — Frontend (Angular)

מסמך זה ממפה את חלק ה-Frontend של הפרויקט, הממוקם תחת `Frontend/`. כל טענה מגובה בציטוט קובץ+שורה מהריפו. i18n/תרגומים אינם מכוסים לעומק כאן (מסמך נפרד).

---

## 1. Framework ותצורה

- **Framework**: Angular, גרסה `^22.1.0` עבור כל חבילות `@angular/*` (core, common, compiler, forms, platform-browser, router, animations, cdk, material, service-worker) — מוגדר ב-`Frontend/package.json:17-31`. הגרסה המותקנת בפועל (`node_modules/@angular/core/package.json`) היא `22.1.0`.
- **Angular CLI**: `^22.1.2` (`Frontend/package.json:35`), builder ראשי `@angular-devkit/build-angular:application` (esbuild-based) — `Frontend/angular.json:36`.
- **TypeScript**: `~6.0.3` (`Frontend/package.json:52`), מוגדר תחת `Frontend/tsconfig.json` עם `strict: true`, `noImplicitOverride`, `noPropertyAccessFromIndexSignature`, `target: ES2022`, `module: ES2022` (`Frontend/tsconfig.json:6-19`), ותוספות Angular-ספציפיות: `strictTemplates`, `strictInjectionParameters`, `strictInputAccessModifiers` (`Frontend/tsconfig.json:21-25`).
- **tsconfig.app.json** מרחיב את tsconfig.json הראשי, מגדיר `types: ["@angular/localize"]` ונקודת כניסה `src/main.ts` (`Frontend/tsconfig.app.json:1-11`).
- **tsconfig.spec.json** מרחיב את אותו קובץ עבור בדיקות Karma/Jasmine, כולל `src/**/*.spec.ts` (`Frontend/tsconfig.spec.json:6-9`).
- **פרויקט Angular יחיד** בשם `"Tanakh"` בתוך `angular.json`, `sourceRoot: "src"`, `prefix: "app"` (`Frontend/angular.json:6-21`).
- **Standalone components בלבד בפועל**: סכמת ה-schematics ב-`angular.json:8-17` מציינת `"standalone": false` כברירת מחדל ל-component/directive/pipe חדשים שנוצרים דרך ה-CLI, אך כל הקומפוננטות הקיימות בפרויקט הן standalone בפועל (למשל `Frontend/src/app/app.component.ts:23-29` עם מערך `imports`, ו-`bootstrapApplication` ב-`Frontend/src/main.ts:7` ולא `NgModule`/`platformBrowserDynamic`).
- **Change detection**: `provideZonelessChangeDetection()` — האפליקציה רצה ללא Zone.js (`Frontend/src/app/app.config.ts:22`). כל הקומפוננטות שנבדקו משתמשות ב-`ChangeDetectionStrategy.OnPush` (למשל `Frontend/src/app/app.component.ts:27`).
- **i18n build config** (רק לצורך ניתוב, לא ניתוח מעמיק): `sourceLocale: he`, locale נוסף `en` עם קובץ תרגום `src/locale/messages.en.xlf` ו-`baseHref: /en/` (`Frontend/angular.json:22-33`).
- **Service Worker**: מופעל בבנייה production בלבד, קונפיגורציה `ngsw-config.json` (`Frontend/angular.json:84`), עם asset groups ל-`app` (prefetch) ו-`assets` (lazy) — `Frontend/ngsw-config.json:1-24`.
- **Bundle budgets** (production): initial 350kb/500kb, allScript 850kb/1.1mb, anyComponentStyle 4kb/8kb, סגנון גלובלי 110kb/150kb (`Frontend/angular.json:60-81`).
- **קבצי style גלובליים הנטענים ע"י Angular CLI** (סדר טעינה, `Frontend/angular.json:47-52` ו-`Frontend/angular.json:127-132` לבדיקות): `@angular/material/prebuilt-themes/indigo-pink.css`, `@angular/cdk/a11y-prebuilt.css`, `src/styles/a11y.scss`, `src/styles.css` — לפי סדר זה (ראו סעיף 8).
- **Linting**: Stylelint על קבצי `css`/`scss` דרך script `lint:css` (`Frontend/package.json:12`), קונפיגורציה ב-`Frontend/.stylelintrc.json` (אוסרת `outline: none/0`, אוסרת `!important` פרט לחריגה מפורשת ל-`_a11y-modes.scss`/`_a11y-utils.scss`).
- **בדיקות**: Karma+Jasmine ליחידה (`Frontend/angular.json:118-138`, script `test` ב-`Frontend/package.json:9`), Playwright ל-e2e נגישות (`Frontend/playwright.config.ts`, `Frontend/e2e/a11y.spec.ts`), ו-Lighthouse CI (`Frontend/lighthouserc.json`, script `lighthouse:a11y` ב-`Frontend/package.json:11`).

---

## 2. טבלת תלויות (package.json)

מקור: `Frontend/package.json:16-53`. עמודת "שימוש בפועל" מבוססת על חיפוש import/usage תחת `Frontend/src`.

### dependencies

| חבילה | גרסה | סוג | שימוש בפועל בקוד |
|---|---|---|---|
| `@angular/animations` | `^22.1.0` | dependency | מסופק דרך `provideAnimations()` ב-`Frontend/src/app/app.config.ts:28` (נדרש ע"י Angular Material לאנימציות דיאלוג). |
| `@angular/cdk` | `^22.1.0` | dependency | `LiveAnnouncer` ב-`Frontend/src/app/core/a11y/a11y.service.ts:3`, `Dir` (bidi) ב-`Frontend/src/app/app.component.ts:4`, `CdkScrollable` ב-`Frontend/src/app/components/read-permission/read-permission.component.ts:3` ו-`Frontend/src/app/components/welcome-modal/welcome-modal.component.ts:4`, `A11yModule` ב-`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts:2`. |
| `@angular/common` | `^22.1.0` | dependency | `Location`/`DOCUMENT` וכו', למשל `Location` ב-`Frontend/src/app/app.component.ts:3`, `DatePipe` ב-`Frontend/src/app/admin/users/admin-users.component.ts:1`. |
| `@angular/compiler` | `^22.1.0` | dependency | נדרש ל-Angular JIT/AOT compiler (תשתית build, ללא import ישיר באפליקציה). |
| `@angular/core` | `^22.1.0` | dependency | ליבת הפריימוורק — בשימוש בכל קובץ קומפוננטה/שירות (למשל `Component`, `Injectable`, `signal` ב-`Frontend/src/app/services/theme.service.ts:1`). |
| `@angular/forms` | `^22.1.0` | dependency | `FormsModule`/`NgForm` ב-`Frontend/src/app/components/subscribe/subscribe.component.ts:5`, `Frontend/src/app/components/entrance/entrance.component.ts:5`, `Frontend/src/app/admin/login/admin-login.component.ts:3` ועוד. |
| `@angular/material` | `^22.1.0` | dependency | `MatDialog`/`MatDialogRef` ב-`Frontend/src/app/services/dialog.service.ts:2`, `MatTableModule`/`MatPaginatorModule` ב-`Frontend/src/app/admin/users/admin-users.component.ts:5-7`, `MatIcon` ב-`Frontend/src/app/components/scroll-to-top-button/scroll-to-top-button.component.ts:2`. |
| `@angular/platform-browser` | `^22.1.0` | dependency | `bootstrapApplication` ב-`Frontend/src/main.ts:3`, `DomSanitizer` ב-`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts:5`. |
| `@angular/platform-browser-dynamic` | `^22.1.0` | dependency | לא נמצא import ישיר תחת `Frontend/src` (הפרויקט עולה דרך `bootstrapApplication` הסטטי מ-`platform-browser`, לא `platformBrowserDynamic`). מותקן כתלות סטנדרטית של Angular CLI. |
| `@angular/router` | `^22.1.0` | dependency | `provideRouter`, `Routes`, `Router` וכו', למשל `Frontend/src/app/app.routes.ts:1`, `Frontend/src/app/app.config.ts:2`. |
| `@angular/service-worker` | `^22.1.0` | dependency | `provideServiceWorker` ב-`Frontend/src/app/app.config.ts:5`, `SwUpdate`/`VersionReadyEvent` ב-`Frontend/src/app/core/app-update.service.ts:2`. |
| `@types/gematriya` | `^2.0.5` | dependency | לא נמצא import ישיר (חבילת type-declarations בלבד). היא מספקת טיפוסי TypeScript ל-import של `gematriya` הבלתי-מוקלד, למשל ב-`Frontend/src/app/components/chapter/chapter.component.ts:10`. |
| `gematriya` | `^2.0.0` | dependency | ממיר מספרים לגימטריה עברית — `Frontend/src/app/components/chapter/chapter.component.ts:10,50,108`, `Frontend/src/app/components/chapterlist/chapterlist.component.ts:6,74`, `Frontend/src/app/components/home/home.component.ts:7,86`. |
| `rxjs` | `~7.8.0` | dependency | Observables/אופרטורים בכל שכבת השירותים וה-interceptors, למשל `Frontend/src/app/core/interceptors/retry.interceptor.ts:2`, `Frontend/src/app/services/api-call.service.ts:3`. |
| `tslib` | `^2.3.0` | dependency | ספריית עזר של TypeScript להפעלת `importHelpers: true` (`Frontend/tsconfig.json:14`) — ללא import ישיר בקוד היישום (משמש את פלט הקומפילציה). |

### devDependencies

| חבילה | גרסה | סוג | שימוש בפועל בקוד |
|---|---|---|---|
| `@angular-devkit/build-angular` | `^22.1.2` | devDependency | ה-builder בפועל של `ng build`/`ng serve`/`ng test`, מוגדר ב-`Frontend/angular.json:36,101,119`. |
| `@angular/cli` | `^22.1.2` | devDependency | כלי ה-CLI (`ng ...`), הסכימה מוגדרת ב-`Frontend/angular.json:2`. |
| `@angular/compiler-cli` | `^22.1.0` | devDependency | קומפיילר AOT בזמן build (תשתית, ללא import ישיר). |
| `@angular/localize` | `^22.1.0` | devDependency | `$localize` בכל קובץ עם טקסטים מתורגמים (`i18n`), למשל `Frontend/src/app/core/interceptors/error.interceptor.ts:7-15`; נטען כ-polyfill ב-`Frontend/angular.json:55` ו-`Frontend/angular.json:135`, ומיובא כ-reference type ב-`Frontend/src/main.ts:1`. |
| `@axe-core/playwright` | `^4.12.1` | devDependency | `AxeBuilder` ב-`Frontend/e2e/a11y.spec.ts:2` (בדיקות נגישות אוטומטיות). |
| `@lhci/cli` | `^0.15.1` | devDependency | מריץ את script `lighthouse:a11y` (`Frontend/package.json:11`) מול `Frontend/lighthouserc.json`. |
| `@playwright/test` | `^1.62.1` | devDependency | מסגרת ה-e2e, `Frontend/playwright.config.ts:1`, `Frontend/e2e/a11y.spec.ts:1`. |
| `@types/jasmine` | `~5.1.0` | devDependency | טיפוסי TypeScript ל-Jasmine (בשימוש דרך `tsconfig.spec.json:9`, קבצי `*.spec.ts` בכל הפרויקט). |
| `istanbul-lib-instrument` | `^6.0.3` | devDependency | לא נמצא import ישיר תחת `Frontend/src`; ספריית instrumentation לכיסוי קוד, נצרכת בעקיפין ע"י `karma-coverage` (ראו `Frontend/package-lock.json` — `karma-coverage` תלוי בגרסה נפרדת 5.2.1 של אותה חבילה; הגרסה 6.0.3 מוגדרת ישירות ב-`package.json`). |
| `jasmine-core` | `~5.1.0` | devDependency | מנוע ההרצה של קבצי `*.spec.ts` (למשל `Frontend/src/app/app.component.spec.ts`), דרך ה-builder ב-`Frontend/angular.json:119`. |
| `karma` | `~6.4.0` | devDependency | Test runner, מופעל ע"י `Frontend/angular.json:119` (`@angular-devkit/build-angular:karma`). |
| `karma-chrome-launcher` | `~3.2.0` | devDependency | מריץ בדיקות ב-ChromeHeadless, בשימוש ע"י script `verify` (`Frontend/package.json:13`). |
| `karma-coverage` | `~2.2.0` | devDependency | דוחות כיסוי קוד לבדיקות Karma (תשתית build, אין import ישיר). |
| `karma-jasmine` | `~5.1.0` | devDependency | מגשר בין Karma ל-Jasmine (תשתית build). |
| `karma-jasmine-html-reporter` | `~2.1.0` | devDependency | דוח HTML של תוצאות בדיקה (תשתית build). |
| `postcss-scss` | `^4.0.9` | devDependency | `customSyntax` עבור Stylelint על קבצי SCSS — `Frontend/.stylelintrc.json:2`. |
| `sass` | `^1.102.0` | devDependency | מהדר ה-SCSS בפרויקט (`Frontend/src/styles/*.scss`, `*.component.scss` בכל הקומפוננטות). |
| `stylelint` | `^17.14.1` | devDependency | script `lint:css` (`Frontend/package.json:12`), קונפיגורציה `Frontend/.stylelintrc.json`. |
| `typescript` | `~6.0.3` | devDependency | מהדר TypeScript, `Frontend/tsconfig.json`. |

לא נמצאה שום חבילת Tailwind, NgRx, או ספריית ניהול state צד-שלישי כלשהי ב-`Frontend/package.json` (רק בתוך `node_modules/@schematics/angular/tailwind` — schematic מובנה שאינו בשימוש בפרויקט זה, ללא הפעלה בקובצי config).

---

## 3. מפת קבצים — `Frontend/src/app`

### `app.component.ts` / `app.routes.ts` / `app.config.ts` (שורש)
- `Frontend/src/app/app.component.ts` — קומפוננטת השורש (`app-root`); מנהלת header/footer ציבוריים, ניווט "אחורה" דינמי (`backTarget`), זיהוי אם המסלול הנוכחי הוא פאנל הניהול (`isAdminRoute`), ופתיחת דיאלוגים משפטיים דרך query params (`Frontend/src/app/app.component.ts:61-70`).
- `Frontend/src/app/app.routes.ts` — טבלת הניתוב הראשית (ראו סעיף 4).
- `Frontend/src/app/app.config.ts` — ה-`ApplicationConfig` שמזריק providers גלובליים: zoneless change detection, router, HTTP client + interceptors, service worker, ו-4 `provideAppInitializer` (עדכון גרסה, PWA install, בדיקת תחזוקה, דיאלוג welcome) — `Frontend/src/app/app.config.ts:20-41`.
- `Frontend/src/app/app.component.spec.ts` — בדיקת יחידה לקומפוננטת השורש.

### `admin/` — פאנל ניהול נסתר
תיקייה שטוחה עם שירותים ב-root וקומפוננטות בתת-תיקיות. הנתיב עצמו מוסתר (`environment.adminRoutePath`, ראו סעיף 6).
- `admin-auth.service.ts` — login/OTP/logout/checkSession מול `/api/v1/admin/auth/*` (`Frontend/src/app/admin/admin-auth.service.ts:10-32`).
- `admin-date-range.service.ts` — signal-state לטווח תאריכים נבחר (`today`/`7d`/`30d`) + auto-refresh כל 60 שניות (`Frontend/src/app/admin/admin-date-range.service.ts:5,22-24`).
- `admin-export.service.ts` — הורדת CSV (`users`/`sms-log`/`error-log`) כ-blob מ-`/api/v1/admin/export/{resource}` (`Frontend/src/app/admin/admin-export.service.ts:13-20`).
- `admin-logs.service.ts` — קריאה/פתרון/ניקוי לוגים מ-`/api/v1/admin/logs` (`Frontend/src/app/admin/admin-logs.service.ts:21-46`).
- `admin-sms.service.ts` — יתרת SMS/סטטיסטיקות/לוג/שליחת הודעת בדיקה מ-`/api/v1/admin/sms` (`Frontend/src/app/admin/admin-sms.service.ts:19-45`).
- `admin-stats.service.ts` — נתוני overview מ-`/api/v1/admin/stats/overview` (`Frontend/src/app/admin/admin-stats.service.ts:15-20`).
- `admin-system.service.ts` — health/maintenance/banner/feature-flags מ-`/api/v1/admin/system/*` (`Frontend/src/app/admin/admin-system.service.ts:15-49`).
- `admin-users.service.ts` — רשימת/חסימת/מחיקת משתמשים מ-`/api/v1/admin/users` (`Frontend/src/app/admin/admin-users.service.ts:25-46`).
- `admin.guard.ts` — `CanActivateFn` הבודק סשן admin פעיל (`checkSession`) ומפנה ל-login אם נכשל (`Frontend/src/app/admin/admin.guard.ts:7-14`).
- `admin.models.ts` — טיפוסי TypeScript משותפים לתגובות ה-API של הניהול (`Frontend/src/app/admin/admin.models.ts`).
- `admin.routes.ts` — טבלת ניתוב פנימית של הפאנל (ראו סעיף 4).
- `date-range.util.ts` — פונקציית עזר `resolveDateRange` הממירה preset לטווח תאריכים בפועל (`Frontend/src/app/admin/date-range.util.ts:3-20`).
- `query-params.util.ts` — `toQueryParams` המסנן ערכי query ריקים/undefined (`Frontend/src/app/admin/query-params.util.ts:5-13`).
- `login/admin-login.component.ts` — טופס דו-שלבי (סיסמה ואז OTP) (`Frontend/src/app/admin/login/admin-login.component.ts:22-105`).
- `logs/admin-logs.component.ts` — טבלת לוגי שגיאות עם סינון/pagination/ייצוא (`Frontend/src/app/admin/logs/admin-logs.component.ts`).
- `overview/admin-overview.component.ts` — לוח KPI ראשי, נטען מחדש בכל שינוי טווח תאריכים (`Frontend/src/app/admin/overview/admin-overview.component.ts:23-29`).
- `shared/confirm-dialog.component.ts` — דיאלוג אישור/ביטול גנרי (`Frontend/src/app/admin/shared/confirm-dialog.component.ts`).
- `shared/download-blob.util.ts` — מפעיל הורדת קובץ מ-Blob בדפדפן (`Frontend/src/app/admin/shared/download-blob.util.ts:5-12`).
- `shell/admin-shell.component.ts` — מעטפת הניווט של הפאנל (תפריט, בורר טווח תאריכים, logout) (`Frontend/src/app/admin/shell/admin-shell.component.ts`).
- `sms/admin-sms.component.ts` — ניהול SMS: יתרה, סטטיסטיקות, לוג, שליחת הודעת בדיקה (`Frontend/src/app/admin/sms/admin-sms.component.ts`).
- `system/admin-system.component.ts` — בריאות מערכת, מצב תחזוקה, באנר הכרזות, feature flags (`Frontend/src/app/admin/system/admin-system.component.ts`).
- `users/admin-users.component.ts` — טבלת משתמשים עם חיפוש/חסימה/מחיקה/ייצוא (`Frontend/src/app/admin/users/admin-users.component.ts`).

### `components/` — מסכי הקריאה הציבוריים
- `booklist/booklist.component.ts` — רשימת ספרים לפי חטיבה (תורה/נביאים/כתובים), טעינה מ-`getBookList`, מציג התקדמות קריאה (`Frontend/src/app/components/booklist/booklist.component.ts:36-75`).
- `chapter/chapter.component.ts` — מסך קריאת פרק: טעינת פסוקים (`getVerses`), TTS, גלילה אוטומטית, סימון "נקרא", ניווט לפרק הבא/קודם, שמירת התקדמות (`Frontend/src/app/components/chapter/chapter.component.ts`).
- `chapterlist/chapterlist.component.ts` — רשימת פרקים לספר נבחר, טעינה מ-`getBookByTitle` (`Frontend/src/app/components/chapterlist/chapterlist.component.ts`).
- `entrance/entrance.component.ts` — מסך כניסה ראשוני (אנימציית מילים, בדיקת חג/שבת מ-`getHolidays`, שער הזנת שם) (`Frontend/src/app/components/entrance/entrance.component.ts`).
- `home/home.component.ts` — מסך בית: ברכה, "המשך קריאה", סטטיסטיקות קריאה (`Frontend/src/app/components/home/home.component.ts`).
- `read-permission/read-permission.component.ts` — דיאלוג המבקש אישור שמירת התקדמות מקומית (`localStorage`) (`Frontend/src/app/components/read-permission/read-permission.component.ts`).
- `scroll-to-top-button/scroll-to-top-button.component.ts` — כפתור "למעלה" צף עבור קונטיינר גלילה נתון (`Frontend/src/app/components/scroll-to-top-button/scroll-to-top-button.component.ts`).
- `settings/settings.component.ts` — עמוד הגדרות: מצב כהה/בהיר, גופן קורא, התקנת PWA, צור קשר, מארח את `SubscribeComponent` (`Frontend/src/app/components/settings/settings.component.ts`).
- `subscribe/subscribe.component.ts` — הרשמה/ניהול תזכורות SMS, כולל זרימת OTP מלאה (`Frontend/src/app/components/subscribe/subscribe.component.ts`).
- `welcome-modal/welcome-modal.component.ts` — דיאלוג ברוכים הבאים, נפתח פעם אחת ל-6 חודשים (`Frontend/src/app/services/dialog.service.ts:16`).

כל קומפוננטות ה-`components/*` כוללות גם `*.spec.ts` (בדיקת יחידה).

### `core/` — תשתית אפליקטיבית
- `a11y/a11y.model.ts` — טיפוסים וברירות מחדל להגדרות נגישות (`FontScale`, `ContrastMode`, `A11ySettings`, `A11Y_DEFAULTS`) (`Frontend/src/app/core/a11y/a11y.model.ts`).
- `a11y/a11y.service.ts` — ניהול מצב הגדרות הנגישות (signal + effect), שמירה ל-`localStorage`, יישום מחלקות CSS על `<html>`, ניהול "סרגל קריאה" דינמי (`Frontend/src/app/core/a11y/a11y.service.ts`).
- `a11y/route-focus.service.ts` — מעביר focus ל-`<h1>`/`#main-content` ומכריז עליו בכל ניווט (`NavigationEnd`) (`Frontend/src/app/core/a11y/route-focus.service.ts`).
- `app-update.service.ts` — עוטף `SwUpdate` (Service Worker) לאיתור גרסה חדשה ורענון (`Frontend/src/app/core/app-update.service.ts`).
- `global-error-handler.ts` — `ErrorHandler` גלובלי; מזהה שגיאות טעינת chunk (deploy חדש) ומציג הנחיה לרענון, אחרת מציג מסך שגיאה fatal (`Frontend/src/app/core/global-error-handler.ts`).
- `interceptors/error.interceptor.ts` — מתרגם קודי HTTP שגיאה להודעות בעברית ומציג אותן דרך `NotificationService` (`Frontend/src/app/core/interceptors/error.interceptor.ts`).
- `interceptors/retry.interceptor.ts` — retry אוטומטי (עד 2 ניסיונות, backoff מעריכי) לבקשות `GET` בלבד (`Frontend/src/app/core/interceptors/retry.interceptor.ts`).
- `tts/tts-provider.ts` — abstract class המגדיר חוזה ספק-TTS (מאפשר להחליף מנוע בעתיד) (`Frontend/src/app/core/tts/tts-provider.ts`).
- `tts/tts-text-normalizer.ts` — ניקוי טקסט פסוק לפני הקראה (טעמים, ניקוד, שם הוי"ה, סימני פרשה) (`Frontend/src/app/core/tts/tts-text-normalizer.ts`).
- `tts/tts.model.ts` — טיפוסים והגדרות ברירת מחדל להקראה (`Frontend/src/app/core/tts/tts.model.ts`).
- `tts/tts.service.ts` — לוגיקת ניגון/השהיה/חזרה, ניהול "נקודת המשך", עצירה אוטומטית ביציאה מהכרטיסייה (`Frontend/src/app/core/tts/tts.service.ts`).
- `tts/web-speech-provider.service.ts` — מימוש בפועל של `TtsProvider` דרך Web Speech API, כולל עקיפות ל-quirks של Chrome/iOS Safari (`Frontend/src/app/core/tts/web-speech-provider.service.ts`).
- לקובצי `*.spec.ts` המקבילים (`app-update.service.spec.ts`, `global-error-handler.spec.ts`, `error.interceptor.spec.ts`, `retry.interceptor.spec.ts`) — בדיקות יחידה.

**הערה**: משימת ה-task מציינת אפשרות לתיקייה `Frontend/src/app/core/interceptors` — קיימת בפועל וכוללת את שני ה-interceptors הנ"ל בלבד. אין תיקיות `guards`/`pipes`/`directives`/`store` תחת `core` או `app` (ראו סעיף "לא ידוע" למטה).

### `models/`
- `BookData.ts` — ממשק `BookData` (section, heTitle, title, length, chapters, book, heBook) — מתאר צורת נתוני ספר המוחזרת מה-API (`Frontend/src/app/models/BookData.ts`).

### `services/`
- `api-call.service.ts` — השירות המרכזי לכל קריאות ה-API הציבוריות (ראו סעיף 6 לפירוט מלא).
- `book.service.ts` — signal-state פשוט לרשימת מספרי פרקים של הספר הנוכחי (`Frontend/src/app/services/book.service.ts`).
- `dialog.service.ts` — פותח דיאלוגי Welcome / Read-Permission (`Frontend/src/app/services/dialog.service.ts`).
- `error-state.service.ts` — signal-state למצב שגיאה גלובלי (`none`/`fatal`/`reload`) — נצרך ע"י `GlobalErrorHandler` ו-`ErrorScreenComponent` (`Frontend/src/app/services/error-state.service.ts`).
- `maintenance.service.ts` — בודק מצב תחזוקה בעליית האפליקציה (מדלג על נתיב הניהול) (`Frontend/src/app/services/maintenance.service.ts`).
- `notification.service.ts` — signal-state ל"טוסט" הודעה (שגיאה/מידע), נעלם אוטומטית אחרי 6 שניות (`Frontend/src/app/services/notification.service.ts`).
- `pwa-install.service.ts` — עוטף את אירוע `beforeinstallprompt` להתקנת PWA (`Frontend/src/app/services/pwa-install.service.ts`).
- `reader-prefs.service.ts` — signal-state להעדפות קריאה (גופן, גודל טקסט, ניקוד) הנשמר ב-`localStorage` (`Frontend/src/app/services/reader-prefs.service.ts`).
- `reading-history.service.ts` — signal-state להיסטוריית פרקים שסומנו כ"נקראו", כולל חישוב streak — כולו client-side (`localStorage` בלבד, אין endpoint) (`Frontend/src/app/services/reading-history.service.ts:20-27`).
- `theme.service.ts` — מצב כהה/בהיר ידני, שכבה מעל `prefers-color-scheme` (`Frontend/src/app/services/theme.service.ts`).
- קובצי `*.spec.ts` מקבילים ל-`api-call.service`, `book.service`, `dialog.service`, `pwa-install.service`.

### `shared/`
- `a11y/accessibility-widget/accessibility-widget.component.ts` — ה-widget הצף לשליטה בהגדרות נגישות (גודל טקסט, ניגודיות, וכו') (`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts`).
- `a11y/skip-link/skip-link.component.ts` — קישור "דילוג לתוכן הראשי" (`Frontend/src/app/shared/a11y/skip-link/skip-link.component.ts`).
- `announcement-banner/announcement-banner.component.ts` — באנר הכרזות הנטען מהשרת (`getAnnouncementBanner`), עם דחייה per-content (`Frontend/src/app/shared/announcement-banner/announcement-banner.component.ts`).
- `contact-links.ts` — קבועים: קישור WhatsApp ואימייל יצירת קשר (`Frontend/src/app/shared/contact-links.ts`).
- `cookie-banner/cookie-banner.component.ts` — באנר עוגיות סטטי עם דחייה יחידה (`Frontend/src/app/shared/cookie-banner/cookie-banner.component.ts`).
- `error-screen/error-screen.component.ts` — מסך שגיאה fatal, עם רענון/חזרה לבית (`Frontend/src/app/shared/error-screen/error-screen.component.ts`).
- `israeli-mobile-phone-validator.ts` — ולידציה/נירמול מספר טלפון ישראלי, "משקף במדויק" ולידציה מקבילה בצד השרת (`Frontend/src/app/shared/israeli-mobile-phone-validator.ts:1-4`).
- `legal/legal-content-html.ts`, `legal/legal-content.ts` — תוכן משפטי (תנאי שימוש/פרטיות/נגישות) — נטענים דינמית (lazy chunk) (`Frontend/src/app/shared/legal/legal-dialog.service.ts:13-16`).
- `legal/legal-dialog.service.ts` — נקודת כניסה יחידה לפתיחת דיאלוגי תוכן משפטי (`Frontend/src/app/shared/legal/legal-dialog.service.ts`).
- `legal/legal-modal/legal-modal.component.ts` — קומפוננטת הדיאלוג המשפטי עצמו, מציג HTML "מהימן" (developer-authored בלבד) (`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts:17-18`).
- `maintenance-screen/maintenance-screen.component.ts` — מסך תחזוקה (מוצג כש-`MaintenanceService` מדווח `enabled: true`) (`Frontend/src/app/shared/maintenance-screen/maintenance-screen.component.ts`).
- `reminder-subscription.ts` — ניהול "manage token" בזיכרון מקומי לצורך גישה עתידית להרשמת התזכורת ללא מנגנון login (`Frontend/src/app/shared/reminder-subscription.ts:1-6`).
- `tts/tts-player/tts-player.component.ts` — ממשק נגן ההקראה (play/pause/next/prev/rate/voice) (`Frontend/src/app/shared/tts/tts-player/tts-player.component.ts`).
- `user-prefs.ts` — קריאה/שמירה של שם המשתמש שהוזן בשער הכניסה (`Frontend/src/app/shared/user-prefs.ts`).

לא קיימות תיקיות `pipes/`, `directives/`, `guards/` (כתיקיות עצמאיות) או `store/` תחת `Frontend/src/app` — ה-guard היחיד (`admin.guard.ts`) יושב ישירות בתוך `Frontend/src/app/admin/`.

---

## 4. טבלת ניתוב (Routes)

מקור ראשי: `Frontend/src/app/app.routes.ts`. מקור פאנל הניהול: `Frontend/src/app/admin/admin.routes.ts`.

| נתיב (path) | קומפוננטה שנטענת | Guard/הרשאה | Lazy-loaded | קובץ הגדרה |
|---|---|---|---|---|
| `""` | (redirect ל-`entrance`) | — | — | `Frontend/src/app/app.routes.ts:5` |
| `<environment.adminRoutePath>` (למשל `admin-x9k2`) | `adminRoutes` (children, ראו טבלה שנייה) | ראו children | כן (`loadChildren`) | `Frontend/src/app/app.routes.ts:6-12` |
| `entrance` | `EntranceComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:13-17` |
| `home` | `HomeComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:18-22` |
| `settings` | `SettingsComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:23-27` |
| `books/:section` | `BooklistComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:28-32` |
| `books/:section/:book` | `ChapterlistComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:33-37` |
| `books/:section/:book/:chapterNumber/:keepReading` | `ChapterComponent` | אין | כן (`loadComponent`) | `Frontend/src/app/app.routes.ts:38-42` |
| `*` | (redirect ל-`home`) | — | — | `Frontend/src/app/app.routes.ts:43` |

**כל הנתיבים** נטענים תחת `provideRouter(routes, withComponentInputBinding(), withPreloading(PreloadAllModules))` — כלומר גם אם מוגדרים כ-lazy (`loadComponent`/`loadChildren`), Angular מבצע preload של כל ה-chunks ברקע מיד לאחר עליית האפליקציה (`Frontend/src/app/app.config.ts:27`).

### תת-ניתוב פאנל הניהול (`Frontend/src/app/admin/admin.routes.ts`)

| נתיב (יחסי ל-prefix הניהול) | קומפוננטה שנטענת | Guard/הרשאה | Lazy-loaded | קובץ הגדרה |
|---|---|---|---|---|
| `login` | `AdminLoginComponent` | אין | כן | `Frontend/src/app/admin/admin.routes.ts:5-8` |
| `""` (root) | `AdminShellComponent` (מכיל children) | **כן** — `adminGuard` (`Frontend/src/app/admin/admin.guard.ts`) | כן | `Frontend/src/app/admin/admin.routes.ts:9-13` |
| `""` (בתוך shell) | (redirect ל-`overview`) | יורש guard מההורה | — | `Frontend/src/app/admin/admin.routes.ts:14` |
| `overview` | `AdminOverviewComponent` | יורש guard מההורה | כן | `Frontend/src/app/admin/admin.routes.ts:15-18` |
| `users` | `AdminUsersComponent` | יורש guard מההורה | כן | `Frontend/src/app/admin/admin.routes.ts:19-22` |
| `sms` | `AdminSmsComponent` | יורש guard מההורה | כן | `Frontend/src/app/admin/admin.routes.ts:23-26` |
| `logs` | `AdminLogsComponent` | יורש guard מההורה | כן | `Frontend/src/app/admin/admin.routes.ts:27-30` |
| `system` | `AdminSystemComponent` | יורש guard מההורה | כן | `Frontend/src/app/admin/admin.routes.ts:31-34` |

ה-`adminGuard` בודק סשן פעיל דרך `AdminAuthService.checkSession()` ומפנה ל-`/{adminRoutePath}/login` בכישלון (`Frontend/src/app/admin/admin.guard.ts:11-13`).

נתיב הבסיס של פאנל הניהול (`environment.adminRoutePath`) אינו מקושר משום מקום בממשק המשתמש (הערת קוד: "Hidden admin panel — deliberately not in the app's navigation, not localized" — `Frontend/src/app/app.routes.ts:7-9`), ומוגדר להדרה מאינדוקס מנועי חיפוש דרך header סטטי (`Frontend/src/assets/_headers:8-9`).

---

## 5. ניהול State

אין שימוש ב-NgRx, Akita, או כל ספריית state-management ייעודית אחרת (לא מופיע ב-`package.json`, לא נמצא import כלשהו). ניהול המצב מתבצע כולו באמצעות:

1. **Angular Signals** (`signal`/`computed`/`effect`) בתוך שירותי `providedIn: 'root'` — התבנית השלטת בפרויקט. דוגמאות:
   - `Frontend/src/app/services/notification.service.ts:10-11` (הודעת טוסט נוכחית).
   - `Frontend/src/app/services/error-state.service.ts:10-11` (מצב שגיאה גלובלי).
   - `Frontend/src/app/services/reading-history.service.ts:30-31` (מפת פרקים שנקראו).
   - `Frontend/src/app/services/reader-prefs.service.ts:41-47` (העדפות קריאה).
   - `Frontend/src/app/core/a11y/a11y.service.ts:15,32-39` (הגדרות נגישות + `effect` לסנכרון DOM/localStorage אוטומטי).
   - `Frontend/src/app/core/tts/tts.service.ts:38-49` (מצב נגן ההקראה).
   - `Frontend/src/app/admin/admin-date-range.service.ts:11-20` (טווח תאריכים בפאנל ניהול, כולל `computed` לגזירת הטווח בפועל).
2. **Persistence ל-`localStorage`** — כמעט כל שירות signal-based שומר את מצבו ל-`localStorage` (מפתחות ייחודיים כמו `tanach.readerPrefs.v1`, `tanach.readHistory.v1`, `tanach.a11y.v1`, `tanach.tts.v1`, `tanach.theme`, `tanach.username`, `tanach.reminderManageToken`) כך שהמצב שורד רענון דף. ראו למשל `Frontend/src/app/services/reader-prefs.service.ts:3,24-30,69-72`.
3. **RxJS Observables** — משמשים בעיקר לתקשורת HTTP חד-פעמית (`HttpClient` מחזיר `Observable`), לא לניהול state מתמשך. שימוש טיפוסי: `Frontend/src/app/services/api-call.service.ts` (כל המתודות מחזירות `Observable`).
4. **State מקומי בקומפוננטה** (`@Input`/שדות רגילים, לא signal) — לדוגמה שדות טופס פשוטים ב-`Frontend/src/app/components/subscribe/subscribe.component.ts:46-55`.
5. **`localStorage` ישיר ללא שכבת שירות** — חלק מהזרימות (לדוגמה "היכן המשתמש הפסיק לקרוא", `HasStorage`/`SectionRef`) נשמרות ישירות מתוך קומפוננטות (`Frontend/src/app/components/read-permission/read-permission.component.ts:94-95`, `Frontend/src/app/components/home/home.component.ts:25,73`) ולא דרך שירות ייעודי — קיימת הערת קוד מפורשת שהמנגנון הזה נפרד מ-`ReadingHistoryService` (`Frontend/src/app/services/reading-history.service.ts:25-27`).

אין Context API (זו לא React) ואין Redux/Store מרכזי גלובלי יחיד — כל תחום נתונים מנוהל בשירות `providedIn: 'root'` נפרד משלו.

---

## 6. תקשורת עם השרת (Server communication)

### מקור כתובת ה-API

`environment.apiUrl`, המוגדר ב-`Frontend/src/environments/environment.ts:3` (dev: `https://localhost:5001`) ומוחלף בבנייה production דרך `fileReplacements` (`Frontend/angular.json:85-90`) בקובץ `Frontend/src/environments/environment.production.ts:13`. **בקובץ ה-production כרגע הערך זהה לזה של dev** (`https://localhost:5001`) עם הערת TODO מפורשת שזהו placeholder שטרם הוחלף בכתובת production אמיתית לפני ההשקה (`Frontend/src/environments/environment.production.ts:1-4`).

באופן דומה, `environment.adminRoutePath` (הנתיב הסודי לפאנל הניהול, `admin-x9k2`) מסומן כ-placeholder שחייב הוחלפה ע"י צנרת הפריסה לפני production (`Frontend/src/environments/environment.production.ts:6-10`).

### `ApiCallService` — קריאות ציבוריות (Frontend/src/app/services/api-call.service.ts)

| מתודה | HTTP | Endpoint |
|---|---|---|
| `getHolidays()` | GET | `/JewishCalendar/getJewishCalendar` (`api-call.service.ts:15`) |
| `getVerses(book, chapter)` | GET | `/Tanakh/books/{book}/{chapter}` (`api-call.service.ts:19`) |
| `getBookList(section)` | GET | `/Tanakh/books/{section}` (`api-call.service.ts:23`) |
| `getBookByTitle(book)` | GET | `/Tanakh/books/main/{book}` (`api-call.service.ts:27`) |
| `requestOtp(phoneNumber)` | POST | `/api/v1/subscriptions/otp/request` (`api-call.service.ts:31`) |
| `subscribe(subscriptionRequest)` | POST | `/api/v1/subscriptions` (`api-call.service.ts:35`) |
| `getReminderPreferences(manageToken)` | GET | `/api/v1/subscriptions/me` (`api-call.service.ts:39`) |
| `updateReminderPreferences(...)` | POST | `/api/v1/subscriptions/me` (`api-call.service.ts:43-47`) |
| `unsubscribeReminder(manageToken)` | POST | `/api/v1/subscriptions/me/unsubscribe` (`api-call.service.ts:51`) |
| `updateReadingProgress(readingProgress)` | POST | `/api/v1/reading-progress` (`api-call.service.ts:55`) |
| `getMaintenanceStatus()` | GET | `/api/v1/system/maintenance` (`api-call.service.ts:59`) |
| `getAnnouncementBanner()` | GET | `/api/v1/system/banner` (`api-call.service.ts:63`) |

צרכני `ApiCallService`: `EntranceComponent` (`getHolidays`), `HomeComponent` (`getBookByTitle`), `BooklistComponent` (`getBookList`), `ChapterlistComponent` (`getBookByTitle`), `ChapterComponent` (`getVerses`, `updateReadingProgress`, `getBookByTitle`), `SubscribeComponent` (`requestOtp`, `subscribe`, `getReminderPreferences`, `updateReminderPreferences`, `unsubscribeReminder`), `MaintenanceService` (`getMaintenanceStatus`), `AnnouncementBannerComponent` (`getAnnouncementBanner`).

### שירותי הניהול (`admin/`) — כולם עם `withCredentials: true` (עוגיית סשן)

| שירות | Base URL | מתודות/Endpoints |
|---|---|---|
| `AdminAuthService` | `{apiUrl}/api/v1/admin/auth` | POST `/login`, POST `/verify-otp`, POST `/logout`, GET `/session` (`admin-auth.service.ts:14-31`) |
| `AdminStatsService` | `{apiUrl}/api/v1/admin/stats` | GET `/overview` (`admin-stats.service.ts:15-20`) |
| `AdminUsersService` | `{apiUrl}/api/v1/admin/users` | GET (list), PATCH `/{id}` (block/unblock), DELETE `/{id}` (`admin-users.service.ts:25-42`) |
| `AdminSmsService` | `{apiUrl}/api/v1/admin/sms` | GET `/balance`, GET `/stats`, GET `/log`, POST `/test` (`admin-sms.service.ts:24-41`) |
| `AdminLogsService` | `{apiUrl}/api/v1/admin/logs` | GET (list), GET `/top`, PATCH `/{id}/resolve`, POST `/cleanup` (`admin-logs.service.ts:25-42`) |
| `AdminSystemService` | `{apiUrl}/api/v1/admin/system` | GET/PUT `/maintenance`, GET/PUT/DELETE `/banner`, GET `/health`, GET/PUT/DELETE `/flags(/{name})` (`admin-system.service.ts:15-49`) |
| `AdminExportService` | `{apiUrl}/api/v1/admin/export/{resource}` | GET (blob, `resource` ∈ `users`/`sms-log`/`error-log`) (`admin-export.service.ts:13-20`) |

כל מתודות ה-POST/PUT/PATCH/DELETE בפרויקט (הן הציבוריות והן הניהוליות) אינן עוברות דרך `retryInterceptor` (המוגבל ל-GET בלבד, ראו סעיף 7).

---

## 7. טיפול בשגיאות, loaders/spinners ו-HTTP interceptors

### HTTP Interceptors (`Frontend/src/app/core/interceptors`, נרשמים ב-`Frontend/src/app/app.config.ts:29`)

1. **`retryInterceptor`** (`Frontend/src/app/core/interceptors/retry.interceptor.ts`) — פועל רק על בקשות `GET` (`retry.interceptor.ts:6`); מבצע עד 2 ניסיונות חוזרים עם backoff מעריכי (`500ms * 2^n`), אך לא מנסה שוב על שגיאות 4xx (`retry.interceptor.ts:9-16`).
2. **`errorInterceptor`** (`Frontend/src/app/core/interceptors/error.interceptor.ts`) — ממפה קודי סטטוס (`0, 400, 401, 403, 404, 429, 500, 503`) להודעות שגיאה בעברית ומציג אותן דרך `NotificationService.showError` (`error.interceptor.ts:6-24`), ורושם ל-console (`error.interceptor.ts:22`).

### Global error handler
`GlobalErrorHandler` (`Frontend/src/app/core/global-error-handler.ts`) — מוזרק כ-`ErrorHandler` גלובלי (`Frontend/src/app/app.config.ts:24`). מזהה שגיאת טעינת chunk (deploy חדש בזמן שהמשתמש פתוח באפליקציה) ומציג הנחיית רענון דרך `ErrorStateService.showReloadPrompt` (`global-error-handler.ts:12-17`); אחרת מציג מסך שגיאה fatal (`showFatal()`) ומדווח (`report()` — פונקציה ריקה בכוונה, עם TODO לחיבור לשירות ניטור שגיאות חיצוני, ראו `global-error-handler.ts:23-28`).

### מסכי מצב גלובליים
- `ErrorStateService` (`Frontend/src/app/services/error-state.service.ts`) מנהל signal `{ kind: 'none' | 'fatal' | 'reload', message? }`, ונצרך ב-`AppComponent`'s template כדי להציג `ErrorScreenComponent` במקום ה-`router-outlet` (`Frontend/src/app/app.component.html:50-57`).
- `NotificationService` (`Frontend/src/app/services/notification.service.ts`) מציג "טוסט" (הודעה צפה) עם auto-dismiss אחרי 6 שניות; מוצג ב-`Frontend/src/app/app.component.html:5-10`.
- `MaintenanceScreenComponent` — מוצג כאשר `MaintenanceService.status().enabled === true`, קודם ל-router-outlet (`Frontend/src/app/app.component.html:51-52`).

### Loaders / Spinners
לא נמצא spinner גלובלי מבוסס-interceptor (אין HTTP loading interceptor הבודק isLoading גלובלי). אינדיקציות טעינה מנוהלות per-component:
- `SubscribeComponent` — progress-bar מדומה (setInterval, לא מבוסס התקדמות אמיתית) בזמן שליחת טופס (`Frontend/src/app/components/subscribe/subscribe.component.ts:289-311`).
- `ReadPermissionComponent` — progress-bar דומה (`Frontend/src/app/components/read-permission/read-permission.component.ts:38-55`).
- `EntranceComponent` — `isLoading` signal בזמן קריאת `getHolidays()` (`Frontend/src/app/components/entrance/entrance.component.ts:16,30-49`).
- שירותי admin (`admin-*.service.ts` בשילוב הקומפוננטות שלהם) — signal `loading`/`busy` per-request, למשל `Frontend/src/app/admin/users/admin-users.component.ts:31,95-113`.
- `data`/`loadError` signals ב-`ChapterComponent`, `BooklistComponent`, `ChapterlistComponent` (מצב "undefined" עד הגעת הנתונים).

---

## 8. עיצוב (Styling)

### טכנולוגיה
SCSS + CSS רגיל (component-scoped `.scss`/`.css` ליד כל קומפוננטה, בתוספת global styles). **אין Tailwind** (ראו סעיף 2). ספריית UI: **Angular Material** (`@angular/material`, prebuilt theme `indigo-pink` — ראו להלן), בשימוש בעיקר לדיאלוגים (`MatDialog`), טבלאות/pagination בפאנל הניהול (`MatTableModule`, `MatPaginatorModule`), ואייקונים (`MatIcon`).

### קבצי style גלובליים ($סדר הטעינה$ לפי `Frontend/angular.json:47-52`)
1. `@angular/material/prebuilt-themes/indigo-pink.css` — ערכת נושא בסיסית של Material (בהירה, קבועה).
2. `@angular/cdk/a11y-prebuilt.css`.
3. `Frontend/src/styles/a11y.scss` — קובץ כניסה שמייבא (`@use`) חמישה partials: `tokens`, `fonts`, `components`, `a11y-modes`, `a11y-utils` (`Frontend/src/styles/a11y.scss:1-7`).
4. `Frontend/src/styles.css` — קובץ נפרד ברמת שורש `src/`.

### פלטת צבעים וגופנים — **שני מקורות נפרדים קיימים בפועל**:

**א. `Frontend/src/styles/_tokens.scss`** (נטען דרך `a11y.scss`, מוקדם יותר בסדר) — מוגדר כמקור האמת הנוכחי לפי הערת קוד ("הApplication Design system … היא the design system") (`Frontend/src/styles/_tokens.scss:54-71`):
- צבעים: `--color-bg: #f5ead8`, `--color-surface: #ebddc5`, `--color-text: #201e1d`, `--color-accent: #c67139`, `--color-accent-2: #7a8a5e` ועוד סולמות `neutral-100..900`/`accent-100..900`/`accent-2-100..900` (`_tokens.scss:74-121`).
- גופנים: `--font-heading: "Frank Ruhl Libre", "Noto Serif Hebrew", serif`, `--font-body: "Rubik", "Segoe UI", Arial, sans-serif`, `--font-scripture: "Noto Serif Hebrew", "Times New Roman", serif` (`_tokens.scss:125-129`), עם הערת קוד מפורשת שזו החלפה מכוונת לגופני Caprasimo/Figtree המקוריים כי אין להם glyphs בעברית (`_tokens.scss:60-65`).
- Dark mode: `@mixin dark-palette` (`_tokens.scss:182-201`), מיושם דרך `Frontend/src/styles/_a11y-modes.scss:140-148` (הן ל-`prefers-color-scheme: dark` והן ל-`[data-theme="dark"]` הידני שמגדיר `Frontend/src/app/services/theme.service.ts:39`).

**ב. `Frontend/src/styles.css`** (נטען אחרון, `Frontend/angular.json:51`) — מגדיר **`:root` נפרד** עם ערכי צבע דומים אך לא זהים (`--color-bg: #f5ead8` זהה, אך `--font-heading: 'Caprasimo', system-ui, sans-serif` ו-`--font-body: 'Figtree', system-ui, sans-serif` — כלומר בדיוק הגופנים ש-`_tokens.scss` מציין כלא-תומכי-עברית — `Frontend/src/styles.css:43-46`), וכן `--color-whatsapp`/`--color-whatsapp-700` (`styles.css:40-41`) שאינם קיימים ב-`_tokens.scss`. קובץ זה מכיל גם CSS reset, כללי טיפוגרפיה (`h1..h6`, `p`, `a`), רכיבי `.btn`/`.input`/`.card`/`.tag`, ועקיפות ל-Material (`.mat-mdc-dialog-surface`, טבלאות Material במצב כהה — `styles.css:256-299`).

מכיוון ש-`styles.css` נטען **אחרי** `a11y.scss` (סעיף "סדר הטעינה" למעלה) ושתי הבחירות מגדירות אותם custom properties על `:root`, הצהרות `styles.css` גוברות במקרה של התנגשות (לפי סדר ה-cascade הרגיל של CSS). לא נמצא בקוד תיעוד המסביר במפורש איזה משני הקבצים נחשב "הפעיל בפועל" הלכה למעשה בכל שילוב — ראו סעיף "לא ידוע" למטה.

### גופנים בפועל שנטענים בדפדפן (`Frontend/src/index.html:27`)
Google Fonts: `Rubik` (400/500/600/700) ו-`Frank Ruhl Libre` (400/500/700), בתוספת `Material Icons` (`Frontend/src/index.html:27-28`). כמו כן קובץ `Frontend/src/styles/_fonts.scss` מגדיר `@font-face` self-hosted ל-`Noto Serif Hebrew` (400/700, WOFF2, subset לטווח יוניקוד עברי) עבור טקסט הפסוקים (`_fonts.scss:1-21`), עם קבצי הפונט תחת `Frontend/src/assets/fonts/noto-serif-hebrew-400.woff2` ו-`...-700.woff2`, וקישור `<link rel="preload">` ל-400 (`Frontend/src/index.html:9`). לא נמצא טעינה בפועל של Caprasimo/Figtree (המוזכרים ב-`styles.css`) — לא ב-`index.html` ולא כ-`@font-face` self-hosted.

### פרטים נוספים
- Breakpoints משותפים ב-`Frontend/src/styles/_breakpoints.scss` (מובייל-first mixins `from-xs`..`from-xxl`).
- מחלקות עזר עיצוביות משותפות (`.btn`, `.card`, `.dialog*`, `.field`, `.input`, `.tag`, `.switch`, `.seg-pill`) ב-`Frontend/src/styles/_components.scss`.
- מצבי נגישות ויזואליים (ניגודיות גבוהה/הפוכה, גווני אפור, הדגשת קישורים, גופן קריא, ריווח טקסט מוגבר, עצירת אנימציות, סמן מוגדל, סרגל קריאה) ב-`Frontend/src/styles/_a11y-modes.scss`, מיושמים כמחלקות על `<html>` ע"י `A11yService`.
- `Frontend/src/styles/_a11y-utils.scss` — `.visually-hidden`, focus ring גלובלי (`:focus-visible`), ו-`prefers-reduced-motion`.
- `theme-color` ב-`<meta>` תואם את `--color-bg` (`#f5ead8`) — `Frontend/src/index.html:5`.

---

## לא ידוע / דורש אימות

- **התנגשות טוקנים בין `Frontend/src/styles/_tokens.scss` ל-`Frontend/src/styles.css`**: שני הקבצים מגדירים `:root` עם `--color-*`/`--font-*` שונים חלקית (בפרט `--font-heading`/`--font-body` — Frank Ruhl Libre/Rubik מול Caprasimo/Figtree), ושניהם נטענים בפועל דרך `Frontend/angular.json:47-52`. חיפשתי הערת קוד או מסמך שמצהיר במפורש איזה מהם "המקור החי" בפריסה בפועל — לא נמצא הסבר מפורש מעבר להערות בתוך `_tokens.scss` עצמו (שמתארות רק את הכוונה ההיסטורית של המעבר, לא את מצב `styles.css` הנוכחי). לא ניתן לקבוע מקוד בלבד האם `styles.css` הוא שריד (dead file בכוונה) או קובץ פעיל שנועד לשמש לצרכים ספציפיים (למשל reset/Material overrides) תוך כוונה שה-`:root` הכפול שבו לא באמת ישפיע (ייתכן שהוא חופף בטעות).
- **גרסת `@angular/material`/`@angular/cdk`/`rxjs` המדויקת המותקנת**: דיווחתי את הגרסה שמופיעה בפועל תחת `node_modules` עבור `@angular/core`/`@angular/material` (22.1.0) ו-`gematriya`/`typescript` (2.0.0 / 6.0.3 בהתאמה); לא בדקתי את כל שאר החבילות ב-`node_modules` שורה-שורה מול `package-lock.json` (רק את הטווח המוצהר ב-`package.json`) — ייתכנו הבדלים דקים בין הטווח המוצהר (`^22.1.0` וכו') לגרסת ה-resolve המדויקת של כל תת-חבילה.
- **`istanbul-lib-instrument` (devDependency, גרסה `^6.0.3`)**: לא נמצא import ישיר בקוד הפרויקט; זוהתה כתלות המשמשת בעקיפין את שרשרת כיסוי הבדיקות (`karma-coverage`, שמכיל גרסה פנימית 5.2.1 נפרדת משלו לפי `package-lock.json`). לא הצלחתי לאתר קובץ קונפיגורציה (`karma.conf.js` אינו קיים בריפו — הקונפיגורציה מגיעה כולה מברירת המחדל של ה-builder ב-`Frontend/angular.json:118-138`) שמפנה ישירות לחבילה הזו בשם, כך שלא ניתן לאשר במלואו מדוע גרסה 6.0.3 מוצהרת בנפרד ב-`package.json` ולא רק כתלות טרנזיטיבית.
- **`environment.adminRoutePath` בפרודקשן**: `Frontend/src/environments/environment.production.ts:16` מכיל כרגע את הערך הליטרלי `admin-x9k2` (זהה לסביבת הפיתוח), עם הערת TODO מפורשת שהוא "must be overwritten... by the deploy pipeline before this build is ever published" (`environment.production.ts:6-10`). לא נמצא בקוד ה-Frontend את הערך הסופי שבאמת ישמש בפרודקשן (תלוי בצנרת פריסה חיצונית שלא נבדקה כחלק ממשימה זו).
- **קיום קובץ `karma.conf.js` נפרד**: לא נמצא בשורש `Frontend/` — אומת שהבדיקות רצות לפי ברירת המחדל של ה-builder בלבד (`Frontend/angular.json:118-138`); אם קיים קובץ כזה במקום אחר בריפו לא הצלחתי לאתרו בחיפוש שביצעתי תחת `Frontend/`.
