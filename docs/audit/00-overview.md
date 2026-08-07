# סקירת-על של הפרויקט — Tanakh

> מסמך זה הוא חלק ממיפוי טכני מלא של הפרויקט לקראת עלייה לפרודקשן. הוא מבוסס אך ורק על קריאת הקוד והקבצים בריפו, נכון לתאריך המיפוי. כל עובדה מגובה בקובץ ושורה. שום דבר כאן אינו המלצה — זהו תיאור עובדתי בלבד.

## מה הפרויקט עושה

לפי מבנה הקוד, זהו אתר לקריאת תנ"ך בעברית. יש Backend ב-.NET שחושף API לתוכן התנ"ך (`Backend/Tanakh.Api/Controllers/TanakhController.cs`) ולוח שנה עברי (`Backend/Tanakh.Api/Controllers/JewishCalendarController.cs`), ומאפשר למשתמשים לשמור התקדמות קריאה (`Backend/Tanakh.Api/Controllers/ReadingProgressController.cs`) ולהירשם לתזכורות (`Backend/Tanakh.Api/Controllers/SubscriptionsController.cs`) שנשלחות דרך SMS (ראו `Backend/README.md:7`: "reminders are sent via SMS4FREE - email is no longer used for anything, reminders or otherwise"). לצד זה קיים פאנל ניהול (Admin) עם התחברות מוגנת ב-OTP (`Backend/Tanakh.Api/Controllers/AdminAuthController.cs`), ניהול משתמשים, לוגים, סטטיסטיקות, מכסת SMS וניהול מערכת (`Backend/Tanakh.Api/Controllers/Admin*Controller.cs`).

ה-Frontend הוא אפליקציית Angular 22 (`Frontend/package.json:17`) עם ממשק בעברית (RTL), הכולל רכיבי קריאה של ספרים/פרקים (`Frontend/src/app/components/booklist`, `Frontend/src/app/components/chapter`, `Frontend/src/app/components/chapterlist`), הרשמה לתזכורות (`Frontend/src/app/components/subscribe`), תמיכת הקראה (TTS — `Frontend/src/app/core/tts`, `Frontend/src/app/shared/tts`), תפריט נגישות מובנה (`Frontend/src/app/core/a11y`, `Frontend/src/app/shared/a11y`), ופאנל ניהול נפרד תחת `Frontend/src/app/admin`.

## עץ תיקיות ברמה העליונה

```
Tanakh/
├── Backend/            # שרת ה-API ב-.NET 10 (Web API + Domain + Infrastructure + Tests)
├── Frontend/           # אפליקציית הלקוח ב-Angular 22
├── docs/               # תיעוד פרויקט: ADR-ים, checklists, runbooks, ומסמך זה (docs/audit)
├── .github/workflows/  # הגדרות CI/CD של GitHub Actions
├── .claude/            # הגדרות מקומיות של Claude Code (settings.local.json)
├── .env                # קובץ סביבה ברמת השורש (שמות משתנים בלבד מתועדים ב-07-infra-and-deploy.md)
└── .gitignore
```

### תת-תיקיות עיקריות — Backend

```
Backend/
├── Tanakh.Api/              # פרויקט ה-Web API: Controllers, Program.cs (נקודת הכניסה), Auth, appsettings
├── Tanakh.Domain/            # שכבת דומיין: ממשקים (I*Service), ישויות, ולידציה, תזמון, SMS, auditing
├── Tanakh.Infrastructure/    # מימוש תשתיתי: EF Core (Data), מיגרציות, Reminders, Retention, Seeding, Services
├── Tanakh.Tests/              # פרויקט בדיקות יחידה
├── db/                        # קבצי init/roles/dumps של ה-DB (ל-docker-compose)
├── docker-compose.yml         # הרצת PostgreSQL מקומי
├── Dockerfile                 # בניית אימג' פרודקשן לבאקנד
├── global.json                # גרסת ה-.NET SDK
└── README.md                  # תיעוד קונפיגורציה (משתני סביבה, seed, reset)
```

### תת-תיקיות עיקריות — Frontend

