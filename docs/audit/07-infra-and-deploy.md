# 07 — מפת תשתית ופריסה (Infra & Deploy) — מבוסס ראיות

מסמך זה ממפה את מצב התשתית/הפריסה בפועל של הריפו, כפי שהוא נקרא מקבצי הקונפיגורציה בפועל נכון לתאריך הכתיבה. כל טענה מצוטטת לקובץ ושורה מדויקים. טענות שמקורן במסמכים (docs) בלבד, ללא קובץ קונפיגורציה תואם בפועל, מסומנות ככאלה במפורש.

---

## 1. היכן האתר אמור לרוץ בפרודקשן — לפי ראיות קונפיגורציה

### 1.1 Backend/Dockerfile — מבנה מלא

הקובץ `Backend/Dockerfile` מגדיר בנייה דו-שלבית (multi-stage build):

- **שלב build** (`Backend/Dockerfile:1`): `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`, `WORKDIR /src` (`Backend/Dockerfile:2`).
  - מעתיק `global.json` (`Backend/Dockerfile:3`).
  - מעתיק את קבצי ה-`.csproj` בלבד של שלושת הפרויקטים `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Api` (`Backend/Dockerfile:4-6`) ומריץ `dotnet restore Tanakh.Api/Tanakh.Api.csproj` (`Backend/Dockerfile:7`) — טכניקת caching של שכבות Docker.
  - מעתיק את שאר קוד המקור של שלושת הפרויקטים (`Backend/Dockerfile:8-10`).
  - מריץ `dotnet publish Tanakh.Api/Tanakh.Api.csproj -c Release -o /app --no-restore` (`Backend/Dockerfile:11`).
- **שלב runtime** (`Backend/Dockerfile:13`): `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime`, `WORKDIR /app` (`Backend/Dockerfile:14`).
  - מעתיק את פלט ה-publish משלב ה-build (`Backend/Dockerfile:15`): `COPY --from=build /app .`
  - מגדיר `ENV ASPNETCORE_URLS=http://+:8080` (`Backend/Dockerfile:16`).
  - `EXPOSE 8080` (`Backend/Dockerfile:17`).
  - `ENTRYPOINT ["dotnet", "Tanakh.Api.dll"]` (`Backend/Dockerfile:18`).

**הערה עובדתית:** הדוקרפייל אינו מגדיר `ASPNETCORE_ENVIRONMENT` בשום שלב (נבדק במפורש — אין מופע של `ASPNETCORE_ENVIRONMENT` בקובץ). הדוקרפייל גם אינו מכיל שום שלב שמעתיק קבצי frontend, ואינו מתקין nginx או שרת קבצים סטטיים כלשהו — הוא בונה ומריץ אך ורק את ה-API של ה-backend.

### 1.2 Backend/docker-compose.yml

