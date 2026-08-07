# 01 — ביקורת אבטחה לקראת עלייה לאוויר (Security Readiness)

מסמך זה הוא ביקורת אבטחה (לא ביקורת סגנון/איכות) לקראת עלייה לפרודקשן על שרת יחיד (ללא load balancer, ללא autoscaling), עם עומס צפוי של עד כמה מאות משתמשים במקביל. הוא מבוסס על קריאת קוד ישירה (Backend + Frontend), הרצת `npm audit` ו-`dotnet list package --vulnerable` בפועל, וסריקת היסטוריית git — לא על מסמכי האודיט הקודמים (אלה שימשו כמפה ראשונית בלבד, וכל טענה קונקרטית אומתה מחדש מול הקוד הנוכחי). לא בוצע חיבור לשום מסד נתונים או שרת חי.

כל ממצא מדורג לפי חומרה (🔴/🟠/🟡/🟢) ומאמץ תיקון (S/M/L), ומופיע עם ציטוט קוד מדויק. איפה שנדרש היסק, זה מסומן במפורש כ"השערה, דורש אימות".

---

## 1. אימות וסשנים (Auth & Sessions)

המערכת כוללת שני מנגנוני "זהות" נפרדים לגמרי: (א) סשן עוגייה למנהל היחיד, ו-(ב) "manage token" חתום למנוי ציבורי (אין login/סיסמה למנויים).

### 1.1 סשן מנהל — עוגיית ASP.NET Core Cookie Auth

- הגדרה מלאה ב-`Backend/Tanakh.Api/Program.cs:84-106`: `options.Cookie.Name = "tanakh_admin"`, `HttpOnly = true`, `SecurePolicy = CookieSecurePolicy.Always`, `SameSite = SameSiteMode.Strict`, `ExpireTimeSpan = TimeSpan.FromHours(8)`, `SlidingExpiration = false`.
- **תקין**: עוגייה חתומה (ASP.NET Data Protection), `HttpOnly`+`Secure`+`SameSite=Strict` — קומבינציה זו גם מונעת גישה מ-JS (XSS-resistant יחסית) וגם מבטלת בפועל את רוב וקטורי ה-CSRF בלי צורך בטוקן CSRF נפרד, כי הדפדפן פשוט לא שולח את העוגייה בבקשות cross-site (כולל ניווט top-level).
- מפתח החתימה: ASP.NET Core Data Protection ברירת מחדל (מפתחות נשמרים בדיסק המקומי, לא ב-env var נפרד) — לא אותר קונפיגורציה מפורשת ל-`PersistKeysToFileSystem`/`SetApplicationName` ב-`Program.cs`. **השערה, דורש אימות**: על מכונה יחידה בלי volume קבוע (או אם ה-container מתחלף/נפרס מחדש) כל restart עלול לאבד את מפתח ההצפנה ולפסול את כל העוגיות הקיימות — לא נבדק אם ה-hosting platform (Render לפי `docs/audit/07-infra-and-deploy.md`) שומר filesystem בין deploys.

### 1.2 אין ריענון טוקן, אין invalidation בצד שרת בלוגאאוט — SEC-08

- `LogoutAsync` (`Backend/Tanakh.Api/Controllers/AdminAuthController.cs:153-160`) קורא `HttpContext.SignOutAsync(...)` בלבד — זו קריאה שרק מורה לדפדפן למחוק את העוגייה (מגדיר עוגיית תפוגה בתגובה). אין server-side ticket store/רשימת עוגיות שבוטלו.
- המשמעות: אם עוגיית `tanakh_admin` דלפה (למשל הועתקה מ-DevTools, memory dump, לוג פרוקסי) *לפני* שהמנהל התנתק, אותה עוגייה גנובה תמשיך לעבוד במלואה עד תום 8 השעות — לוגאאוט לא מבטל אותה, כי אין מנגנון גם צד-שרת שמסמן אותה כמבוטלת.
- אין ריענון טוקן (`SlidingExpiration = false`) — פג תוקף באמצע פעולה מוביל ל-401 (`Program.cs:96-100`, `OnRedirectToLogin` מחזיר 401 JSON), וה-guard בפרונט (`Frontend/src/app/admin/admin.guard.ts:11-13`) מפנה חזרה למסך login — טיפול תקין, לא איבוד מידע.
- **חומרה**: 🟡 בינוני — לתקן: להוסיף ticket store מבוסס DB (טבלה שקושרת session id לתוקף), או לפחות endpoint "revoke all sessions" למקרה חשד לדליפה. **מאמץ**: M.

### 1.3 ריבוי מכשירים — אין הגבלת סשנים במקביל

- מכיוון שהעוגייה חתומה (stateless), התחברות ממכשיר חדש **לא** מבטלת עוגיות קיימות ממכשירים אחרים — כל עוגיה תקפה עד 8 שעות בפני עצמה, ואין הגבלה על מספר הסשנים הפעילים. מאחר שיש מנהל יחיד בלבד (`AdminOptions`, `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs`), זה סיכון נמוך בפועל, לא ממצא בפני עצמו.

### 1.4 manage token של מנויים — ללא תפוגה — SEC-06