```
Frontend/
├── src/app/
│   ├── admin/          # פאנל ניהול (login, overview, logs, sms, system, users, shell, shared)
│   ├── components/     # רכיבי המסך הציבורי: booklist, chapter, chapterlist, entrance, home, settings, subscribe, welcome-modal ועוד
│   ├── core/            # שירותי ליבה: a11y, interceptors, tts
│   ├── shared/          # רכיבים/עזרים משותפים: a11y, announcement-banner, cookie-banner, legal, error-screen, maintenance-screen, tts
│   ├── services/        # שירותי תקשורת עם ה-API ולוגיקה משותפת
│   └── models/           # טיפוסי TypeScript
├── src/environments/    # קונפיגורציית סביבה (dev/production)
├── src/locale/           # קבצי תרגום XLIFF (Angular i18n)
├── src/assets/           # תמונות, אייקונים, פונטים
├── e2e/                  # בדיקות Playwright (כולל a11y.spec.ts)
└── dist/                 # פלט build (נוצר בזמן build, לא חלק מהמקור)
```

*(פירוט מלא של כל קובץ בכל תיקייה מופיע במסמכים הייעודיים — ראו תוכן העניינים למטה.)*

## טבלת סטאק טכנולוגי

| שכבה | טכנולוגיה | גרסה מדויקת | הקובץ שממנו נלקחה הגרסה |
|---|---|---|---|
| Frontend | Angular (core/cli/cdk/material/forms/router/animations/service-worker) | `^22.1.0` (`@angular/cli` ו-`@angular-devkit/build-angular`: `^22.1.2`) | `Frontend/package.json:17-27,34-37` |
| Frontend | TypeScript | `~6.0.3` | `Frontend/package.json:52` |
| Frontend | RxJS | `~7.8.0` | `Frontend/package.json:30` |
| Frontend | Angular i18n (`@angular/localize`) | `^22.1.0` | `Frontend/package.json:37` |
| Frontend — בדיקות | Karma + Jasmine | `karma ~6.4.0`, `jasmine-core ~5.1.0` | `Frontend/package.json:43,47` |
| Frontend — בדיקות E2E/נגישות | Playwright + `@axe-core/playwright` + Lighthouse CI | `@playwright/test ^1.62.1`, `@axe-core/playwright ^4.12.1`, `@lhci/cli ^0.15.1` | `Frontend/package.json:38-40` |
| Backend | .NET SDK / ASP.NET Core Web API | `net10.0` (SDK `10.0.302`) | `Backend/Tanakh.Api/Tanakh.Api.csproj:4`, `Backend/global.json:3` |
| Backend | Entity Framework Core (Design) | `10.0.10` | `Backend/Tanakh.Api/Tanakh.Api.csproj:20` |
| Backend | OpenAPI / Scalar (תיעוד API) | `Microsoft.AspNetCore.OpenApi 10.0.10`, `Microsoft.OpenApi 2.11.0`, `Scalar.AspNetCore 2.16.16` | `Backend/Tanakh.Api/Tanakh.Api.csproj:19,24,29` |
| Database | PostgreSQL | `16` (תמונת `postgres:16-alpine`) | `Backend/docker-compose.yml:3` |
| Infra — Backend image | Docker, מבוסס `mcr.microsoft.com/dotnet/sdk:10.0` (build) ו-`mcr.microsoft.com/dotnet/aspnet:10.0` (runtime) | `10.0` | `Backend/Dockerfile:1,13` |

פירוט תלויות מלא (כולל כל חבילה בנפרד ומקום השימוש בקוד) נמצא ב-`01-frontend.md` וב-`03-backend.md`.

## הרצה לוקאלית

### Frontend
פקודות מוגדרות תחת `scripts` ב-`Frontend/package.json:4-14`:

| פקודה | מטרה |
|---|---|
| `npm run start` → `ng serve --ssl` | הרצת שרת פיתוח עם SSL |
| `npm run build` → `ng build` | בנייה (ברירת מחדל: production, ראו `Frontend/angular.json:98`) |
| `npm run watch` → `ng build --watch --configuration development` | בנייה מתמשכת לפיתוח |
| `npm test` → `ng test` | בדיקות יחידה ב-Karma |
| `npm run e2e:a11y` → `playwright test` | בדיקות E2E לנגישות |
| `npm run lighthouse:a11y` → `lhci autorun` | בדיקות Lighthouse |
| `npm run lint:css` → `stylelint "src/**/*.{css,scss}"` | linting ל-CSS/SCSS |
| `npm run verify` → `ng test --watch=false --browsers=ChromeHeadless && ng build --configuration production && npm audit --omit=dev --audit-level=high` | רצף בדיקה מלא |

