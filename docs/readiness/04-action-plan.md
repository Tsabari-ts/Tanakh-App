# 04 — תוכנית טיפול (Action Plan)

> מסמך זה מאחד את כל 28 הממצאים הייחודיים מ-`01-security.md` (SEC-XX), `02-load-and-scale.md` (LOAD-XX) ו-`03-pre-launch-checklist.md` (OPS-XX). שלושה זוגות ממצאים תוארו פעמיים בשני מסמכים שונים (אותה תקלה, נמצאה משני זוויות ניתוח) — אוחדו כאן לשורה אחת עם שני המזהים המקוריים. מספור חדש (`RB-XX` לחוסמים, `R-XX` לשאר) לצורך הפניה נוחה בתוך מסמך זה בלבד — לפרטים המלאים (ציטוטי קוד, הסבר מלא) חזרו למסמך המקור לפי ה-ID המקורי.

---

## א. טבלת מאסטר

| # | ממצא | קטגוריה | חומרה | מאמץ | קובץ:שורה | תלוי ב-# |
|---|---|---|---|---|---|---|
| RB-01 | אין גיבוי DB עובד — workflow תלוי ב-secret `DIRECT_DATABASE_URL` שלא הוגדר, ואין פרויקט Neon (SEC-01/OPS-05) | אבטחה+תפעול | 🔴 | M | `.github/workflows/backend-backup.yml:8-12,26` | RB-02 |
| RB-02 | אין דומיין/hosting מסופק בפועל — `apiUrl`, CORS, ngsw-config, נתיב אדמין כולם placeholder זהים ל-dev (OPS-07) | תפעול | 🔴 | M | `Frontend/src/environments/environment.production.ts:1-17` | — |
| RB-03 | אין ראיה למנגנון restart/process supervisor אחרי קריסה על שרת יחיד (LOAD-01) | עומס/חוסן | 🔴 | S–M | `Backend/Dockerfile` (חסר); אין קונפיג orchestrator | RB-02 |
| RB-04 | אין workflow deploy/CD, אין תיוג Docker image, אין תוכנית/פקודת rollback (OPS-08) | תפעול | 🔴 | L | `.github/workflows/*` (3 קבצים, אף אחד לא deploy) | RB-02 |
| R-05 | `verify-otp` אדמין ללא rate limiting וללא קישור ל-session — DoS זול נגד המנהל היחיד (SEC-04) | אבטחה | 🟠 | S | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:98-128` | — |
| R-06 | אין `UseForwardedHeaders` — מאחורי proxy, rate limiting/IP-hash נשברים (SEC-02/OPS-01) | אבטחה | 🟠 | S | `Backend/Tanakh.Api/Program.cs:120-154` | RB-02 (לדעת טופולוגיית proxy) |
| R-07 | `JewishCalendarService` יוצר `new HttpClient()` לכל בקשה, ללא cache/timeout — סיכון socket exhaustion (SEC-03/LOAD-03) | אבטחה+עומס | 🟠 | S–M | `Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:53-69` | — |
| R-08 | `TanakhTextService` בונה מחדש את כל מילון התנ"ך (23,206 פסוקים) בכל קריאת פרק בודדת | עומס | 🟠 | M | `Backend/Tanakh.Api/Services/TanakhTextService.cs:41-88` | — |
| R-09 | אין `Cache-Control`/`ETag`/דחיסה על ה-API של תוכן התנ"ך — ההזדמנות הגדולה ביותר להקלה על השרת | עומס | 🟠 | S | `Backend/Tanakh.Api/Controllers/TanakhController.cs` | — |
| R-10 | `/health/ready` לא בודק חיבור DB — "בריא" מדומה גם כשה-DB למטה | עומס/תפעול | 🟠 | S | `Backend/Tanakh.Api/Program.cs:167-168,306-309` | — |
| R-11 | ניתוב wildcard שגוי (`"*"` במקום `"**"`) — 404 לא נתפס, ניווט נכשל בשקט (OPS-02) | תפעול | 🟠 | S | `Frontend/src/app/app.routes.ts:43` | — |
| R-12 | אין שום ניטור/alerting בפרודקשן — התשתית בפרונט קיימת אך לא מחוברת בכוונה (OPS-04) | תפעול | 🟠 | M | `Frontend/src/app/core/global-error-handler.ts:23-28` | RB-02 |
| R-13 | ה-runbook לשחזור לא תואם את הסכימה הנוכחית, ומעולם לא תורגל מול Neon אמיתי (OPS-06) | תפעול | 🟠 | S/M | `docs/runbooks/restore.md:45-47,56-68` | RB-01 |
| R-14 | אין rotation/size cap על הלוגים — סיכון מילוי דיסק על שרת יחיד (OPS-09) | תפעול | 🟠 | M | `Backend/Tanakh.Api/appsettings.json:2-7` | RB-02 |
| R-15 | חסרות כותרות אבטחה: CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy | אבטחה | 🟡 | S | `Backend/Tanakh.Api/Program.cs:249-256` | — |
| R-16 | manage token של מנוי ללא תפוגה בפועל, מועבר כ-query string ב-GET | אבטחה | 🟡 | S | `Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:33-71` | — |
| R-17 | קודי OTP נשמרים בטקסט גלוי בתוך `sms_log.message` | אבטחה | 🟡 | S | `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:104-114` | — |
| R-18 | לוגאאוט אדמין הוא client-side בלבד — אין server-side ticket invalidation | אבטחה | 🟡 | M | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:153-160` | — |
| R-19 | סיסמת DB מקומית מוטבעת בטקסט גלוי ב-`appsettings.Development.json` (DB מקומי בלבד) | אבטחה | 🟡 | S | `Backend/Tanakh.Api/appsettings.Development.json:10` | — |
| R-20 | גודל connection pool ל-DB לא מוגדר/לא ידוע מהקוד | עומס | 🟡 | S | `Backend/Tanakh.Api/Program.cs:35-51` | RB-02 (מחרוזת חיבור אמיתית) |
| R-21 | N+1 ב-`ReminderDispatcherService` — שאילתה+שמירה נפרדת לכל שורה בבאץ' | עומס | 🟡 | M | `Backend/Tanakh.Infrastructure/Reminders/ReminderDispatcherService.cs:89-107` | — |
| R-22 | retry interceptor בפרונט מכפיל עומס פי 3 בדיוק כשהשרת מחזיר 5xx | עומס | 🟡 | S–M | `Frontend/src/app/core/interceptors/retry.interceptor.ts:4-17` | — |
| R-23 | עמוד הכניסה טוען ~592KB תמונות לא ממוטבות + favicon של 570KB כ-og:image | עומס | 🟡 | S | `Frontend/src/app/components/entrance/entrance.component.scss:14,68,183` | — |
| R-24 | אין `robots.txt`/`sitemap.xml` | תפעול | 🟡 | S | `Frontend/src/assets/` (לא נמצאו) | — |
| R-25 | אין הגבלת אורך שרתית על שדות טקסט חופשי בהרשמה | אבטחה | 🟢 | S | `Backend/Tanakh.Api/Model/SubscriptionRequest.cs:10,29-33` | — |
| R-26 | 27 חולשות `npm audit`, כולן ב-devDependencies בלבד (0 ב-production) | אבטחה | 🟢 | S | `Frontend/package.json` (devDependencies) | — |
| R-27 | `ReminderPlannerService` מכניס שורות ברצף אחת-אחת (תקין בקנה מידה נוכחי) | עומס | 🟢 | M | `Backend/Tanakh.Infrastructure/Reminders/ReminderPlannerService.cs:80-111` | — |
| R-28 | אין override מפורש ל-graceful shutdown timeout | עומס | 🟢 | S | לא נמצא ב-`Backend` (ברירת מחדל ASP.NET Core) | RB-02 |