- `UnsubscribeTokenService.Issue` (`Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:24-31`) כותב timestamp לתוך ה-payload (`$"{subscriberId}|{DateTimeOffset.UtcNow:O}"`), אבל `TryValidate` (`UnsubscribeTokenService.cs:33-71`) **בודק רק את החתימה** — אין שום בדיקת TTL/תפוגה על ה-timestamp שבפיילוד. כלומר טוקן שהונפק היום יעבוד גם בעוד 5 שנים, כל עוד ה-`Hashing:Pepper` לא התחלף.
- הטוקן הזה הוא המנגנון היחיד לגישה ל-`GET/POST /api/v1/subscriptions/me` ול-`POST /api/v1/subscriptions/me/unsubscribe` (`Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-160`) — ואין rate limiting על שלושת ה-endpoints האלה כלל (בניגוד ל-signup/OTP).
- `GET /api/v1/subscriptions/me` (`SubscriptionsController.cs:112-127`) מקבל את הטוקן כ-**query string** (`[FromQuery] string token`) — טוקן ללא תפוגה בכתובת URL נשמר בהיסטוריית דפדפן, בלוגים של שרתי proxy/CDN, ועלול לדלוף דרך כותרת `Referer` לצד שלישי אם יש קישור יוצא בעמוד ההגדרות — ואין `Referrer-Policy` מוגדר בשום מקום (ראו SEC-05).
- **חומרה**: 🟡 בינוני (לא PII רגיש במיוחד — היכולת המקסימלית שנחשפת היא שינוי שעת תזכורת/ביטול הרשמה של אותו מנוי, לא גישה לנתוני מנויים אחרים). **מאמץ**: S — להוסיף בדיקת גיל ל-payload timestamp (`TryValidate` כבר מפרסר אותו) עם TTL סביר (למשל שנה), ולשקול מעבר ל-`POST` עם הטוקן בגוף הבקשה במקום query string ב-`GetPreferencesAsync`.

---

## 2. הרשאות (Authorization) — מיפוי מלא של 41 ה-endpoints

נבדקו כל 13 ה-controllers בפועל (`Backend/Tanakh.Api/Controllers/*.cs`). התוצאה:

- **כל** endpoint תחת `api/v1/admin/*` (למעט `login`/`verify-otp`) נושא `[Authorize(Policy = "AdminOnly")]` ברמת ה-class: `AdminController.cs:15`, `AdminUsersController.cs:14`, `AdminLogsController.cs:15`, `AdminExportController.cs:16`, `AdminSmsController.cs:14`, `AdminStatsController.cs:12`, `AdminSystemController.cs:20`. **לא נמצא אף admin endpoint שנגיש בלי בדיקת role** — כולל בדיקה ידנית של `AdminSystemController` (הכי גדול, 10 actions) ו-`AdminExportController` (export CSV).
- המדיניות `AdminOnly` (`Program.cs:107-108`) היא `RequireClaim(ClaimTypes.Role, "admin")` — תואמת ל-claim שמונפק ב-`VerifyOtpAsync` (`AdminAuthController.cs:136-138`) בלבד, כך שאין דרך לקבל את ה-claim בלי לעבור את שלב ה-OTP.
- **IDOR — נבדק במפורש ולא נמצא**: כל הפעולות שמנוי ציבורי מבצע על "המשאב שלו" (`GetPreferencesAsync`, `UpdatePreferencesAsync`, `UnsubscribeAsync` ב-`SubscriptionsController.cs:112-160`, ו-`UpsertAsync` ב-`ReadingProgressController.cs:35-56`) מקבלות `subscriberId` **אך ורק** מפענוח טוקן HMAC חתום בצד שרת (`unsubscribeTokenService.TryValidate`) — לעולם לא כ-route/query parameter גולמי שהלקוח שולט בו. אין endpoint ציבורי שמקבל `subscriberId`/`{id}` ישירות מהלקוח ושולף לפיו בלי אימות בעלות.
  - ב-endpoints של האדמין יש כן `{id:guid}` גולמי (`AdminUsersController.cs:41,61` — PATCH/DELETE; `AdminLogsController.cs:55` — resolve) — אבל אלה מוגנים ב-`AdminOnly` ומיועדים במפורש לאפשר לאדמין לפעול על *כל* משתמש/רשומה, כך שזה לא IDOR אלא התנהגות מכוונת של לוח בקרה.