מקור: `Frontend/package.json:4-14`.

### Backend
לפי `Backend/README.md`, הפיתוח המקומי דורש הגדרת user-secrets (`dotnet user-secrets set ...`, `Backend/README.md:52-59`) עבור מפתחות `Sms`, `Hashing:Pepper` ו-`Admin`. הרצת מסד הנתונים המקומי היא דרך `Backend/docker-compose.yml` (שירות `postgres`). פקודות ייעודיות לזריעת נתונים/איפוס סכמה מתועדות ב-`Backend/README.md:95,100`: `dotnet run -- --seed` ו-`dotnet run -- --reset-db` (חסומות מחוץ לסביבת `Development`, `Backend/README.md:103`).

פירוט מלא של משתני הסביבה, האימות, וה-endpoints נמצא ב-`03-backend.md`.

## Build לפרודקשן

**Frontend**: `ng build` עם קונפיגורציית `production` כברירת מחדל (`Frontend/angular.json:98`), פלט לתיקייה `dist/tanakh` (`Frontend/angular.json:38`). ה-build כולל Service Worker (`ngsw-config.json`, `Frontend/angular.json:84`) והחלפת קובץ הסביבה ל-`environment.production.ts` (`Frontend/angular.json:85-90`). קיימת גם קונפיגורציית i18n המגדירה שפת מקור `he` ותרגום נוסף ל-`en` תחת `src/locale/messages.en.xlf` עם `baseHref: /en/` (`Frontend/angular.json:22-33`) — פירוט מלא של מנגנון זה נמצא ב-`02-frontend-json-and-i18n.md`.

**Backend**: `Backend/Dockerfile` מבצע `dotnet publish Tanakh.Api/Tanakh.Api.csproj -c Release -o /app` בשלב build (`Backend/Dockerfile:11`), ומריץ את התוצר בשלב runtime נפרד עם `ENV ASPNETCORE_URLS=http://+:8080` ו-`EXPOSE 8080` (`Backend/Dockerfile:16-17`), נקודת כניסה `dotnet Tanakh.Api.dll` (`Backend/Dockerfile:18`).

## תוכן עניינים — שאר מסמכי האודיט

- [`01-frontend.md`](01-frontend.md) — צד לקוח: פריימוורק, תלויות, מפת קבצים, ראוטים, ניהול סטייט, תקשורת עם השרת, סטיילינג.
- [`02-frontend-json-and-i18n.md`](02-frontend-json-and-i18n.md) — כל קבצי ה-JSON בפרונט, וניתוח מעמיק של מנגנון ה-i18n (`i18n="@@..."`).
- [`03-backend.md`](03-backend.md) — צד שרת: פריימוורק, תלויות, מפת קבצים, endpoints, אימות והרשאות, middlewares, משתני סביבה.
- [`04-database.md`](04-database.md) — מסד הנתונים: טבלאות, שדות, קשרים, ERD, אינדקסים, מיגרציות, seed data.
- [`05-services-and-integrations.md`](05-services-and-integrations.md) — שירותים חיצוניים ופנימיים, כולל ניתוח מעמיק של שירות התזכורות.
- [`06-accessibility.md`](06-accessibility.md) — נגישות: תקן, ווידג'ט, ARIA, ניווט מקלדת, RTL, alt text.
- [`07-infra-and-deploy.md`](07-infra-and-deploy.md) — תשתית: סביבות, CI/CD, Docker, קבצי קונפיגורציה.

## לא ידוע / דורש אימות

- לא אותרה עדות בקוד לסביבת **Staging** נפרדת (רק Development ו-Production נראות מוגדרות) — נבדק ב-`Frontend/src/environments/` וב-`Backend/Tanakh.Api/appsettings*.json`; אימות מלא ופירוט נמצאים ב-`07-infra-and-deploy.md`, שם זה נבדק לעומק על ידי סוכן ייעודי.
- יעד ה-hosting הסופי בפרודקשן (Render/Neon/Cloudflare Pages) מוזכר ב-`Backend/README.md:84-85` וב-`docs/LAUNCH-CHECKLIST.md`, אך לא אומת כאן אם קיימת כבר קונפיגורציה מחוברת בפועל (כגון סוד GitHub Actions לפריסה) — ראו בדיקה מפורטת ב-`07-infra-and-deploy.md`.