הקובץ מגדיר שירות יחיד בשם `postgres` (`Backend/docker-compose.yml:2`):
- `image: postgres:16-alpine` (`Backend/docker-compose.yml:3`), `container_name: tanakh-postgres` (`Backend/docker-compose.yml:4`), `restart: unless-stopped` (`Backend/docker-compose.yml:5`).
- משתני סביבה: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` — נלקחים ממשתני סביבה חיצוניים (`Backend/docker-compose.yml:7-9`).
- מיפוי פורטים: `"${POSTGRES_PORT:-5432}:5432"` (`Backend/docker-compose.yml:10-11`).
- volumes: `tanakh_pgdata:/var/lib/postgresql/data` וגם `./db/init:/docker-entrypoint-initdb.d:ro` (`Backend/docker-compose.yml:12-14`).
- healthcheck עם `pg_isready` (`Backend/docker-compose.yml:15-19`).

**אין בקובץ זה שום שירות עבור ה-API עצמו ואין שירות עבור ה-frontend** — כלומר `docker-compose.yml` הזה משמש להרצת מסד נתונים Postgres מקומי בלבד (לפיתוח), לא לפריסת המערכת כולה. תיקיית `Backend/db/init` מכילה את `01-extensions.sql`, ותיקיית `Backend/db` מכילה גם `Backend/db/dumps/pg_dump.sh` ו-`Backend/db/roles/{app_user.sql,migrations_user.sql,verify.sh}`.

### 1.3 חיפוש אחר קבצי קונפיגורציית פריסה נוספים

בוצע חיפוש רקורסיבי בכל הריפו (למעט `node_modules`, `.git`, `.angular`) אחר: `vercel.json`, `netlify.toml`, `render.yaml`, `fly.toml`, קבצי Azure/AWS (`*.bicep`, `app.yaml`), `Procfile`, `web.config`, `azure-pipelines*`, קבצי nginx (`nginx.conf`, `*.nginx*`), ו-`Dockerfile` נוסף כלשהו. **התוצאה: לא נמצא אף אחד מהקבצים הללו בכל הריפו**, מלבד `Backend/Dockerfile` ו-`Backend/docker-compose.yml` שכבר תוארו. אין קובץ Dockerfile עבור ה-Frontend.

### 1.4 קובץ Cloudflare Pages בפועל — `Frontend/src/assets/_headers`

נמצא קובץ קונפיגורציה קונקרטי ספציפי ל-Cloudflare Pages: `Frontend/src/assets/_headers`. הקובץ עצמו מתעד את מטרתו (`Frontend/src/assets/_headers:1`): "Cloudflare Pages static-response headers". הוא מגדיר כותרת HTTP `X-Robots-Tag: noindex, nofollow` עבור הנתיב `/admin-x9k2/*` (`Frontend/src/assets/_headers:8-9`). הערת קוד בקובץ (`Frontend/src/assets/_headers:2-3`) מציינת שהוא מועתק אוטומטית על-ידי `ng build` מ-`src/assets` אל שורש פלט ה-build (`dist/tanakh`), לצד `index.html`. הערת `TODO(LAUNCH)` בקובץ (`Frontend/src/assets/_headers:5-7`) מציינת שהנתיב `admin-x9k2` הוא placeholder שחייב להתאים בדיוק ל-`adminRoutePath` שב-`environment.production.ts`, ושניהם יוחלפו על-ידי צינור הפריסה (deploy pipeline) לפני build פרודקשן אמיתי.

זהו הממצא הקונקרטי היחיד בריפו התומך בטענת "Cloudflare Pages" (ראו גם סעיף "לא ידוע" למטה לגבי מסמכים המזכירים Render/Neon/Cloudflare Pages כיעד מתוכנן ללא קובץ קונפיגורציה תואם עבור Render/Neon).

### 1.5 פלט ה-build של ה-Frontend (`Frontend/dist`)

נצפה בפועל תוכן התיקייה `Frontend/dist/tanakh/`:
- `Frontend/dist/tanakh/browser/` — מכיל `index.html`, קובצי JS מפוצלים (`chunk-*.js`, `main-*.js`, `polyfills-*.js`), `styles-*.css`, `favicon.ico`, `manifest.webmanifest`, `ngsw-worker.js`, `ngsw.json`, `safety-worker.js`, `worker-basic.min.js`, ותיקיית `assets/` (הכוללת את `_headers`).
- `Frontend/dist/tanakh/prerendered-routes.json` — תוכנו בפועל הוא `{"routes": {}}` (ריק), כלומר אין כרגע נתיבים שעברו prerender בפלט הבנייה הנוכחי.
- `Frontend/dist/tanakh/3rdpartylicenses.txt`.

מבנה זה (רק `browser/`, ללא תיקיית שרת/`server/`) עקבי עם `Frontend/angular.json:36` שמגדיר את ה-builder כ-`@angular-devkit/build-angular:application` המפיק bundle דפדפן בלבד (ללא יעד שרת Node).

### 1.6 מסמך ADR 003 — החלטת SSR/SSG

`docs/adr/003-frontend-ssr-decision.md:1-8` מתעד החלטה (Accepted, 2026-07-31) לדחות את המימוש של SSR/SSG עד שיהיה דומיין פרודקשן, כאשר היעד שנקבע (כשימומש) הוא SSG מלא (build-time prerendering) ולא SSR דינמי (`docs/adr/003-frontend-ssr-decision.md:37-45`). המסמך מציין במפורש (`docs/adr/003-frontend-ssr-decision.md:26`): "there is no production domain or hosting decision yet". זה עקבי עם הממצא בסעיף 1.5 שפלט ה-build כרגע הוא SPA טהור בצד לקוח (ללא prerender בפועל).

---

## 2. סביבות (Environments)

### 2.1 Frontend — `Frontend/src/environments/`

קיימים שני קבצים בלבד: `environment.ts` (`Frontend/src/environments/environment.ts`) ו-`environment.production.ts` (`Frontend/src/environments/environment.production.ts`). אין קובץ `environment.staging.ts` או דומה.

ההחלפה ביניהם מוגדרת ב-`Frontend/angular.json:85-90` תחת קונפיגורציית ה-build בשם `production`, באמצעות `fileReplacements`: `src/environments/environment.ts` מוחלף ב-`src/environments/environment.production.ts`.

מבנה `environment.ts` (`Frontend/src/environments/environment.ts:1-10`): המפתחות הם `production` (`false`), `apiUrl` (`'https://localhost:5001'`), `enableServiceWorker` (`false`), `logLevel` (`'debug'`), `adminRoutePath` (`'admin-x9k2'`).

מבנה `environment.production.ts` (`Frontend/src/environments/environment.production.ts:11-17`): אותם מפתחות בדיוק — `production` (`true`), `apiUrl`, `enableServiceWorker` (`true`), `logLevel` (`'error'`), `adminRoutePath`. **הבדל מבני:** אין הבדל במפתחות בין שני הקבצים — רק בערכים. הערת קוד בראש הקובץ (`Frontend/src/environments/environment.production.ts:1-10`) מציינת במפורש שהערך של `apiUrl` הוא placeholder זמני שמצביע על localhost כי "no production domain exists yet", ושחייב להיות מוחלף בפועל על-ידי צינור הפריסה לפני פרסום.

### 2.2 Backend — `appsettings.json` מול `appsettings.Development.json`

נבדק תוכן שני הקבצים תחת `Backend/Tanakh.Api/`:

- **`appsettings.json`** (הבסיס, נטען בכל הסביבות): מכיל אך ורק את המקטעים `Logging.LogLevel` (עם `Default`, `Microsoft`, `Microsoft.Hosting.Lifetime`) ו-`AllowedHosts` (`Backend/Tanakh.Api/appsettings.json:1-10`). **אין** בקובץ זה מקטעי `ConnectionStrings`, `Reminders` או `Cors`.
- **`appsettings.Development.json`**: מכיל את `Logging.LogLevel` (אותו מבנה), ובנוסף שלושה מקטעים שאינם קיימים ב-`appsettings.json` הבסיסי: `ConnectionStrings.AppDb` (`Backend/Tanakh.Api/appsettings.Development.json:9-11`, הערך מכיל מחרוזת חיבור עם אשראי — **לא משוכפל כאן, [REDACTED]**), `Reminders.PublicBaseUrl` ו-`Reminders.ApiBaseUrl` (`Backend/Tanakh.Api/appsettings.Development.json:12-15`, שני הערכים מצביעים ל-`localhost`), ו-`Cors.AllowedOrigins` (מערך עם ערך יחיד `https://localhost:4200`, `Backend/Tanakh.Api/appsettings.Development.json:16-18`).

**לא קיים** קובץ `appsettings.Production.json` בריפו (נבדק במפורש — לא נמצא תחת `Backend/Tanakh.Api/`).

לפי `Backend/README.md:66-81`, בסביבת production אותם מפתחות (`Sms:*`, `Hashing:Pepper`, `Admin:*`, `Cors:AllowedOrigins:0`) מסופקים כמשתני סביבה בפורמט `__` (קו תחתון כפול) במקום `:` (למשל `Sms__Key`, `Cors__AllowedOrigins__0`) ולא דרך קובץ `appsettings.Production.json` — כלומר ההבדל המבני בין הסביבות הוא: הבסיס (`appsettings.json`) מוגדר לרוץ בפרודקשן ומקבל את הערכים החסרים (`ConnectionStrings`, `Cors`, `Sms`, `Hashing`, `Admin` וכו') ממשתני סביבה, בעוד `appsettings.Development.json` מגדיר חלק מהם (`ConnectionStrings.AppDb`, `Reminders`, `Cors`) ישירות כקובץ JSON מקומי.

---

## 3. איך ה-Frontend מוגש בפרודקשן ואיך הוא מוצא את ה-Backend

### 3.1 הגשת קבצים סטטיים

כאמור בסעיף 1.1, `Backend/Dockerfile` אינו מגיש קבצים סטטיים של ה-Frontend כלל — הוא בונה ומריץ רק את ה-API (`dotnet Tanakh.Api.dll`, `Backend/Dockerfile:18`). כן, נבדק גם `Backend/Tanakh.Api/Program.cs` לאיתור `UseStaticFiles`/`UseSpa`/`MapFallback` — לא נמצא אף אחד מהם בקובץ. כלומר אין בקוד ה-backend שום נתיב שמגיש קבצי HTML/JS/CSS של ה-Frontend, ואין reverse proxy מוגדר בריפו בין frontend ל-backend (לא נמצא קובץ nginx או קונפיגורציית proxy כלשהי, ראו סעיף 1.3).

הראיה הקונקרטית היחידה לאופן הגשת ה-Frontend היא קובץ ה-Cloudflare Pages `Frontend/src/assets/_headers` שתואר בסעיף 1.4, המרמז שה-Frontend מיועד להיות מוגש כאתר סטטי דרך Cloudflare Pages, נפרד מה-backend.

### 3.2 איך ה-Frontend מוצא את ה-Backend בכל סביבה

- **סביבת פיתוח (`environment.ts`):** `apiUrl: 'https://localhost:5001'` (`Frontend/src/environments/environment.ts:3`).
- **סביבת production (`environment.production.ts`):** `apiUrl: 'https://localhost:5001'` (`Frontend/src/environments/environment.production.ts:13`) — **אותו ערך בדיוק כמו בפיתוח**. כפי שצוין בסעיף 2.1, הערת TODO בקובץ (`Frontend/src/environments/environment.production.ts:1-4`) מבהירה שזהו placeholder זמני עד שייבחר דומיין production אמיתי.
- קובץ Service Worker: `Frontend/ngsw-config.json` מגדיר שני `dataGroups` עם URLs מוצפנים-קשיח (hardcoded) לקאשינג: `https://localhost:5001/Tanakh/books/**` (`Frontend/ngsw-config.json:34`) ו-`https://localhost:5001/JewishCalendar/**` (`Frontend/ngsw-config.json:46`) — שימו לב שאלו כתובות שונות מ-`environment.production.ts` (`5001` לעומת אזכור `44308` ב-`docs/LAUNCH-CHECKLIST.md`, ראו סעיף 5).
- לפי `docs/LAUNCH-CHECKLIST.md:9` (שורת L-01), יש להגדיר את כתובת ה-API האמיתית של פרודקשן בשני מקומות: `Frontend/src/environments/environment.production.ts` (`apiUrl`) וגם `Frontend/ngsw-config.json` (`dataGroups[].urls`) — משום שקובץ ה-JSON הסטטי של ה-Service Worker אינו יכול להפנות אל `environment.ts` בזמן ריצה.
- הרשאות CORS בצד ה-backend: `Backend/Tanakh.Api/Program.cs:243-245` קורא את `Cors:AllowedOrigins` מהקונפיגורציה ובונה מדיניות CORS מפורשת (`WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()`), כלומר רק origins המוגדרים במפורש (ב-`appsettings.Development.json` מקומית, או כמשתנה סביבה `Cors__AllowedOrigins__0` בפרודקשן לפי `Backend/README.md:80`) מורשים לקרוא ל-API עם credentials.

---

## 4. CI/CD — קבצי `.github/workflows/`

### 4.1 `backend-ci.yml`

**טריגר** (`.github/workflows/backend-ci.yml:3-11`): מופעל על `push` ועל `pull_request`, אך ורק כאשר יש שינוי בנתיבים `Backend/**` או ב-`.github/workflows/backend-ci.yml` עצמו (`paths` filter).

**Job יחיד:** `build-and-test`, רץ על `ubuntu-latest`, עם `working-directory` ברירת מחדל `Backend` (`.github/workflows/backend-ci.yml:14-18`). שלבים לפי סדר:
1. `actions/checkout@v4` (שורה 20).
2. `actions/setup-dotnet@v4` עם `global-json-file: Backend/global.json` (שורות 22-25).
3. "Guard against raw SQL query APIs" — סורק בגרפ אחר `FromSqlRaw`/`ExecuteSqlRaw` בקבצי `.cs` ונכשל (`exit 1`) אם נמצאו (שורות 33-38).
4. `dotnet restore` (שורות 40-41).
5. `dotnet build --no-restore -warnaserror` (שורות 43-44).
6. `dotnet test --no-build` (שורות 46-47).

### 4.2 `frontend-a11y-ci.yml`

**טריגר** (`.github/workflows/frontend-a11y-ci.yml:3-11`): מופעל על `push` ועל `pull_request`, אך ורק כאשר יש שינוי בנתיבים `Frontend/**` או ב-`.github/workflows/frontend-a11y-ci.yml` עצמו.

**Job יחיד:** `a11y`, רץ על `ubuntu-latest`, עם `working-directory` ברירת מחדל `Frontend` (שורות 14-18). שלבים לפי סדר:
1. `actions/checkout@v4` (שורה 20).
2. `actions/setup-node@v4` עם `node-version: "22"`, `cache: "npm"`, `cache-dependency-path: Frontend/package-lock.json` (שורות 22-27).
3. `npm ci` (שורות 29-30).
4. Stylelint — `npm run lint:css` (שורות 32-33).
5. התקנת דפדפני Playwright — `npx playwright install --with-deps chromium` (שורות 35-36).
6. Build — `npm run build` (שורות 38-39).
7. חבילת בדיקות נגישות axe-core — `npm run e2e:a11y`, עם משתנה סביבה `CI: true` (שורות 41-46).
8. תקציב נגישות Lighthouse — `npm run lighthouse:a11y` (שורות 48-49).
9. אם נכשל: העלאת דוח Playwright כ-artifact בשם `playwright-report`, נתיב `Frontend/playwright-report/`, נשמר 7 ימים (שורות 51-57, `if: failure()`).

### 4.3 `backend-backup.yml`

**טריגר** (`.github/workflows/backend-backup.yml:13-16`): מתוזמן (`schedule`) עם ביטוי cron `"0 3 * * *"` — כלומר **מופעל כל יום בשעה 3:00 בלילה (UTC)**. בנוסף מופעל ידנית דרך `workflow_dispatch: {}` (שורה 16).

**Job יחיד:** `backup`, רץ על `ubuntu-latest` (שורות 19-20). שלבים לפי סדר:
1. `actions/checkout@v4` (שורה 22).
2. "Take backup" — מתקין `postgresql-client` דרך `apt-get`, ואז מריץ `bash Backend/db/dumps/pg_dump.sh`, עם משתני סביבה `DATABASE_URL` (מתוך `secrets.DIRECT_DATABASE_URL`) ו-`BACKUP_DIR=./backup-out` (שורות 24-30).
3. "Upload backup artifact" — מעלה כ-artifact בשם `tanakh-db-backup-${{ github.run_id }}`, נתיב `backup-out/*.dump`, נשמר 90 יום (שורות 35-40).

הערות בראש הקובץ (`.github/workflows/backend-backup.yml:1-12`) מתעדות שה-workflow **תלוי ב-secret בשם `DIRECT_DATABASE_URL` שטרם הוגדר** (משום שטרם קיים פרויקט Neon עבור האפליקציה), ושה-workflow צפוי להיכשל בשלב "Take backup" עד שיוגדר.

---

## 5. קבצי `.env.example` בריפו — שמות משתנים בלבד

בוצע חיפוש אחר `.env.example` בכל הריפו (root, `Backend/`, `Frontend/`). **נמצא קובץ אחד בלבד:** `Backend/.env.example`. **לא נמצא** קובץ `.env.example` בשורש הריפו ולא תחת `Frontend/`.

### `Backend/.env.example` — שמות המשתנים המוגדרים (ללא ערכים)

| שם משתנה | מיקום |
|---|---|
| `POSTGRES_USER` | `Backend/.env.example:5` |
| `POSTGRES_PASSWORD` | `Backend/.env.example:6` |
| `POSTGRES_DB` | `Backend/.env.example:7` |
| `POSTGRES_PORT` | `Backend/.env.example:8` |
| `ConnectionStrings__AppDb` | `Backend/.env.example:15` |
| `ConnectionStrings__MigrationsDb` | `Backend/.env.example:16` |

הערות בקובץ (`Backend/.env.example:1-14`) מבהירות: הקובץ מיועד להעתקה ל-`.env` (המוחרג מ-git); `POSTGRES_USER`/`POSTGRES_PASSWORD` הם אשראי ה-superuser של Postgres להרצה מקומית של docker-compose (לא תפקיד ה-runtime של האפליקציה); שני משתני `ConnectionStrings__*` נקראים ישירות על-ידי האפליקציה/`dotnet-ef` בלבד, ודורשים הרצה קודמת (חד-פעמית) של `db/roles/migrations_user.sql` ואז `db/roles/app_user.sql`.

### קובץ `.env` בשורש הריפו

קיים קובץ `.env` בשורש הריפו (`.env`), אך **גודלו 0 בתים (ריק לחלוטין)** — אומת ישירות (`wc -c` על הקובץ החזיר 0). אין בו משתנים כלל.

### קובץ `Backend/.env`

קיים (נצפה ברשימת התיקייה `Backend/`), אך בהתאם לכללי המשימה (לא לגעת/לצטט ערכי secrets) תוכנו לא נקרא ולא מפורט כאן — סעיף זה של המשימה דורש רק את קבצי ה-`.env.example`.

---

## 6. קבצי קונפיגורציה ברמת השורש — root, `Backend/`, `Frontend/`

### שורש הריפו

| קובץ | מטרה |
|---|---|
| `.gitignore` | רשימת נתיבים/דפוסי קבצים המוחרגים מ-git עבור כל הריפו (למשל `.env`, תיקיות build של .NET כמו `bin/`/`obj/`, `.vs/`). |
| `.env` | קובץ סביבה בשורש הריפו — נמצא ריק (0 בתים), ראו סעיף 5. |

### `Backend/`

| קובץ | מטרה |
|---|---|
| `Backend/Dockerfile` | הגדרת בניית container דו-שלבית להרצת ה-API של ה-backend (ראו סעיף 1.1). |
| `Backend/docker-compose.yml` | הרצת שירות Postgres מקומי לצורכי פיתוח (ראו סעיף 1.2). |
| `Backend/.dockerignore` | מחריג את `bin/`, `obj/`, `.vs/`, `*.user` מהקשר הבנייה (build context) של Docker (`Backend/.dockerignore:1-4`). |
| `Backend/global.json` | נועל את גרסת ה-.NET SDK ל-`10.0.302` עם `rollForward: "latestFeature"` ו-`allowPrerelease: false` (`Backend/global.json:2-6`). |
| `Backend/.config/dotnet-tools.json` | מניפסט כלי dotnet מקומיים (`dotnet-ef` גרסה `10.0.10`) המותקנים דרך `dotnet tool restore` (`Backend/.config/dotnet-tools.json:1-13`). |
| `Backend/.env` | קובץ סביבה מקומי (אמיתי, לא לדוגמה) — קיים בריפו, תוכנו לא נקרא/פורט (ראו סעיף 5). |
| `Backend/.env.example` | תבנית לקובץ `.env` מקומי, מפרטת שמות משתנים בלבד (ראו סעיף 5). |
| `Backend/Tanakh.sln` | קובץ Solution של Visual Studio/.NET, מקשר בין ארבעת הפרויקטים: `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Api`, `Tanakh.Tests` (`Backend/Tanakh.sln:6-12`). |
| `Backend/README.md` | תיעוד קונפיגורציה (משתני סביבה נדרשים, secrets, seed data) — לא קובץ קונפיגורציה מבצעי בעצמו. |

### `Frontend/`

| קובץ | מטרה |
|---|---|
| `Frontend/angular.json` | קובץ הקונפיגורציה הראשי של Angular CLI — מגדיר builder, נתיב פלט (`dist/tanakh`), i18n (`he` מקור, `en` יעד), קונפיגורציות build ל-`production`/`development` כולל תקציבי גודל bundle ו-`fileReplacements` להחלפת קובץ environment (`Frontend/angular.json:1-141`). |
| `Frontend/package.json` | הגדרת תלויות (Angular 22.1, Material, CDK וכו') וסקריפטי npm: `start`, `build`, `test`, `e2e:a11y`, `lighthouse:a11y`, `lint:css`, `verify` (`Frontend/package.json:4-13`). |
| `Frontend/package-lock.json` | נעילת גרסאות מדויקות של כל עץ התלויות של npm. |
| `Frontend/.editorconfig` | כללי עיצוב עורך (רווחים, קידוד, ניקוי רווחים סופיים) לכל הריפו-משנה, כולל כלל ספציפי ל-`.ts` (`quote_type = single`) ול-`.md` (`Frontend/.editorconfig:1-16`). |
| `Frontend/.stylelintrc.json` | כללי linting ל-CSS/SCSS: איסור `outline: none`, איסור יחידת `px` על `font-size`, איסור `!important` (עם חריגים לקבצי a11y ספציפיים) (`Frontend/.stylelintrc.json:1-25`). |
| `Frontend/ngsw-config.json` | קונפיגורציית Angular Service Worker — קבוצות assets לקאשינג ו-`dataGroups` לנתיבי API עם אסטרטגיות קאשינג שונות (`Frontend/ngsw-config.json:1-56`). |
| `Frontend/playwright.config.ts` | קונפיגורציית Playwright ל-E2E — `testDir: './e2e'`, `baseURL: 'http://localhost:4200'`, מפעיל `ng serve --configuration development` כ-`webServer` (`Frontend/playwright.config.ts:1-22`). |
| `Frontend/lighthouserc.json` | קונפיגורציית Lighthouse CI — מריץ נגד `http://localhost:4200/home` ו-`/settings`, אוכף ציון נגישות מינימלי `0.95` (`Frontend/lighthouserc.json:1-19`). |
| `Frontend/tsconfig.json` | קונפיגורציית TypeScript בסיסית ומשותפת (strict mode, target `ES2022`) (`Frontend/tsconfig.json:1-29`). |
| `Frontend/tsconfig.app.json` | מרחיב את `tsconfig.json` עבור קוד האפליקציה (`src/main.ts` כנקודת כניסה) (`Frontend/tsconfig.app.json:1-24`). |
| `Frontend/tsconfig.spec.json` | מרחיב את `tsconfig.json` עבור קבצי בדיקות (`*.spec.ts`) עם טיפוסי `jasmine` (`Frontend/tsconfig.spec.json:1-23`). |

---

## לא ידוע / דורש אימות

1. **ספק אירוח בפועל (Render/Cloudflare Pages/Neon) — ללא קובץ קונפיגורציה תואם.** מספר מסמכים בריפו מזכירים יעדי אירוח מתוכננים: `Backend/README.md:83-85` מזכיר "Render/Neon/Cloudflare Pages, per the free-tier hosting plan"; `docs/backend-modernization-handoff.md:252` מזכיר "hosting target is a single free-tier Render instance"; `docs/runbooks/restore.md:6-10` מתאר תהליך שחזור מול "Neon console". חיפשתי בכל הריפו קבצי קונפיגורציה ספציפיים ל-Render (`render.yaml`) או ל-Neon — **לא נמצא אף אחד מהם**. הממצא הקונקרטי היחיד התומך בכיוון Cloudflare הוא `Frontend/src/assets/_headers` (ראו סעיף 1.4). לא ניתן לאמת מתוך קבצי קונפיגורציה בפועל שריפו זה מחובר/מוגדר בפועל ל-Render או ל-Neon.
2. **ערך אמיתי של `apiUrl`/דומיין פרודקשן.** כפי שצוין בסעיף 2.1/3.2, `environment.production.ts` מכיל כרגע placeholder זהה לסביבת הפיתוח (`https://localhost:5001`). לא נמצא בשום קובץ קונפיגורציה נוכחי בריפו דומיין פרודקשן אמיתי או ערך API URL סופי.
3. **פער בין הפורט המוזכר ב-`docs/LAUNCH-CHECKLIST.md:9` (`https://localhost:44308`) לבין הפורטים בפועל בקוד** (`environment.ts`/`environment.production.ts` משתמשים ב-`5001`, `ngsw-config.json` משתמש גם הוא ב-`5001`). לא נמצא בקוד עצמו אזכור לפורט `44308` — לא ניתן לקבוע מהו מקור הפער הזה מתוך קבצי הקונפיגורציה הנוכחיים.
4. **תוכן `Backend/.env` (הקובץ האמיתי, לא ה-example).** בהתאם לכללי המשימה נמנעתי מלקרוא/לצטט את תוכנו; לכן לא ניתן לאמת ממנו האם המשתנים בו תואמים 1:1 לרשימת `Backend/.env.example`.
5. **האם קיים `appsettings.Production.json`.** נבדק במפורש ולא נמצא — צוין בגוף המסמך (סעיף 2.2) כממצא ולא כ"לא ידוע", אך מצוין כאן שוב לשקיפות: קונפיגורציית production מסתמכת (לפי `Backend/README.md` בלבד, לא קובץ קונפיגורציה) על משתני סביבה, ולא אומת מול שום מנגנון הזרקת סביבה בפועל (למשל אין evidence לאיך/היכן משתני הסביבה הללו מוזרקים ל-container בפרודקשן בפועל, כי אין קובץ orchestration/hosting-platform בריפו).