---

## ב. שלושה גלים

### גל 1 — חוסמי לייב (🔴, 4 ממצאים)

**RB-02 — אין דומיין/hosting מסופק בפועל**
- הבעיה: `apiUrl`, CORS, ה-Service Worker, ונתיב האדמין כולם עדיין ערכי localhost/dev.
- התיקון: להחליט בפועל על פלטפורמת אירוח (Render/Neon/Cloudflare Pages, לפי `Backend/README.md:83-85`, או חלופה), להקים את החשבונות/הפרויקטים, ואז לעדכן: `Frontend/src/environments/environment.production.ts` (apiUrl + adminRoutePath אמיתי), `Cors:AllowedOrigins` בפרודקשן, `Frontend/ngsw-config.json` (dataGroup URLs), ו-`Frontend/src/assets/_headers` (להרחיב את כלל ה-`noindex` לנתיב האדמין האמיתי).
- איך מוודאים שנפתר: פתיחת האתר מהדומיין האמיתי עובדת קצה-לקצה (כולל התחברות אדמין ב-HTTPS אמיתי).
- הערכת זמן: M (הנדסית) + זמן המתנה חיצוני (רישום דומיין/DNS/חשבונות) שלא תלוי במאמץ הנדסי בלבד.

**RB-01 — אין גיבוי DB עובד (תלוי ב-RB-02)**
- הבעיה: workflow הגיבוי היומי כושל בכל ריצה כי `DIRECT_DATABASE_URL` לא הוגדר, ואין עדיין פרויקט Neon.
- התיקון: לאחר הקמת ה-DB המנוהל (חלק מ-RB-02), להגדיר את ה-secret `DIRECT_DATABASE_URL` ב-GitHub Actions, ולוודא ש-`.github/workflows/backend-backup.yml` רץ בהצלחה פעם אחת ידנית (`workflow_dispatch`) לפני ההשקה.
- איך מוודאים שנפתר: הרצת workflow ידנית מסתיימת ב-✅, וקובץ ה-dump אכן מופיע כ-artifact.
- הערכת זמן: M.

