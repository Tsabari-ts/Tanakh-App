# רשימת בדיקה לפני עלייה לאוויר (Pre-Launch Checklist) — Tanakh

> מסמך זה הוא **רשימת בדיקה מעשית**, לא סקירת סגנון. כל שורה מגובה בקובץ ושורה קונקרטיים מתוך הקוד הנוכחי (אומת ישירות ע"י קריאת הקוד, לא רק לפי `docs/audit/*`). `[x]` = אומת בפועל שקיים. `[ ]` = אומת בפועל שחסר, כולל תיאור מה חיפשתי ואיפה. פריטי `[ ]` שמהווים סיכון אמיתי מסומנים ב-ID מסוג `OPS-XX` עם דירוג חומרה/מאמץ, ומרוכזים בטבלה בסוף המסמך. הקשר: פריסה לשרת פרודקשן **יחיד**, ללא load balancer.

| רמה | הגדרה |
|---|---|
| 🔴 קריטי — חוסם לייב | |
| 🟠 גבוה — לתקן לפני לייב | |
| 🟡 בינוני — שבוע ראשון | |
| 🟢 נמוך — בהמשך | |

---

## 1. קונפיגורציית פרודקשן

- [x] **Production היא ברירת המחדל של ה-build** — `"defaultConfiguration": "production"` ב-`Frontend/angular.json:98`, כך ש-`ng build` ללא דגלים מייצר build פרודקשן.
- [x] **Source maps כבויים ב-build פרודקשן** — קונפיגורציית `production` (`Frontend/angular.json:59-91`) אינה מגדירה `sourceMap` כלל; רק קונפיגורציית `development` (`Frontend/angular.json:92-96`) מגדירה `"sourceMap": true` במפורש. ברירת המחדל של ה-builder (`@angular/build:application`) עבור `sourceMap` היא `false` (`Frontend/node_modules/@angular-devkit/build-angular/node_modules/@angular/build/src/builders/application/schema.json`, שדה `sourceMap.default`), כלומר ב-build פרודקשן אין מפות מקור.
- [x] **אין `console.log`/`console.debug`/`console.info`/`console.warn` שיוריים בקוד ה-Frontend** — נבדק בחיפוש גורף על `Frontend/src` (רגקס `console\.(log|debug|info|warn)`): **0 תוצאות**. קיימים רק 3 מופעי `console.error`, כולם טיפול-שגיאות מכוון ולא דיבוג שנשכח: `Frontend/src/main.ts:8` (bootstrap failure), `Frontend/src/app/core/interceptors/error.interceptor.ts:22` (לוג שגיאת HTTP), `Frontend/src/app/core/global-error-handler.ts:10` (מותנה מפורשות ב-`if (!environment.production)`).
- [x] **אין `Console.WriteLine` שיורי בקוד ה-Backend** — חיפוש גורף על `Backend` העלה **מופע יחיד**: `Backend/Tanakh.Api/Program.cs:195`, שהוא הפלט המכוון של כלי-העזר החד-פעמי `--hash-admin-password` (מדפיס גיבוב סיסמה להעתקה ל-`Admin:PasswordHash`) — לא לוג דיבוג שנשכח.
- [x] **`EnableSensitiveDataLogging` מופעל רק ב-Development** — `Backend/Tanakh.Api/Program.cs:47-50` (`if (builder.Environment.IsDevelopment()) { options.EnableSensitiveDataLogging(); }`), כך שבפרודקשן פרמטרים של שאילתות EF Core לא נחשפים בלוג.
- [x] **עמוד שגיאה מפורט / תיעוד API פתוחים רק ב-Development** — `app.UseDeveloperExceptionPage()`, `app.MapOpenApi()`, `app.MapScalarApiReference()` מותנים ב-`app.Environment.IsDevelopment()` (`Backend/Tanakh.Api/Program.cs:223-228`); מחוץ ל-Development רץ במקומם `app.UseExceptionHandler()` + `app.UseHsts()` (`Backend/Tanakh.Api/Program.cs:229-233`), כלומר שגיאות לא חושפות stack trace ללקוח בפרודקשן (מטופל ע"י `GlobalExceptionHandler.cs`).
- [ ] **אין middleware של `ForwardedHeaders`** — חיפוש גורף על `Backend` אחר `ForwardedHeaders`/`X-Forwarded` החזיר 0 תוצאות. ה-`Dockerfile` חושף HTTP רגיל בלבד (`ENV ASPNETCORE_URLS=http://+:8080`, `Backend/Dockerfile:16-17`) — כלומר בפריסה אמיתית משהו (nginx/Caddy וכו') חייב לשבת מול השרת לצורך TLS. בלי `UseForwardedHeaders`, כל בקשה שמגיעה דרך ה-reverse proxy תיראה מגיעה מכתובת ה-IP הפנימית של הפרוקסי בעיני `HttpContext.Connection.RemoteIpAddress` — וזה בדיוק המפתח שלפיו כל שלוש מדיניות ה-rate limiting מתחלקות למחיצות (`RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ...)`, `Backend/Tanakh.Api/Program.cs:120-154`). בפועל זה עלול להפוך את `AdminLogin`/`SubscriptionOtpRequest`/`SubscriptionCreate` למכסה משותפת גלובלית של 5 בקשות בחלון הזמן לכל המשתמשים גם יחד, במקום per-client. ⇐ **OPS-01**.

---

## 2. משתני סביבה

מנגנון הקונפיגורציה: `appsettings.json` ← `appsettings.{Environment}.json` ← משתני סביבה (`Section__Key` ← `Section:Key`). **אין** `appsettings.Production.json` בריפו (נבדק: לא קיים תחת `Backend/Tanakh.Api/`) — כל ערך ייחודי לפרודקשן חייב להגיע ממשתנה סביבה. הרשימה הבאה נגזרה מקריאה ישירה של `Environment.GetEnvironmentVariable`, `IConfiguration`/`AddOptions<T>().Bind(...)` ומחרוזות חיבור בכל `Backend/`, והצלבה מול `Backend/.env.example`.

### חובה — בלעדיהם האפליקציה לא תפעל כראוי בפרודקשן

```
ConnectionStrings__AppDb
ConnectionStrings__MigrationsDb
Hashing__Pepper
Sms__Key
Sms__User
Sms__Pass
Sms__Sender
Sms__DryRun
Admin__Username
Admin__PasswordHash
Admin__Phone
Cors__AllowedOrigins__0
Reminders__PublicBaseUrl
Reminders__ApiBaseUrl
```

הערות מדויקות לכל אחד:
- `ConnectionStrings__AppDb` — היחיד שגורם לחריגה **מיידית** אם חסר (`?? throw new InvalidOperationException(...)`, `Backend/Tanakh.Api/Program.cs:37-38`).
- `ConnectionStrings__MigrationsDb` — לא נקרא ע"י תהליך האפליקציה הרץ עצמו; נדרש רק בזמן הרצת `dotnet ef` (`Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:16`, זורק אם חסר) — כלומר חובה לתהליך הפריסה/מיגרציות, לא ל-`dotnet Tanakh.Api.dll` עצמו.
- `Hashing__Pepper` — לא נבדק ב-startup (אין `ValidateOnStart`/`IValidateOptions` בשום מקום ב-`Backend`, נבדק בחיפוש גורף), אלא נכשל **בזמן ריצה** בפעם הראשונה שנעשה בו שימוש: `Backend/Tanakh.Infrastructure/Services/HashingService.cs:21-24` (זריקת `InvalidOperationException` אם ריק) ובחתימת manage-token-ים ב-`Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:83`. בלעדיו: אין OTP, אין הרשמה, אין ניהול מנוי.
- `Sms:*` (`Key`/`User`/`Pass`/`Sender`) — ברירת מחדל `""` (`Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:11-12,20`), כלומר האפליקציה עולה בלעדיהם אך כל שליחת SMS (תזכורות, OTP הרשמה, OTP מנהל) תיכשל.
- `Sms__DryRun` — ברירת המחדל בקוד היא `true` (`Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:36`) — **חובה** להגדיר במפורש `false` בפרודקשן, אחרת אף SMS אמיתי לא ייצא (`Backend/README.md:75`).
- `Admin:*` (`Username`/`PasswordHash`/`Phone`) — ברירת מחדל `""` (`Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:7,11,14`) — בלעדיהם אין אפשרות להתחבר לפאנל הניהול כלל.
- `Cors__AllowedOrigins__0` (וכל אינדקס נוסף לפי מספר המקורות בפועל) — ברירת מחדל מערך ריק (`Backend/Tanakh.Api/Program.cs:243-244`) — בלעדיו **כל** קריאה מהדפדפן (כולל הפרונט הציבורי, לא רק האדמין) תיחסם ע"י CORS כי אין credentialed origin מורשה (`Backend/Tanakh.Api/Program.cs:245`).
- `Reminders__PublicBaseUrl` / `Reminders__ApiBaseUrl` — ברירת מחדל `""` (`Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:39,42`) — נכנסים לקישורים שנשלחים בפועל בהודעות התזכורת ב-SMS; בלעדיהם קישורי התזכורת יהיו שבורים/ריקים.

### יש להם ברירת מחדל בקוד — מומלץ לסקור ערך, לא חובה טכנית

```
Admin__LowBalanceThreshold
TanakhData__DataDirectory
Retention__ReminderDeliveriesRetentionDays
Retention__UnsubscribedSubscriberRetentionMonths
Retention__RunInterval
Retention__BatchSize
Retention__DelayBetweenBatches
Reminders__PlannerCron
Reminders__DispatchIntervalSeconds
Reminders__MaxLatenessMinutes
Reminders__BatchSize
Reminders__MaxAttempts
Reminders__RetryBackoffMinutes
Reminders__SendRatePerSecond
Reminders__DefaultTimezone
Reminders__DefaultStartBook
Reminders__DefaultStartChapter
Reminders__SmsTemplate
BUILD_VERSION
ASPNETCORE_ENVIRONMENT
```

`BUILD_VERSION` מוצג בפאנל בריאות המנהל אם מוגדר, אחרת `"dev"` (`Backend/Tanakh.Api/Controllers/AdminSystemController.cs:56`). `ASPNETCORE_ENVIRONMENT` לא מוגדר בשום מקום עבור נתיב הפרודקשן (לא ב-`Backend/Dockerfile`, נבדק — אין מופע של `ASPNETCORE_ENVIRONMENT` בקובץ) — ברירת המחדל הסטנדרטית של ASP.NET Core כשהמשתנה חסר היא `Production`, כך שזה בטוח-כברירת-מחדל, אך מומלץ להגדיר במפורש כדי שטעות תצורה עתידית (למשל דריסה בטעות ל-`Development`) לא תפעיל בשוגג `EnableSensitiveDataLogging`/`UseDeveloperExceptionPage`/`--seed`/`--reset-db`.

### לא נדרשים לשרת הפרודקשן עצמו (שימוש local dev בלבד)

```
POSTGRES_USER
POSTGRES_PASSWORD
POSTGRES_DB
POSTGRES_PORT
```

אלה נקראים אך ורק ע"י `Backend/docker-compose.yml:7-11` להרמת Postgres מקומי לפיתוח, ולא ע"י קוד ה-.NET עצמו (`Backend/.env.example:3-8`) — אין להעתיק אותם לשרת הפרודקשן כפי שהם, כי בפרודקשן ה-DB (Neon, לפי `docs/database.md`/`docs/audit/04-database.md`) מנוהל בנפרד.

---

## 3. מיגרציות

- [x] **אין הרצת מיגרציות אוטומטית ב-startup** — חיפוש גורף אחר `Database.Migrate`/`MigrateAsync` בכל `Backend` מעלה שימוש יחיד, בתוך `Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:40-41`, שקרוא **רק** מהדגלים `--seed`/`--reset-db` ב-`Backend/Tanakh.Api/Program.cs:199-221` — וגם אלה חסומים במפורש מחוץ ל-Development (`Backend/Tanakh.Api/Program.cs:203-206`: `throw new InvalidOperationException("--seed and --reset-db are only allowed in the Development environment.")`). כלומר תהליך ה-API עצמו **לעולם לא** מריץ מיגרציות בפרודקשן.
- [x] **הפקודות והסדר המדויקים מתועדים** ב-`docs/database.md:94-98`:
  1. `db/roles/migrations_user.sql` ואז `db/roles/app_user.sql` (חד-פעמי לכל סביבה, `docs/database.md:151-159`).
  2. `dotnet ef database update --project Tanakh.Infrastructure --startup-project Tanakh.Api` (עם `ConnectionStrings__MigrationsDb` מוגדר כמשתנה סביבה, ולא דרך `appsettings`, ראו `Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:16`).
  3. רק לאחר מכן להפעיל את `dotnet Tanakh.Api.dll` (`Backend/Dockerfile:18`) עם `ConnectionStrings__AppDb`.
- [ ] **אין שום שלב אוטומטי (CI/CD) שמריץ את שני השלבים הללו בסדר הנכון לפני עלייה של גרסה חדשה** — נבדק תוכן כל שלושת קבצי ה-workflow הקיימים (`.github/workflows/backend-ci.yml`, `backend-backup.yml`, `frontend-a11y-ci.yml`); אף אחד מהם לא מריץ `dotnet ef database update` או בונה/דוחף image לפריסה. כלומר בכל דיפלוי, מריץ-אנוש חייב לזכור להריץ מיגרציות **לפני** שהגרסה החדשה עולה, בסדר הנכון, ידנית. פער זה חופף ל-**OPS-08** (ראו סעיף "תוכנית rollback").

---

## 4. לוגים

- [x] **הלוגים יוצאים לקונסול (stdout) בלבד, דרך ה-logging הסטנדרטי של ASP.NET Core** — `Backend/Tanakh.Api/appsettings.json:2-7` מגדיר רק `Logging:LogLevel` (`Default=Information`, `Microsoft=Warning`, `Microsoft.Hosting.Lifetime=Information`); נבדק גם `Backend/Tanakh.Api/Tanakh.Api.csproj` וכל שאר קבצי ה-`.csproj` (`docs/audit/03-backend.md` סעיף 2) — **אין** הפניה ל-Serilog/NLog/Application Insights/כל sink חיצוני. `Backend/Dockerfile` (18 שורות) גם הוא לא מגדיר הפניית stdout לקובץ.
- [ ] **אין שום rotation/size cap על הלוגים** — לא נמצא אף קובץ קונפיגורציה (`Backend/appsettings*.json`, `Backend/Dockerfile`) שמגביל גודל/מספר קבצי לוג. מכיוון שהלוג יוצא ל-stdout, האחריות עוברת לחלוטין לאופן ההרצה בפרודקשן (למשל driver ברירת המחדל של Docker, `json-file`, שאין לו הגבלת גודל כברירת מחדל ללא `--log-opt max-size`) — **אין בריפו שום קובץ (`docker-compose.yml`/`Dockerfile`/CI) שמגדיר את זה**. על שרת יחיד ללא load balancer, לוג שלא מתגלגל יכול למלא את הדיסק ולהפיל את כל המכונה (כולל את ה-DB אם הוא משותף לאותו דיסק). ⇐ **OPS-09**.
- [x] **שגיאות בלתי-מטופלות נשמרות גם ב-DB** (לא רק בקונסול) — `GlobalExceptionHandler` כותב שורת `ErrorLog` (`Level=Error`, `Message`, `StackTrace`, `Endpoint`, `StatusCode`) ל-`AppDbContext`, עם בליעת כשל כתיבה כדי לא לחסום את תגובת השגיאה עצמה — `Backend/Tanakh.Api/GlobalExceptionHandler.cs:54-74`. יומן זה ניתן לצפייה/ניקוי/ייצוא דרך `AdminLogsController`/`AdminExportController` (פאנל ניהול בלבד, לא push).

---

## 5. ניטור

- [ ] **אין שום כלי ניטור/alerting חיצוני מחובר בקוד** — חיפוש גורף על כל הריפו (למעט `node_modules`) אחר `Sentry`/`ApplicationInsights`/`Datadog`/`healthchecks.io`/`UptimeRobot`/`PagerDuty` לא העלה אף שימוש בפועל בקוד; טבלת התלויות המלאה של כל 4 פרויקטי ה-.NET (`docs/audit/03-backend.md` סעיף 2) ו-`Frontend/package.json` אינן כוללות שום חבילת ניטור/error-tracking.
- [ ] **התשתית ל-error monitoring בפרונט קיימת אך לא מחוברת בכוונה** — `Frontend/src/app/core/global-error-handler.ts:23-28`, מתודת `report()`, גוף ריק עם הערה מפורשת: `// TODO(LAUNCH): wire to an error monitoring service (Sentry / App Insights / none). Deliberately unwired — requires an account and a hosted environment.` המאושרר גם ברשימה `docs/LAUNCH-CHECKLIST.md:11` (פריט L-04, סטטוס "Open").
- [x] **קיים health-check פנימי לבדיקת בריאות התהליך, אך הוא pull-based ונגיש רק לאדמין המחובר** — `GET /api/v1/admin/system/health` (`Backend/Tanakh.Api/Controllers/AdminSystemController.cs:32-61`) מחזיר `uptimeSeconds`, `databaseConnected` (`dbContext.Database.CanConnectAsync`, שורה 36), `diskFreeBytes`, `buildVersion` — אך דורש התחברות ידנית לפאנל, אין ping אוטומטי חיצוני שקורא לו.
- [x] **קיימים גם endpoint-י liveness/readiness סטנדרטיים** — `GET /health/live` (ללא בדיקת תלויות, `Predicate = _ => false`, `Backend/Tanakh.Api/Program.cs:295-298`) ו-`GET /health/ready` (בודק רק קיום קבצי `TanakhData.json`/`TanakhStructure.json` על הדיסק, לא את ה-DB — `Backend/Tanakh.Api/Program.cs:306-309`, `Backend/Tanakh.Infrastructure/HealthChecks/TanakhDataHealthCheck.cs:7,20-22`) — אלה קיימים בקוד אך שום דבר בריפו לא מבצע עליהם polling אוטומטי/מתריע אם הם נופלים.
- ⇐ סיכום: **אין שום מנגנון ניטור/alerting חי בפרודקשן** — לא uptime monitor חיצוני שפוֹלינג את `/health/live`, לא Sentry/App Insights לשגיאות front/back, לא התראה אוטומטית על יתרת SMS נמוכה (יש רק סף מוגדר, `Admin:LowBalanceThreshold`, ללא push — `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:19`). ⇐ **OPS-04**.

---

## 6. גיבויים

- [x] **מנגנון גיבוי מוגדר בקוד/CI** — `.github/workflows/backend-backup.yml` רץ בקרון יומי (`"0 3 * * *"`, UTC) וגם ידנית (`workflow_dispatch`), `.github/workflows/backend-backup.yml:14-16`; מריץ `bash Backend/db/dumps/pg_dump.sh` עם `DATABASE_URL=${{ secrets.DIRECT_DATABASE_URL }}` (`.github/workflows/backend-backup.yml:24-30`) ומעלה את קובץ ה-dump כ-artifact עם שמירה ל-90 יום (`.github/workflows/backend-backup.yml:35-40`).
- [ ] **ה-secret שהמנגנון תלוי בו לא מוגדר** — ראש הקובץ עצמו מתעד זאת במפורש: `.github/workflows/backend-backup.yml:8-12` — *"Requires a repo secret `DIRECT_DATABASE_URL` ... not yet configured, since no Neon project exists for this app yet. Until it is, this workflow will fail at the 'Take backup' step"*. כלומר **אין כרגע אף גיבוי אוטומטי אמיתי שרץ** — ה-workflow קיים אבל נכשל בכל הרצה. ⇐ **OPS-05**.
- [x] **קיים גם runbook לשחזור** — `docs/runbooks/restore.md`, עם שני מסלולים: Neon PITR (עמודים 6-20) ו-`pg_dump`/`pg_restore` לעותק בלתי-תלוי-ספק (עמודים 22-39).
- [ ] **ה-runbook לא תואם את הסכימה הנוכחית** — `docs/runbooks/restore.md:45-47` מפרט רשימת "8 טבלאות" לאימות שחזור: `subscribers`, `reading_progress`, `reminder_deliveries`, `email_events`, `suppression_list`, `consent_records`, `audit_log`, `__EFMigrationsHistory`. בבדיקת הסכימה בפועל (`Backend/Tanakh.Infrastructure/Data/AppDbContext.cs`, מיפוי מלא ב-`docs/audit/04-database.md` סעיף 3 ובקבצי `Backend/Tanakh.Domain/Entities/*.cs`) **אין** טבלאות `email_events`/`suppression_list` — הן שרידים מהארכיטקטורה הישנה מבוססת-אימייל (הוסרה, ראו `Backend/README.md:7`: "email is no longer used for anything"); ולעומת זאת ה-runbook **לא מזכיר** חמש טבלאות שקיימות בפועל: `otp_codes`, `subscriber_otp_codes`, `sms_log`, `app_settings`, `feature_flags` (רשימת ה-`DbSet` המלאה, `Backend/Tanakh.Infrastructure/Data/AppDbContext.cs:20-40`, 11 טבלאות בסך הכל). מי שיריץ את צ'ק-ליסט האימות של ה-runbook כלשונו יפספס לגמרי חלק מהטבלאות האמיתיות ויחפש טבלאות שלא קיימות.
- [ ] **התרגול (drill) שבוצע היה מקומי בלבד, לא מול Neon אמיתי** — `docs/runbooks/restore.md:56-68`, טבלת "Drill log" מכילה שורה יחידה מ-2026-07-30 כנגד `docker-compose` מקומי, עם הצהרה מפורשת: *"This drill exercised the pg_dump/pg_restore mechanism only, against local docker-compose — it has not yet been run against a real Neon project ... Before launch, repeat this drill at least once against the real staging Neon project"*. כלומר גם מסלול ה-PITR (Path 1) וגם ה-pg_dump (Path 2) לא נבדקו אף פעם מול הפרודקשן/staging האמיתיים. ⇐ נכלל תחת **OPS-06** (יחד עם אי-ההתאמה בסכימה למעלה).

---

## 7. דומיין, HTTPS ותעודה

- [x] **עוגיית האדמין דורשת HTTPS באכיפה** — `options.Cookie.SecurePolicy = CookieSecurePolicy.Always` (`Backend/Tanakh.Api/Program.cs:89`) — כלומר בלי HTTPS אמיתי, כניסת מנהל לא תעבוד כלל (העוגייה לא תישלח ע"י הדפדפן).
- [x] **HSTS ו-HTTPS redirection מופעלים מחוץ ל-Development** — `app.UseHsts()` (`Backend/Tanakh.Api/Program.cs:232`), `app.UseHttpsRedirection()` (`Backend/Tanakh.Api/Program.cs:235`, לא מותנה בסביבה כלל).
- [x] **CORS דורש רשימת origins מפורשת עם credentials** — `app.UseCors(x => x.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials())` (`Backend/Tanakh.Api/Program.cs:245`) — אין `AllowAnyOrigin()`, כך שכל origin שלא ברשימת `Cors:AllowedOrigins` נחסם.
- [ ] **אין עדיין דומיין פרודקשן אמיתי בשום קובץ בריפו** — `Frontend/src/environments/environment.production.ts:13`: `apiUrl: 'https://localhost:5001'`, עם הערת `TODO(LAUNCH)` בראש הקובץ (שורות 1-10) שמצהירה במפורש שזה placeholder עד שיבחר דומיין; אותו ערך בדיוק (`https://localhost:5001`) גם ב-`Frontend/src/environments/environment.ts:3` (סביבת פיתוח) — **אין הבדל בין הסביבות** כרגע. אותו localhost hardcoded מופיע גם ב-`Frontend/ngsw-config.json:34,46` (ה-URLs שה-Service Worker שומר בקאש). ⇐ חלק מ-**OPS-07**.
- [ ] **נתיב פאנל האדמין הוא placeholder שחייב להתחלף יחד עם הדומיין** — `adminRoutePath: 'admin-x9k2'` מופיע זהה בפיתוח (`Frontend/src/environments/environment.ts:9`) ובפרודקשן (`Frontend/src/environments/environment.production.ts:16`), עם אזהרה מפורשת ב-`Frontend/src/environments/environment.production.ts:6-10`: *"MUST be overwritten with the real secret path ... Never ship this literal to production."* בנוסף, `Frontend/src/assets/_headers:5-9` מגדיר את כותרת `X-Robots-Tag: noindex, nofollow` על הנתיב `/admin-x9k2/*` בלבד — אם הנתיב האמיתי בפרודקשן לא יעודכן **בשני המקומות בו-זמנית** (וגם מול `ADMIN_ROUTE_PATH`-שקול בצד ה-backend, אם ייקבע כזה), פאנל הניהול האמיתי לא יקבל את הגנת ה-`noindex`, וגם המקור ה"סודי" הנוכחי (`admin-x9k2`) כבר חשוף בקוד המקור הפומבי בגיטהאב. ⇐ חלק מ-**OPS-07**.
- [ ] **אין קובץ קונפיגורציית hosting/TLS בפועל בריפו** — כפי שמתועד ומאומת ב-`docs/audit/07-infra-and-deploy.md` סעיף 1.3, חיפוש גורף אחר `render.yaml`/`netlify.toml`/`vercel.json`/`fly.toml`/קבצי nginx לא העלה דבר מלבד `Frontend/src/assets/_headers` (ספציפי ל-Cloudflare Pages) ו-`Backend/Dockerfile`/`Backend/docker-compose.yml`. כלומר אין שום עדות בקוד לאיך/היכן בפועל תעודת TLS מונפקת/מחודשת בפרודקשן.

---

## 8. SEO ומטא

- [x] **`lang="he"` ו-`dir="rtl"` מוגדרים בתגית `<html>`** — `Frontend/src/index.html:2`: `<html dir="rtl" lang="he">`.
- [x] **תגיות מטא בסיסיות ו-Open Graph קיימות** — `Frontend/src/index.html:16-23`: `og:type`, `og:title`, `og:description`, `og:image`, וכן `twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`. גם `theme-color` (`index.html:5`) ו-viewport תקין ל-mobile (`index.html:8`).
- [x] **כותרת (`<title>`) בעברית קיימת** — `Frontend/src/index.html:6`: `<title>פרק יומי — קריאת תנ״ך</title>`.
- [x] **manifest.webmanifest ואייקוני PWA/iOS קיימים** — `Frontend/src/index.html:9-14`.
- [ ] **אין `robots.txt`** — נבדק `Frontend/src/assets/` (רשימת קבצים מלאה: `IsraelFlag.jpg`, `_headers`, `arrowDown.png` ... `tora1.png`) ונבדק גם חיפוש גורף אחר `robots.txt` בכל `Frontend/src` — **לא נמצא**. הקובץ היחיד שנוגע ל-robots הוא כותרת HTTP חלקית (`X-Robots-Tag: noindex, nofollow` על `/admin-x9k2/*` בלבד, `Frontend/src/assets/_headers:8-9`), לא קובץ `robots.txt` בשורש האתר. ⇐ **OPS-03**.
- [ ] **אין `sitemap.xml`** — אותו חיפוש; לא נמצא קובץ כזה בשום מקום תחת `Frontend/src`. ⇐ חלק מ-**OPS-03**.
- [ ] **אין prerendering בפועל** — `docs/audit/07-infra-and-deploy.md` סעיף 1.5 מאשר ש-`Frontend/dist/tanakh/prerendered-routes.json` מכיל `{"routes": {}}` ריק — כלומר האתר יוצא כ-SPA טהור בצד לקוח בלבד (ללא SSR/SSG), בהתאם להחלטת ה-ADR ב-`docs/adr/003-frontend-ssr-decision.md:26` (נדחה עד שיהיה דומיין פרודקשן). זה לא נמנה כ-OPS נפרד (החלטה מתועדת ומכוונת), אך משפיע על יעילות ה-SEO בפועל בהיעדר sitemap/prerender.

---

## 9. דף 404 ודף שגיאה כללי

- [x] **קיים component ל"מסך שגיאה כללי", מחובר ל-root component** — `Frontend/src/app/shared/error-screen/error-screen.component.ts` (עם `error-screen.component.html`/`.css`), מוזרק ומוצג תמיד ב-`Frontend/src/app/app.component.html:56` (`<app-error-screen></app-error-screen>`) ומיובא ב-`Frontend/src/app/app.component.ts:10,28`. מופעל דרך `ErrorStateService.showFatal()`, הנקרא מ-`Frontend/src/app/core/global-error-handler.ts:19` בכל שגיאת runtime בלתי-מטופלת.
- [ ] **נתיב ה-wildcard ל-404 שגוי ולא באמת תופס כלום** — `Frontend/src/app/app.routes.ts:43`: `{ path: "*", redirectTo: "home" }`. ב-Angular Router נתיב "תפוס-כל" (wildcard) **חייב** להיות `"**"`, לא `"*"` — `"*"` הוא ניסיון להתאים סגמנט-URL בודד שהתוכן המילולי שלו הוא התו `*`, לא "כל נתיב שלא הותאם". נבדק שאין `router.events`/`NavigationError` handler נוסף בשום מקום ב-`Frontend/src/app` (חיפוש גורף) שהיה יכול לתפוס את הכישלון חלופית. בפועל: משתמש שמגיע ל-URL לא קיים (קישור ישן, טעות הקלדה) לא מקבל הפניה ל-`home` ולא מקבל 404 — הניווט פשוט נכשל בשקט בלי שום מסך fallback. ⇐ **OPS-02**.

---

## 10. תוכנית Rollback

- [ ] **אין שום workflow של deploy/CD בריפו** — שלושת קבצי ה-workflow הקיימים תחת `.github/workflows/` הם `backend-ci.yml` (build+test בלבד, על push/PR ל-`Backend/**`), `frontend-a11y-ci.yml` (build+lint+a11y e2e+Lighthouse בלבד, על push/PR ל-`Frontend/**`), ו-`backend-backup.yml` (גיבוי יומי). נבדק חיפוש גורף בכל הריפו אחר קבצים שכוללים "deploy" בשם — התוצאה היחידה היא `docs/audit/07-infra-and-deploy.md` עצמו (מסמך תיעוד, לא workflow). כלומר: **אין בריפו שום מנגנון שמעלה גרסה חדשה לפרודקשן**, לא כל שכן מנגנון rollback.
- [ ] **אין אסטרטגיית תיוג/גרסאות ל-Docker image** — `Backend/Dockerfile` לא מכיל `LABEL`/ARG גרסה, ואין workflow שבונה ודוחף image עם תג (נבדק בתוך שלושת קבצי ה-workflow הקיימים — אף אחד לא מריץ `docker build`/`docker push`). אין דרך מתועדת "לחזור ל-image הקודם" כי אין תיוג image מלכתחילה.
- [ ] **אין פקודת rollback למיגרציות מתועדת** — `docs/database.md`/`Backend/README.md` מתעדים `dotnet ef database update` (קדימה) אך לא `dotnet ef database update <MigrationName-הקודם>` (אחורה) כצעד rollback מוגדר; `DatabaseSeeder.ResetSchemaAsync` (`Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:40-41`, מוריד עד `"0"` ואז מעלה בחזרה) הוא כלי איפוס-לפיתוח בלבד, חסום מחוץ ל-Development (`Backend/Tanakh.Api/Program.cs:203-206`) — לא כלי rollback לפרודקשן.
- ⇐ **מסקנה מפורשת**: **אין כרגע שום תוכנית/מנגנון rollback מוגדר בריפו** — לא Docker image tags, לא git-revert-then-redeploy מתועד (כי אין deploy workflow לחזור אליו מלכתחילה), ולא פקודת מיגרציה-אחורה. אם משהו יישבר מיד אחרי לייב, אין היום שום "כפתור" מתועד ללחוץ עליו — כל תגובה תהיה אד-הוק. ⇐ **OPS-08**.

---

## טבלת פריטי OPS (גאפים)

| ID | תיאור | חומרה | מאמץ | קובץ/הערה |
|---|---|---|---|---|
| OPS-01 | אין `ForwardedHeaders` middleware — מאחורי reverse proxy, rate limiting ולוגים לפי IP יראו את כתובת הפרוקסי במקום את הלקוח האמיתי | 🟠 גבוה | S | `Backend/Tanakh.Api/Program.cs:120-154` (partition לפי `RemoteIpAddress`); אין `UseForwardedHeaders` בכל `Backend` |
| OPS-02 | נתיב ה-wildcard בראוטינג הפרונט שגוי (`"*"` במקום `"**"`) — 404 לא באמת נתפס, ניווט לנתיב לא קיים נכשל בשקט | 🟠 גבוה | S | `Frontend/src/app/app.routes.ts:43` |
| OPS-03 | אין `robots.txt` ואין `sitemap.xml` בשום מקום ב-Frontend | 🟡 בינוני | S | נבדק `Frontend/src/assets/` וחיפוש גורף ב-`Frontend/src` — לא נמצאו |
| OPS-04 | אין שום ניטור/alerting בפרודקשן (Sentry/App Insights/uptime pinger) — התשתית בפרונט קיימת אך `report()` ריק בכוונה | 🟠 גבוה | M | `Frontend/src/app/core/global-error-handler.ts:23-28`; `docs/LAUNCH-CHECKLIST.md:11` (L-04, Open) |
| OPS-05 | workflow הגיבוי היומי תלוי ב-secret `DIRECT_DATABASE_URL` שלא הוגדר — כרגע **אין גיבוי אמיתי שרץ בפועל** | 🔴 קריטי | M | `.github/workflows/backend-backup.yml:8-12,24-30` |
| OPS-06 | ה-runbook לשחזור (`docs/runbooks/restore.md`) לא תואם את הסכימה הנוכחית (טבלאות לא קיימות/חסרות) והתרגול בוצע מקומית בלבד, מעולם לא מול Neon אמיתי | 🟠 גבוה | S/M | `docs/runbooks/restore.md:45-47,56-68` מול `Backend/Tanakh.Infrastructure/Data/AppDbContext.cs:20-40` |
| OPS-07 | אין דומיין פרודקשן/hosting מסופק בפועל — `apiUrl`, `Cors:AllowedOrigins`, `ngsw-config.json`, ונתיב האדמין (`adminRoutePath`/`_headers`) כולם עדיין placeholder-ים זהים ל-dev | 🔴 קריטי | M | `Frontend/src/environments/environment.production.ts:1-17`; `Frontend/ngsw-config.json:34,46`; `Frontend/src/assets/_headers:5-9` |
| OPS-08 | אין שום workflow של deploy/CD, אין תיוג גרסאות ל-Docker image, ואין תוכנית/פקודת rollback מתועדת בשום מקום בריפו | 🔴 קריטי | L | נבדק כל `.github/workflows/*` (3 קבצים בלבד, אף אחד לא deploy) |
| OPS-09 | אין rotation/size cap על הלוגים (יציאה ל-stdout בלבד, ללא Serilog/sink חיצוני) — סיכון למילוי דיסק על שרת יחיד | 🟠 גבוה | M | `Backend/Tanakh.Api/appsettings.json:2-7`; אין sink חיצוני בכל טבלת התלויות (`docs/audit/03-backend.md` סעיף 2) |