- `AdminAuthController.LoginAsync`/`VerifyOtpAsync` (`AdminAuthController.cs:54-144`) הם אנונימיים בכוונה (זו נקודת הכניסה לאימות עצמה) — מוגנים ב-rate limiting ו-lockout בלבד, ראו סעיף 5.
- שני controllers ציבוריים לגמרי בכוונה (ואין בהם מידע רגיש): `SystemController` (`SystemController.cs:17`, מצב תחזוקה/באנר/flags — read-only) ו-`TanakhController`/`JewishCalendarController` (תוכן תנ"ך ולוח שנה — ציבורי במהותו).

**מסקנה**: לא נמצאה בעיית IDOR או admin endpoint חשוף. השכבה הזו בנויה נכון.

---

## 3. ולידציה וקלט (Validation & Input)

### 3.1 SQL Injection — לא נמצא

- כל הגישה ל-DB עוברת דרך EF Core LINQ (פרמטרי אוטומטית) פרט לשלושה מקומות עם `ExecuteSqlInterpolatedAsync` (לא `FromSqlRaw`/`ExecuteSqlRaw`, שנחסמים אוטומטית ב-CI — ראו `.github/workflows/backend-ci.yml:33-38`, שסורק ומכשיל build אם נמצא שימוש בהם): `Backend/Tanakh.Infrastructure/Services/SubscriptionService.cs:276-282`, `Backend/Tanakh.Infrastructure/Reminders/ReminderPlannerService.cs:105-111`, `Backend/Tanakh.Infrastructure/Reminders/ReminderDispatcherService.cs:207-231`. ב-`ExecuteSqlInterpolatedAsync` הביטוי המשורשר (`FormattableString`) מפורמט אוטומטית לפרמטרים על ידי EF Core — אומת ידנית ששום מחרוזת קלט-משתמש גולמית לא מוזרקת ישירות (רק GUIDs/timestamps/idempotency key מחושב).
- אין NoSQL/Mongo וכו' בפרויקט.

### 3.2 XSS — לא נמצא וקטור מנוצל

- שימוש יחיד ב-`bypassSecurityTrustHtml` בכל ה-Frontend: `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts:26`. התוכן (`data.html`) מגיע מ-`legal-content.ts` — קובץ מקור סטטי שכתוב ע"י מפתחים (מסמכי תנאי שימוש/פרטיות), **לא** קלט משתמש או תגובת API. אומת: אין קריאת API שמזינה את `LegalDoc.html`.
- לא נמצא שימוש נוסף ב-`[innerHTML]`, `DomSanitizer`, או שווה-ערך בכל `Frontend/src` (נבדק בחיפוש גורף).
- Angular templates משתמשים ב-interpolation רגיל (`{{ }}`) לכל תוכן דינמי (למשל `DisplayName` בלוח האדמין) — נסגר אוטומטית (auto-escaping), אין stored XSS.

### 3.3 אין העלאת קבצים בפרויקט

- לא נמצא endpoint שמקבל upload של קובץ (לא `IFormFile`, לא multipart) בשום controller — לא רלוונטי.

### 3.4 אין הגבלת אורך על שדות טקסט חופשי בהרשמה — SEC-10

- `SubscriptionRequest` (`Backend/Tanakh.Api/Model/SubscriptionRequest.cs:10,29-33`): `DisplayName`, `TermsVersion`, `PrivacyVersion`, `ConsentText` — כולם `string`/`string?` בלי שום `[MaxLength]`/`[StringLength]`, נשמרים בעמודות `text` בלי הגבלת אורך ברמת ה-DB (`ConsentRecordConfiguration.cs`, `SubscriberConfiguration.cs` — רק `PhoneNumber` ו-`Status` מוגבלים ב-`HasMaxLength`).
- ה-endpoint (`POST /api/v1/subscriptions`) מוגן רק ב-rate limit של 5 בקשות/שעה/IP (`RateLimiterPolicyNames.SubscriptionCreate`) — אין הגבלת גודל body מפורשת (`MaxRequestBodySize`) מעבר לברירת המחדל של Kestrel. לקוח (או IP-ים מרובים) יכול לשלוח `ConsentText`/`DisplayName` בגודל מגה-בייטים בודדים לכל בקשה, שנשמרים לצמיתות ב-DB.
- **חומרה**: 🟢 נמוך (לא חוסם השקה, לא ניצול-לרעה זול בהינתן rate limit) — **מאמץ**: S — להוסיף `[MaxLength]` על ה-DTO ו-`HasMaxLength` תואם על `ConsentRecordConfiguration`/`SubscriberConfiguration`.

---

## 4. סודות וקונפיגורציה (Secrets & Config)

### 4.1 `.env` — מוחרג כראוי, לא נמצאו סודות בהיסטוריית git

- `.gitignore` (שורש) מחריג `.env`, `.env.*` (עם חריג מפורש ל-`!.env.example`) בשתי מקומות שונים בקובץ (`.gitignore:7`, `.gitignore:495-497,515`).
- `git ls-files | grep -i "\.env"` מחזיר רק `Backend/.env.example` — כלומר `.env`/`Backend/.env` **לא** עוקבים אחרי git, כנדרש.
- נסרקה היסטוריית ה-git המלאה (`git log --all -p --full-history`) אחר קבצי `appsettings*.json` ומחרוזות שנראות כמו סוד (`password|secret|pepper|key|apikey`) — הממצא היחיד הוא הסעיף 4.2 למטה. לא אותרו secrets אמיתיים (SMS4FREE key/pass, Hashing:Pepper, Admin:PasswordHash) שנכנסו אי-פעם להיסטוריה.

### 4.2 סיסמת DB מקומית מוטמעת ב-appsettings.Development.json — SEC-09

- `Backend/Tanakh.Api/appsettings.Development.json:10`: `"AppDb": "Host=localhost;Port=5433;Database=tanakh;Username=app_user;Password=[REDACTED]"` — סיסמה מוטבעת בקובץ שכן עוקב אחרי git.
- **הקשר מקל**: זו סיסמת PostgreSQL **מקומי** (docker-compose, פורט 5433 שאינו הפורט הסטנדרטי 5432, מרמז שזה מכוון ל-side-by-side עם מופע postgres אחר) — לא נגיש מהאינטרנט, לא סוד production. `appsettings.json` הבסיסי (`Backend/Tanakh.Api/appsettings.json:1-10`) **לא** מכיל `ConnectionStrings` כלל, ואין `appsettings.Production.json` בריפו — כך שאין ראיה לכך שאותה סיסמה משמשת בפרודקשן.
- **חומרה**: 🟡 בינוני — לא סיכון ישיר (הסיכון האמיתי הוא רק אם מישהו בטעות ישתמש באותה סיסמה גם ב-DB production, או אם ה-DB המקומי חשוף לרשת מעבר ל-localhost). מומלץ בכל זאת לא להטביע credentials (אפילו dev) בקובץ שנשמר ב-git — להעביר ל-`dotnet user-secrets` (כפי ש-`Backend/README.md:52-59` כבר ממליץ לגבי `Sms`/`Hashing:Pepper`/`Admin`, אך לא עבור `ConnectionStrings:AppDb`). **מאמץ**: S.

### 4.3 אין hardcoded default מסוכן ל-secrets בקוד

- `AdminOptions`/`HashingOptions`/`SmsOptions` (`Backend/Tanakh.Infrastructure/Options/*.cs`) מאתחלים את כל השדות הרגישים ל-`string.Empty` (לא "changeme"/ערך-ברירת-מחדל פעיל) — ו-`AdminAuthController.LoginAsync` (`AdminAuthController.cs:60`) בודק במפורש `configured = !string.IsNullOrEmpty(...)` ונכשל-סגור (`401`) אם `Admin:Username`/`Admin:PasswordHash` לא הוגדרו. `HashingService.Hash` (`HashingService.cs:21-25`) זורק exception אם `Hashing:Pepper` ריק, במקום לגבב עם מחרוזת ריקה בשקט.

### 4.4 מצב שגיאות בפרודקשן — אין דליפת stack trace ללקוח

- `Program.cs:223-233`: ב-`Development` בלבד מופעלים `UseDeveloperExceptionPage()`, `MapOpenApi()`, `MapScalarApiReference()`; ב-production רץ `app.UseExceptionHandler()` בלבד, שמפנה ל-`GlobalExceptionHandler`.
- `GlobalExceptionHandler.TryHandleAsync` (`Backend/Tanakh.Api/GlobalExceptionHandler.cs:26-47`) מחזיר ל-לקוח `ProblemDetails` גנרי בלבד (`Title = "An unexpected error occurred."`, ללא `exception.Message`/stack trace) — ה-stack trace המלא (`exception.ToString()`) נכתב **רק** לטבלת `error_log` (`GlobalExceptionHandler.cs:60-68`), שנגישה רק דרך `AdminLogsController` (`AdminOnly`). **תקין** — אין דליפת מבנה DB/שגיאות פנימיות ללקוח קצה.

---

## 5. רשת ותעבורה (Network)

### 5.1 CORS — allowlist מפורש, לא wildcard

- `Program.cs:243-245`: `WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()`, כאשר `allowedOrigins` נקרא מ-`Cors:AllowedOrigins` (`appsettings.Development.json:16-18` מקומית / משתנה סביבה `Cors__AllowedOrigins__0` בפרודקשן לפי `Backend/README.md:80`). **תקין** — אין `*"`, ותואם לדרישת `AllowCredentials()` (שאסורה יחד עם wildcard מבחינת דפדפנים).
- **הערה תפעולית (לא ממצא אבטחה)**: אם משתנה הסביבה לא יוגדר בפרודקשן, `allowedOrigins` יהיה מערך ריק — כלומר CORS ייחסם לגמרי (fail-closed), לא ייפתח לרווחה. זה בטוח אך עלול לשבור את האתר בטעות אם נשכח — ראה גם `docs/audit/07-infra-and-deploy.md` לגבי היעדר `appsettings.Production.json`.

### 5.2 חסרים כותרות אבטחה — SEC-05

- נבדק `Backend/Tanakh.Api/Program.cs` במלואו: הכותרת האבטחתית היחידה שמוגדרת בקוד היא `X-Robots-Tag: noindex, nofollow` על `/api/v1/admin/*` (`Program.cs:249-256`). `app.UseHsts()` פעיל בפרודקשן (`Program.cs:232`) — זו הכותרת האבטחתית השנייה היחידה (ומגיעה מ-middleware מובנה, לא נכתבת ידנית).
- **לא נמצאה** שום הגדרה של `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` — לא ב-`Program.cs`, לא ב-`Frontend/src/assets/_headers` (הקובץ היחיד עם כותרות סטטיות, ומכיל רק את שורות 8-9 שהוזכרו לעיל), ולא ב-`Frontend/src/index.html` (נבדק — אין meta tag של CSP).
- **השלכה**: בלי `X-Frame-Options`/`frame-ancestors` ב-CSP, לוח הניהול (ואתר הקריאה) פתוחים ל-clickjacking עקרונית. בלי `X-Content-Type-Options: nosniff`, יש חשיפה תיאורטית ל-MIME-sniffing. בלי `Referrer-Policy`, כתובות עם טוקנים ב-query string (כמו SEC-06 לעיל) עלולות לדלוף ב-`Referer`.
- **חומרה**: 🟡 בינוני — לא חוסם השקה בפני עצמו, אך משלים כמה ממצאים אחרים (SEC-06). **מאמץ**: S — להוסיף middleware פשוט ב-`Program.cs` (בדומה ל-middleware הקיים ב-שורות 249-256) שמוסיף את ארבעת הכותרות לכל תגובה, ולעדכן את `Frontend/src/assets/_headers` עם בלוק `/*` גורף לכל הנתיבים (לא רק `/admin-x9k2/*`).

### 5.3 Rate limiting — קיים לנקודות הקריטיות, אך עם שני חורים ממשיים

- שלוש מדיניות מוגדרות (`Program.cs:109-155`, `RateLimiterPolicyNames.cs`): `AdminLogin` (5/15דק'/IP), `SubscriptionOtpRequest` (5/15דק'/IP), `SubscriptionCreate` (5/שעה/IP) — כולן `FixedWindowRateLimiter` לפי `RemoteIpAddress`.

**SEC-04 — אין rate limiting על `verify-otp` של האדמין, וה-OTP הפעיל הוא רשומה גלובלית אחת (לא קשור ל-session/IP שהתחיל את ה-login)**

- `[HttpPost("verify-otp")]` (`AdminAuthController.cs:98`) **לא** נושא `[EnableRateLimiting]` (בניגוד ל-`login` שכן, `AdminAuthController.cs:55`). ההגנה היחידה היא הנעילה הפנימית אחרי 3 ניסיונות כושלים על אותה שורת `otp_codes` (`AdminAuthController.cs:114-128`, `MaxOtpAttempts = 3`).
- `VerifyOtpAsync` שולף את ה-OTP הלא-פג-תוקף/לא-משומש **האחרון בכלל המערכת** (`AdminAuthController.cs:101-104`: `.Where(o => !o.Used && o.ExpiresAt > DateTimeOffset.UtcNow).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync(...)`) — בלי שום קישור ל-session/IP/cookie שביצע את ה-`login` שיצר אותו קוד.
- **תרחיש התקפה**: כל צד שלישי אנונימי (בלי סיסמת אדמין!) יכול לקרוא ל-`POST /api/v1/admin/auth/verify-otp` עם קודים אקראיים בזמן שחלון ה-OTP האמיתי (5 דקות) פתוח (למשל בכל פעם שהאדמין מנסה להתחבר). אחרי 3 ניחושים שגויים, אותה שורת OTP ננעלת (`otp.Used = true`, שורה 120) — **כולל עבור האדמין האמיתי שמנסה להשלים את ה-login שלו באותו רגע**. זו לא פרצת גישה (הסתברות לפצח קוד בן 6 ספרות ב-3 ניסיונות זניחה), אלא וקטור מניעת-שירות זול: תוקף שרואה/מנחש שיש ניסיון login (או פשוט מריץ בקשה כל כמה שניות) יכול למנוע מהאדמין היחיד להתחבר במשך זמן בלתי מוגבל, בלי שום credential.
- **חומרה**: 🟠 גבוה (פגיעה זמינות ודאית בתנאים פשוטים, על נכס יחיד וקריטי — כניסת האדמין היחיד למערכת). **מאמץ**: S — להוסיף `[EnableRateLimiting]` (מדיניות IP חדשה, נניח 10/15דק') על `verify-otp` בנוסף לנעילה הקיימת, ולשקול קישור ה-OTP לזהות הבקשה (למשל cookie חתום זמני שנוצר ב-login ונדרש גם ב-verify) כדי שלא כל אנונימי יוכל בכלל לנסות.

- שאר ה-endpoints הציבוריים (`TanakhController`, `JewishCalendarController`, `SystemController`, וכן שלושת ה-endpoints של `SubscriptionsController.cs:112-160` תחת `/me`) **אין להם שום rate limiting** — ראו גם SEC-03 להשפעה הקונקרטית ביותר של זה.

### 5.4 SEC-03 — קריאת HTTP חיצונית לא-ממוטמנת עם `new HttpClient()` לכל בקשה — סיכון עומס על שרת יחיד

- `JewishCalendarService.FillJewishCalendarAsync` (`Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:53-69`) יוצר `new HttpClient()` **מקומי** (שורה 55) בכל קריאה, ולא דרך `IHttpClientFactory`/`AddHttpClient` (כפי שנעשה נכון עבור `ISmsSender`/`ISmsBalanceService` ב-`Program.cs:159-166`). אין timeout מוגדר במפורש (ברירת מחדל 100 שניות), ואין שום cache — כל קריאה ל-`GET /JewishCalendar/getJewishCalendar` שולחת בקשה סינכרונית חדשה ל-`https://www.hebcal.com/hebcal` (שורה 57).
- ה-endpoint הזה **אנונימי, ללא rate limiting** (`JewishCalendarController.cs:20-25`), ונקרא מ-`entrance.component.ts:32` (`Frontend/src/app/components/entrance/entrance.component.ts`) — כלומר **בכל טעינת האתר** על ידי כל מבקר.
- `new HttpClient()` ליצירה חוזרת-ונשנית הוא anti-pattern מוכר של .NET שגורם ל-socket/port exhaustion תחת עומס מתמשך (כל instance מחזיק socket עד ה-TCP TIME_WAIT, ואינו משותף עם instances אחרים) — בדיוק התרחיש שהמפרט הזה מזהיר ממנו במפורש ("נפילת שירות ודאית בעומס הצפוי" מוגדר כ-🔴 קריטי). על שרת יחיד ללא autoscaling, עם "כמה מאות משתמשים במקביל" שכל אחד טוען את דף הכניסה (ומכאן קורא לזה), זהו ה-endpoint הכי חשוף ל-cascading failure בכל האפליקציה: אם hebcal.com מאט/נופל, כל בקשה נתקעת עד ל-timeout של 100 שניות **בלי** cache שיכול לספוג את זה, ובמקביל ה-thread/socket pool מתכלה.
- לשם השוואה: תוכן התנ"ך (`CacheProvider`/`MemoryTanakhCache`, תפוגה 12 שעות) ו-`AppSettingsService` (מטמון 5 דק') **כן** ממוטמנים כראוי — זו אנומליה נקודתית ב-service אחד, לא דפוס כללי בקוד.
- **חומרה**: 🟠 גבוה (לא 🔴 רק כי "ודאי" תלוי בזמינות/מהירות hebcal.com בפועל, שלא נבדקה כאן — אבל התבנית הטכנית (`new HttpClient()` ללא cache, ללא timeout, על נתיב-קריאה של כל מבקר) היא בדיוק סוג התקלה שגורמת לנפילות בעומס אמיתי). **מאמץ**: S — לרשום את `IJewishCalendarService`/ה-HttpClient שלו דרך `AddHttpClient` (כמו שתי הדוגמאות הקיימות ב-`Program.cs:159-166`) עם timeout מפורש, ולהוסיף cache יומי (התוצאה תקפה לכל היום העברי הנוכחי) דרך `ITanakhCache`/`IMemoryCache` הקיימים.

### 5.5 SEC-02 — אין `UseForwardedHeaders` — Rate limiting וחישובי IP-hash עלולים להישבר מאחורי reverse proxy

- נבדק גורף בכל `Backend`: **לא נמצא** אף שימוש ב-`ForwardedHeadersMiddleware`/`app.UseForwardedHeaders(...)`/קריאה ל-`X-Forwarded-For` בשום מקום. כל שימושי הזיהוי לפי IP מסתמכים ישירות על `HttpContext.Connection.RemoteIpAddress`:
  - שלוש מדיניות ה-rate limiting (`Program.cs:122,136,148`).
  - חתימת IP ל-audit log (`AdminAuthController.cs:164`) ולרשומות הסכמה (`SubscriptionsController.cs:92`, מוזרם ל-`SubscriptionService.RecordConsentAsync`).
- **הבעיה**: Kestrel (שרת ה-.NET המובנה שרץ בתוך ה-container, `Backend/Dockerfile:18`) כמעט תמיד רץ מאחורי reverse proxy כלשהו בפרודקשן (זו ההמלצה הרשמית של מיקרוסופט לכל פריסה — לא מומלץ לחשוף Kestrel ישירות לאינטרנט). `docs/audit/07-infra-and-deploy.md` מתעד ראיות ל-Cloudflare Pages/Render כיעדי אירוח מתוכננים — שניהם טיפוסית ממוקמים מאחורי proxy/edge משלהם. **השערה, דורש אימות**: לא נמצא בריפו קובץ קונפיגורציית reverse proxy קונקרטי (nginx וכו', כפי שגם `07-infra-and-deploy.md` סעיף 1.3 מאשר), כך שלא ניתן לקבוע בוודאות אם הפריסה בפועל תמקם proxy לפני ה-container — אבל זו הקונפיגורציה הסבירה/המומלצת לכל hosting platform מודרני.
- אם כן קיים proxy: **`RemoteIpAddress` יהיה כתובת ה-proxy הפנימית עבור כל בקשה, זהה לכולם**. המשמעות: (1) שלוש מדיניות ה-rate limiting יהפכו ל"דלי" משותף אחד לכל המבקרים במקום דלי-לכל-IP — למשל `SubscriptionCreate` (5 הרשמות/שעה) תיהפך ל"5 הרשמות בשעה **לכל האתר**", כך שהמבקר השישי שינסה להירשם בכל שעה יידחה תמיד, ללא קשר למי הוא — מניעת-שירות עצמית על פיצ'ר ליבה ציבורי; ו-`AdminLogin` תיהפך לדלי משותף שגם תוקף אנונימי יכול למצות כדי לחסום את האדמין האמיתי. (2) `ip_hash` ב-`consent_records`/`audit_log` יהיה זהה לכל הרשומות (חסר תוקף ראייתי לצורך תיקון 13 לחוק הגנת הפרטיות, שהמערכת בפירוש נועדה לתמוך בו — ראו `Backend/Tanakh.Domain/Entities/ConsentRecord.cs:11`).
- **חומרה**: 🟠 גבוה (מותנה בטופולוגיית הפריסה בפועל, שלא אומתה כאן — אך הסבירות גבוהה וההשלכה חמורה: מניעת-שירות עצמית על הרשמה ציבורית ועל login אדמין). **מאמץ**: S — להוסיף `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto })` עם `KnownProxies`/`KnownNetworks` מוגדרים נכון (או `ForwardLimit`/`TrustedNetworks` בהתאם לפלטפורמת האירוח בפועל) לפני `app.UseRouting()` ב-`Program.cs`. **חובה לאמת קודם** מהי טופולוגיית הפריסה בפועל (יש/אין proxy, ומהי כתובתו) לפני שקובעים אילו proxies לסמוך עליהם — סמיכה על header לא-מאומת (בלי `KnownProxies` מוגבל) פותחת וקטור spoofing חדש (לקוח יכול לזייף `X-Forwarded-For` ולעקוף rate limiting לגמרי).

### 5.6 CSRF — מכוסה כראוי, אין ממצא

- ראו סעיף 1.1 — `SameSite=Strict` על עוגיית האדמין מספק הגנת CSRF אפקטיבית בלי צורך בטוקן ייעודי, ל-endpoints ה-admin (שהם היחידים עם side-effects מאחורי אימות מבוסס-עוגייה). ה-endpoints הציבוריים (subscribe/reading-progress) לא מבוססי-עוגייה כלל (טוקן ב-body), כך שאין להם חשיפת CSRF קלאסית מלכתחילה.

---

## 6. תלויות (Dependencies)

### 6.1 Frontend — `npm audit`

- **מלא** (כולל dev): `27 vulnerabilities (3 low, 12 moderate, 12 high)` — כל השרשראות שנמצאו (`inquirer`→`external-editor`→`tmp`, `webpack-dev-server`→`sockjs`→`uuid`, `socket.io-adapter`→`ws`) הן תלויות **פיתוח בלבד** (Lighthouse CI, Playwright/webpack dev server tooling) — לא נבדל בין `dependencies` ל-`devDependencies` בפלט המלא, אך אומת מול הרצה נפרדת.
- **`--omit=dev`** (מה שבאמת נשלח ל-production בבנייה): **`found 0 vulnerabilities`**. כלומר אין חשיפה ידועה בקוד שרץ בדפדפן המשתמש.
- **חומרה**: 🟢 נמוך — התלויות הפגיעות לא נשלחות ל-production ולא נחשפות למשתמש קצה, אך שווה לנקות (`npm audit fix`, לא `--force` בלי בדיקה) כי הן עדיין רצות במכונות מפתחים וב-CI. **מאמץ**: S.

### 6.2 Backend — `dotnet list package --vulnerable --include-transitive`

- הורץ בפועל על `Backend/Tanakh.sln` (כולל טרנזיטיביות, כל 4 הפרויקטים) מול NuGet.org: **"has no vulnerable packages"** עבור `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Api`, `Tanakh.Tests` — כלומר 0 חבילות NuGet פגיעות ידועות, כולל טרנזיטיביות.

### 6.3 חבילות "ישנות"/לא-מתוחזקות — לא אותרה אף אחת שניתן לאמת בוודאות

- כל חבילות ה-NuGet נעולות ל-`10.0.x` (עדכניות ל-.NET 10, `docs/audit/03-backend.md` סעיף 2) — אין ראיה לגרסה מיושנת. בצד ה-Frontend, `Frontend/package.json:17-27` נועל ל-Angular `^22.1.0` (עדכני). לא אותרה חבילת production כלשהי עם evidence קונקרטי (למשל CVE פתוח לא-מתוקן, או absence של release ב-2+ שנים) שניתן לצטט כאן — לא בוצעה השוואה ידנית מול תאריכי release ב-npm/NuGet של כל חבילה (מחוץ לתחום הכלים שהורצו), כך שזהו תחום **לא-מאומת** מעבר לתוצאות ה-audit הפורמליות לעיל.

---

## 7. דאטהבייס (Database)

### 7.1 הרשאות DB — least-privilege, תקין

- `Backend/db/roles/migrations_user.sql`: `migrations_user` הוא ה-owner של סכמת `public` (שורה 32) ומריץ DDL בלבד (migrations/CI) — `REVOKE CREATE ON SCHEMA public FROM PUBLIC` (שורה 35) מונע מכל role אחר (כולל `app_user`) ליצור/לשנות טבלאות.
- `Backend/db/roles/app_user.sql`: `app_user` (מה ש-`ConnectionStrings:AppDb` — חיבור ה-runtime של האפליקציה — משתמש בו) מקבל **רק** `SELECT, INSERT, UPDATE, DELETE` על טבלאות קיימות + `USAGE, SELECT` על sequences (שורות 26-27) — **אין** לו הרשאת DDL כלל. זהו הפרדת-תפקידים נכונה: גם אם יימצא באג injection כלשהו בעתיד בקוד שרץ תחת `app_user`, לא ניתן ליצור/למחוק טבלאות או לשנות סכמה דרכו.
- **אין ממצא** — התבנית הזו תקינה ומיושמת נכון.

### 7.2 גיבוב שדות רגישים

- **סיסמת אדמין**: PBKDF2-HMACSHA256, **210,000 איטרציות**, salt 16 בייט אקראי, hash 32 בייט (`Backend/Tanakh.Infrastructure/Services/AdminPasswordHasher.cs:11-21`) — תואם המלצת OWASP 2024+ למינימום איטרציות. השוואה ב-`Verify` (`AdminPasswordHasher.cs:47`) עם `CryptographicOperations.FixedTimeEquals` — עמיד ל-timing attack. **תקין**.
- **`consent_records.ip_hash`/`audit_log.ip_hash`**: HMAC-SHA256 עם `Hashing:Pepper` משותף (`HashingService.Hash`, `Backend/Tanakh.Infrastructure/Services/HashingService.cs:19-30`) — לא hash חד-כיווני "רגיל" (SHA256 ללא מפתח), אלא HMAC ממופתח, מה שמונע rainbow-table על כתובות IP (טווח ערכים קטן יחסית). **תקין**, בכפוף לממצא SEC-02 (אם ה-IP שנכנס ל-hash הוא כתובת ה-proxy ולא הלקוח האמיתי, ה-hash תקין קריפטוגרפית אך חסר משמעות ראייתית).
- **קודי OTP** (`otp_codes.code_hash`, `subscriber_otp_codes.code_hash`): אותו HMAC-SHA256 עם אותו pepper (`AdminAuthController.cs:82`, `SubscriptionService.cs:61`) — תקין כשלעצמו, אבל ראו SEC-07 להיפגמות דרך `sms_log`.
- **מספרי טלפון** (`subscribers.phone_number`): נשמרים **בטקסט גלוי** (E.164), לא מגובבים — סביר והכרחי כאן (המערכת חייבת לשלוח SMS בפועל למספר, אי אפשר לשלוח ל-hash). לא ממצא.

### 7.3 SEC-07 — קודי OTP נשמרים בטקסט גלוי בתוך `sms_log.message`

- `Sms4FreeSmsSender.SendAsync`/`LogAsync` (`Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:104-114`) כותב את פרמטר `message` **כפי שהתקבל** (כולל הקוד המספרי) לעמודת `sms_log.message` (`text`, ללא מיסוך) — הן עבור OTP אדמין (`AdminAuthController.cs:91`: `$"קוד האימות שלך למערכת הניהול: {code} ..."`) והן עבור OTP מנוי (`SubscriptionService.cs:70`: `$"קוד האימות שלך: {code} ..."`).
- בעוד ש-`otp_codes.code_hash`/`subscriber_otp_codes.code_hash` שומרים רק hash (לא ניתן לשחזור), אותו קוד בדיוק זמין בטקסט גלוי דרך `GET /api/v1/admin/sms/log` וייצוא `GET /api/v1/admin/export/sms-log` (`AdminSmsController.cs:45-64`, `AdminExportController.cs:72-91`) — כלומר עיצוב ה-hashing מנוטרל חלקית על ידי לוג צדדי.
- **הקשר מקל**: הגישה ל-`sms_log` דורשת כבר הרשאת `AdminOnly` (כלומר מי שיכול לקרוא את זה כבר יכול לגשת לכל שאר ה-DB דרך לוח הבקרה בין כה) והקודים פגי-תוקף תוך 5-10 דקות — כך שהסיכון בפועל נמוך יחסית, אבל זהו slippage אמיתי בעקרון "לעולם לא לשמור OTP בטקסט גלוי".
- **חומרה**: 🟡 בינוני. **מאמץ**: S — להחליף את הקוד בפלייסהולדר לפני הכתיבה ל-`sms_log` (למשל `[REDACTED]` בהודעה שנשמרת, תוך שמירת ההודעה המלאה רק לצורך השליחה בפועל דרך ה-provider).

### 7.4 SEC-01 — אין גיבוי עובד כרגע (blocking לפני עלייה לאוויר)

- `.github/workflows/backend-backup.yml` (גיבוי יומי, cron `"0 3 * * *"`) **תלוי במפורש** ב-secret בשם `DIRECT_DATABASE_URL` (שורה 26) — וההערות בראש הקובץ (שורות 4-12) קובעות בבירור: *"not yet configured, since no Neon project exists for this app yet... this workflow will fail at the 'Take backup' step; that's expected"*.
- כלומר: **אין נכון לעכשיו אף גיבוי אוטומטי עובד** למסד הנתונים — לא ב-CI (נכשל בכוונה, כמתועד), ולא נמצא מנגנון גיבוי חלופי בקוד/קונפיגורציה (`Backend/db/dumps/pg_dump.sh` הוא רק ה-script שה-workflow *היה* מריץ, לא רץ עצמאית). התלות היחידה שמוזכרת כ"primary/fast recovery path" (`.github/workflows/backend-backup.yml:4`) היא point-in-time restore המובנה של Neon — אבל `docs/audit/04-database.md` סעיף 1 מאשר במפורש **שאין עדיין פרויקט Neon מסופק** לסביבת production. כלומר, גם מסלול ה-recovery ה"ראשי" וגם ה"משני" אינם קיימים בפועל כרגע.
- זהו בדיוק התרחיש שהמפרט מגדיר כ-🔴 קריטי ("אובדן נתונים"): שרת פרודקשן יחיד, בלי replica, בלי גיבוי עובד — כל כשל חומרה/דיסק/מחיקה בטעות (כולל למשל הרצה שגויה של `--reset-db` מול סביבה לא נכונה, אם מישהו יעקוף בטעות את ה-guard ב-`Program.cs:203-206`) שווה לאובדן נתונים מוחלט וסופי, כולל רשומות `consent_records`/`audit_log` שנועדו לשמש כראיה משפטית לפי תיקון 13.
- **חומרה**: 🔴 קריטי — חוסם לייב. **מאמץ**: M (תלוי בפרוביז'ן בפועל של Neon/DB מנוהל וקביעת ה-secret — הקוד/ה-workflow כבר קיימים ומוכנים, חסר רק החיבור לתשתית אמיתית).

---

## סיכום ממצאים

| ID | תיאור | חומרה | מאמץ | קובץ:שורה |
|---|---|---|---|---|
| SEC-01 | אין גיבוי DB עובד — workflow הגיבוי היומי תלוי ב-secret `DIRECT_DATABASE_URL` שלא הוגדר, ואין עדיין פרויקט Neon; גם ה-PITR ה"ראשי" וגם הגיבוי ה"משני" לא קיימים בפועל | 🔴 קריטי | M | `.github/workflows/backend-backup.yml:4-12,26` |
| SEC-02 | אין `UseForwardedHeaders` — אם השרת רץ מאחורי reverse proxy (סביר לכל פלטפורמת אירוח מודרנית), כל שלוש מדיניות ה-rate limiting (`AdminLogin`/`SubscriptionOtpRequest`/`SubscriptionCreate`) הופכות לדלי משותף לכל המבקרים, ו-`ip_hash` באודיט/הסכמות מאבד תוקף ראייתי | 🟠 גבוה | S | `Backend/Tanakh.Api/Program.cs:122,136,148` |
| SEC-03 | `JewishCalendarService` יוצר `new HttpClient()` חדש בכל בקשה, ללא cache וללא timeout מפורש, לקריאה חיצונית ל-hebcal.com בכל טעינת דף כניסה — סיכון socket exhaustion / hang תחת עומס על שרת יחיד | 🟠 גבוה | S | `Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:53-69` |
| SEC-04 | `verify-otp` של האדמין ללא rate limiting וללא קישור ל-session שיצר את ה-OTP — כל אנונימי יכול לנעול את חלון ה-OTP (3 ניחושים) ולמנוע מהאדמין האמיתי להתחבר | 🟠 גבוה | S | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:98-128` |
| SEC-05 | חסרות כותרות אבטחה (`CSP`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) בכל התגובות — רק `X-Robots-Tag` ו-`HSTS` קיימים | 🟡 בינוני | S | `Backend/Tanakh.Api/Program.cs:249-256`; `Frontend/src/assets/_headers:8-9` |
| SEC-06 | manage token של מנוי ללא תפוגה בבדיקה (למרות timestamp בפיילוד), מועבר כ-query string ב-GET — סיכון דליפה/שימוש בלתי-מוגבל בזמן | 🟡 בינוני | S | `Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:33-71`; `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-127` |
| SEC-07 | קודי OTP (אדמין ומנוי) נשמרים בטקסט גלוי בתוך `sms_log.message`, נגיש/ניתן-לייצוא לאדמין — מנטרל חלקית את ה-hashing של `otp_codes`/`subscriber_otp_codes` | 🟡 בינוני | S | `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:104-114` |
| SEC-08 | לוגאאוט אדמין הוא client-side בלבד (`SignOutAsync` מוחק עוגייה) — אין server-side ticket store; עוגייה גנובה נשארת תקפה עד 8 שעות גם אחרי logout | 🟡 בינוני | M | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:153-160`; `Backend/Tanakh.Api/Program.cs:84-106` |
| SEC-09 | סיסמת DB מקומית מוטבעת בטקסט גלוי ב-`appsettings.Development.json` (DB מקומי בלבד, לא production) | 🟡 בינוני | S | `Backend/Tanakh.Api/appsettings.Development.json:10` |
| SEC-10 | אין הגבלת אורך שרתית על שדות טקסט חופשי בהרשמה (`DisplayName`, `ConsentText`, `TermsVersion`, `PrivacyVersion`) — רק rate limit של 5/שעה/IP מגן | 🟢 נמוך | S | `Backend/Tanakh.Api/Model/SubscriptionRequest.cs:10,29-33` |
| SEC-11 | 27 חולשות ב-`npm audit` המלא (3 נמוכות, 12 בינוניות, 12 גבוהות) — כולן ב-devDependencies בלבד (tooling של Lighthouse/Playwright), `npm audit --omit=dev` מחזיר 0 | 🟢 נמוך | S | `Frontend/package.json` (devDependencies) |

**ללא ממצא (נבדק ואומת כתקין)**: הרשאות admin לכל ה-41 endpoints (אין IDOR, אין admin endpoint לא-מוגן), הזרקת SQL (0 מקומות, כל ה-raw SQL מפורמט/פרמטרי ונחסם ב-CI), XSS (שימוש יחיד ב-`bypassSecurityTrustHtml` על תוכן מפתחים סטטי בלבד), CORS (allowlist מפורש, לא wildcard), CSRF (מכוסה ע"י `SameSite=Strict`), חבילות NuGet פגיעות (0 מתוך כל 4 הפרויקטים כולל טרנזיטיביות), הרשאות DB (least-privilege אמיתי, `app_user` בלי DDL), גיבוב סיסמת אדמין (PBKDF2 210k, תואם OWASP), .env לא ב-git ולא בהיסטוריה, מצב שגיאות פרודקשן (לא דולף stack trace/מבנה DB).