**RB-03 — אין ראיה למנגנון restart אחרי קריסה (תלוי ב-RB-02)**
- הבעיה: אין `HEALTHCHECK` ב-Dockerfile, ואין קונפיג orchestrator בריפו.
- התיקון: תלוי בפלטפורמה שתיבחר ב-RB-02 — אם Render/Fly: לוודא שה-restart policy המובנה של הפלטפורמה מופעל (בד"כ ברירת מחדל). אם VPS גולמי: להגדיר `systemd` unit עם `Restart=always`, או להריץ ב-Docker עם `--restart=unless-stopped`.
- איך מוודאים שנפתר: להרוג ידנית את התהליך (`kill`/`docker stop` בסביבת staging) ולוודא שהוא עולה מחדש תוך שניות ספורות ללא התערבות אנושית.
- הערכת זמן: S (אם הפלטפורמה תומכת מובנה) עד M (אם דורש הגדרת systemd ידנית).

**RB-04 — אין deploy/CD ואין תוכנית rollback (תלוי ב-RB-02)**
- הבעיה: 3 workflows קיימים (CI, a11y-CI, backup) — אף אחד לא בונה/דוחף/פורס גרסה חדשה. אין תיוג Docker image, אין פקודת rollback מתועדת.
- התיקון (בתוך מסמך זה בלבד — קטע קוד מוצע, לא נערך בקוד עצמו):
```yaml
# .github/workflows/backend-deploy.yml (הצעה — לא נוצר בפועל)
# on: push to main (אחרי שה-CI הקיים עבר)
# steps: docker build -t backend:${{ github.sha }} .
#        docker push <registry>/backend:${{ github.sha }}
#        dotnet ef database update  (עם ConnectionStrings__MigrationsDb)
#        <deploy trigger של הפלטפורמה שנבחרה ב-RB-02>
```
  לצד זה: לתעד ב-`docs/runbooks/` צעד-אחר-צעד "מה עושים אם גרסה חדשה שוברת דברים" — כולל פקודת חזרה ל-image הקודם לפי tag, ופקודת `dotnet ef database update <MigrationName-הקודם>` אם המיגרציה האחרונה צריכה נסיגה.
- איך מוודאים שנפתר: דיפלוי מבוקר לסביבת staging, ואז הרצה מכוונת של "rollback" לגרסה הקודמת לפי הראנבוק החדש, בהצלחה.
- הערכת זמן: L.

### גל 2 — לפני העלייה (🟠, 10 ממצאים)

כל הפריטים R-05 עד R-14 מהטבלה למעלה. אלה שלא תלויים ב-RB-02 (R-05, R-07, R-08, R-09, R-10, R-11) ניתן לבצע **כבר עכשיו, במקביל** להחלטת האירוח:

- **R-11** (S): לתקן `Frontend/src/app/app.routes.ts:43` מ-`{ path: "*", redirectTo: "home" }` ל-`{ path: "**", redirectTo: "home" }`.
- **R-05** (S): להוסיף `[EnableRateLimiting("AdminVerifyOtp")]` על `verify-otp` (`AdminAuthController.cs:98`), מדיניות חדשה בדומה ל-`AdminLogin` הקיימת.
- **R-10** (S): להוסיף `.AddDbContextCheck<AppDbContext>()` עם תגית `"ready"` ב-`Program.cs:167-168`.
- **R-09** (S): להוסיף `[ResponseCache]`/כותרות `Cache-Control`/`ETag` ידניות ב-`TanakhController`, ולהפעיל `AddResponseCompression` ב-`Program.cs`.
- **R-07** (S–M): לרשום `JewishCalendarService` כ-typed `HttpClient` דרך `AddHttpClient<...>()` עם timeout קצר, ולהוסיף קאש יומי.
- **R-08** (M): לשמור בקאש את תוצאת `BuildChapterDictionaryAsync` המוגמרת (לא רק את ה-`TanakhContainer` הגולמי) ב-`ITanakhCache`.

התלויים ב-RB-02: **R-06** (ForwardedHeaders — דורש לדעת טופולוגיית proxy בפועל), **R-12** (ניטור — דורש סביבה מאוחסנת + חשבון Sentry/App Insights), **R-14** (log rotation — תלוי במנגנון ההרצה בפלטפורמה שתיבחר). **R-13** תלוי ב-RB-01 (צריך DB מנוהל אמיתי כדי לתרגל שחזור מולו).

### גל 3 — אחרי הלייב (🟡🟢, 14 ממצאים, בקצרה)

- **R-15**: להוסיף middleware פשוט שמצרף 4 כותרות אבטחה לכל תגובה.
- **R-16**: להוסיף בדיקת TTL על ה-timestamp שכבר קיים בפיילוד של manage token.
- **R-17**: להחליף קוד OTP בפלייסהולדר לפני כתיבה ל-`sms_log`.
- **R-18**: להוסיף ticket store מבוסס-DB או endpoint "revoke all sessions".
- **R-19**: להעביר סיסמת DB מקומית ל-`dotnet user-secrets`.
- **R-20**: להגדיר `Maximum Pool Size` מפורש בקונפיגורציית החיבור בפרודקשן, לפי מדידת loadtest.
- **R-21**: לאסוף מנויים ב-`IN` query מרוכז ו-`SaveChangesAsync` יחיד בסוף לולאת הדיספצ'ר.
- **R-22**: לא לנסות שוב על 503, או להוסיף jitter/הגבלת ניסיונות גלובלית ב-retry interceptor.
- **R-23**: לדחוס/להמיר ל-WebP את תמונות הכניסה, ולהקטין את ה-favicon לגודל אייקון אמיתי.
- **R-24**: להוסיף `robots.txt`/`sitemap.xml` בסיסיים.
- **R-25**: להוסיף `[MaxLength]` ב-DTO ו-`HasMaxLength` תואם ב-DB configuration.
- **R-26**: `npm audit fix` (לא `--force`) על תלויות הפיתוח.
- **R-27**: bulk multi-row INSERT יחיד ב-`ReminderPlannerService`, אם בסיס המנויים יגדל משמעותית.
- **R-28**: לוודא/לתעד במפורש את חלון ה-graceful shutdown מול משך זמן ה-restart בפועל של הפלטפורמה.

---

## ג. סדר ביצוע מומלץ (עם תלויות והערכת זמן מצטברת)

1. **להחליט על פלטפורמת האירוח ולהתחיל את ההקמה** (RB-02) — פותח את שאר שלושת החוסמים. *מקביל לשלב 2.* (מצטבר: תלוי בזמינות חיצונית, לא רק מאמץ הנדסי)
2. **לבצע את כל תיקוני הקוד הלא-תלויים** (R-11, R-05, R-10, R-09, R-07, R-08) — ניתן להתחיל מיידית, לא ממתינים לשלב 1. (מצטבר: ~1–2 ימי עבודה)
3. **לאחר שה-DB המנוהל קם** (חלק מ-RB-02): להגדיר `DIRECT_DATABASE_URL` ולוודא גיבוי עובד (RB-01), לתרגל שחזור מולו ולתקן את ה-runbook (R-13). (מצטבר: +יום עבודה אחד, בתלות בזמינות ה-DB)
4. **להגדיר restart policy לפי הפלטפורמה שנבחרה** (RB-03). (מצטבר: +חצי יום עד יום)
5. **לבנות pipeline ה-deploy ותוכנית ה-rollback** (RB-04) — התלוי-הארוך ביותר, אפשר להתחיל לבנות אותו במקביל לשלבים 3-4 ברגע שפלטפורמת היעד ידועה. (מצטבר: +1–2 ימים)
6. **לחבר ניטור/alerting בסביבת הפרודקשן החדשה** (R-12), **להגדיר `UseForwardedHeaders`** לפי הטופולוגיה שהתבררה בפועל (R-06), **ולהגדיר log rotation** בהתאם למנגנון ההרצה (R-14). (מצטבר: +יום עבודה אחד)
7. **ריצת סקריפטי ה-loadtest** (`loadtest/`) מול סביבת staging אמיתית, כדי לקבל את המספר החסר להערכת קיבולת (גודל connection pool בפועל, R-20; נקודת ההשפלה של R-08/R-09).
8. יתר הגבוהים/בינוניים/נמוכים (גל 3) — לבקלוג לשבוע הראשון ואילך, לפי סדר הטבלה.

**סה"כ מצטבר לסגירת כל הקריטיים+גבוהים: כ-5–8 ימי עבודה**, כאשר משך הזמן בפועל תלוי במידה רבה בזמן התגובה החיצוני של הקמת חשבונות האירוח (לא ניתן להאיץ באמצעות מאמץ הנדסי נוסף).

---

## ד. מיטיגציות זמניות

- **אם RB-04 (deploy/rollback pipeline) לא ריאלי לסגור לגמרי לפני ההשקה**: לפחות לתעד ידנית (מסמך טקסט קצר, לא צריך אוטומציה) את שלושת הפקודות הבדיוקות שיש להריץ כדי לחזור לגרסה קודמת — `git checkout <תג-קודם>`, `docker build`/פריסה ידנית, ו-`dotnet ef database update <שם-מיגרציה-קודמת>` — ולוודא ששני אנשים (לא רק המפתח היחיד) יודעים להריץ אותן. זה לא מחליף CD אמיתי, אבל מבטל את התרחיש של "אין שום כפתור ללחוץ עליו".
- **אם R-12 (ניטור) לא ריאלי לחבר Sentry/App Insights לפני ההשקה**: לפחות להגדיר פינג חיצוני חינמי (למשל UptimeRobot/healthchecks.io) שבודק את `/health/live` כל כמה דקות ושולח התראה — 15 דקות הקמה, לא דורש שינוי קוד, סוגר את הפער הכי גרוע ("האתר נפל ואף אחד לא יודע") גם בלי error tracking מלא.
- **אם R-06 (ForwardedHeaders) לא ניתן לאמת לפני ההשקה כי טופולוגיית ה-proxy עדיין לא ודאית**: להשאיר את ה-rate limiting הקיים כפי שהוא (fail-safe בכיוון "יותר מדי הגבלה" ולא "בלי הגבלה בכלל") ולבדוק ידנית ביום ההשקה עצמו האם `RemoteIpAddress` בלוגים משקף IP-ים אמיתיים של מבקרים שונים או כתובת proxy קבועה אחת — זו בדיקה של דקות בודדות שמאששת אם התיקון דחוף מיידית או יכול להמתין לגל 2.
- **אם RB-01 (גיבוי) לא ניתן לסגור לגמרי (Neon PITR) לפני ההשקה**: כמיטיגציה זמנית בלבד, להריץ `pg_dump` ידני ולשמור אותו מחוץ לשרת (למשל הורדה מקומית) פעם ביום באופן ידני עד שה-workflow האוטומטי עובד — עדיף מ"אין שום גיבוי בכלל", גם אם לא בר-קיימא לטווח ארוך.
