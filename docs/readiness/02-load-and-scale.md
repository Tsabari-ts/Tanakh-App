# 02 — עומס, קיבולת וחוסן (Load / Scale / Resilience) — Tanakh

> מסמך זה הוא ביקורת עומס/קיבולת/חוסן לקראת עלייה לאוויר. **הנחת יסוד קריטית**: הפרודקשן רץ על שרת יחיד (VPS/פיזי) — ללא load balancer וללא autoscaling. כל צוואר בקבוק כאן הוא צוואר בקבוק של מכונה אחת. עומס צפוי: עד כמה מאות משתמשים במקביל, כמה אלפים ביום; יש לבדוק גם עמידות בספייק פתאומי של פי 3-5 (שיתוף בקבוצת וואטסאפ/פייסבוק גדולה). המסמך מבוסס על קריאת קוד בפועל (לא חיבור ל-DB/שרת חי, לא הרצת load test). כל ממצא מגובה בקובץ ושורה; ציטוטי קוד מדויקים; ערכי סוד מוצגים כ-`[REDACTED]`. מסמכי `docs/audit/00`-`07` שימשו כמפה ראשונית ואומתו מול הקוד הנוכחי.

## מקרא חומרה

| רמה | הגדרה |
|---|---|
| 🔴 קריטי — חוסם לייב | נפילת שירות ודאית בעומס הצפוי, אובדן נתונים |
| 🟠 גבוה — לתקן לפני לייב | השפלת ביצועים משמעותית בפיק |
| 🟡 בינוני — שבוע ראשון | לא מסכן השקה אך יכאב מהר |
| 🟢 נמוך — בהמשך | חוסן ארוך טווח |

Effort: `S`(≤1h) / `M`(≤1 יום) / `L`(>1 יום).

---

## תקציר ממצאים

| ID | חומרה | כותרת | Effort |
|---|---|---|---|
| LOAD-01 | 🔴 | אין עדות בריפו למנגנון החייאה (process supervisor) אחרי קריסה — שרת יחיד ללא רשת ביטחון | S–M (תלוי בפלטפורמה) |
| LOAD-02 | 🟠 | `TanakhTextService` בונה מחדש את כל מילון התנ"ך (929 פרקים, 23,206 פסוקים) בכל קריאת פרק בודדת | M |
| LOAD-03 | 🟠 | `JewishCalendarService` יוצר `new HttpClient()` גולמי בכל קריאה, ללא timeout מוגדר וללא קאש | S–M |
| LOAD-04 | 🟠 | `/health/ready` לא בודק חיבור DB בכלל — "בריא" מדומה גם כשה-DB למטה | S |
| LOAD-05 | 🟠 | אין `Cache-Control`/`ETag`/דחיסת תגובה על ה-API של תוכן התנ"ך, למרות שהתוכן קבוע כמעט לחלוטין | S |
| LOAD-06 | 🟡 | גודל connection pool ל-DB לא מוגדר בקוד/בקונפיג — לא ניתן לאמת את הערך בפועל | S |
| LOAD-07 | 🟡 | `ReminderDispatcherService` — N+1: שאילתת מנוי בודדת + `SaveChangesAsync` בודד לכל שורה בבאץ' | M |
| LOAD-08 | 🟡 | ה-retry interceptor בפרונט מכפיל עומס עד פי 3 בדיוק כשהשרת כבר עמוס (5xx) | S–M |
| LOAD-09 | 🟡 | עמוד הכניסה (`entrance`) טוען כ-590KB תמונות PNG/JPG לא ממוטבות + favicon של 570KB כ-og:image | S |
| LOAD-10 | 🟢 | `ReminderPlannerService` מכניס שורת `reminder_deliveries` אחת בכל פעם, ברצף, לכל מנוי פעיל | M |
| LOAD-11 | 🟢 | אין override מפורש ל-graceful shutdown timeout — חל ברירת המחדל של ASP.NET Core | S |

---

## 1. מיפוי צווארי בקבוק

### 1.1 שאילתות DB

נבדקו `AdminService.cs`, `SubscriptionService.cs`, `ReadingProgressService.cs`, `ReminderPlannerService.cs`, `ReminderDispatcherService.cs`, `RetentionHostedService.cs` מול רשימת האינדקסים המלאה שתועדה ב-`docs/audit/04-database.md` סעיף 6 (אומתה כאן שוב מול `AppDbContextModelSnapshot.cs`).

**המסקנה: לא נמצאה שאילתה חסרת אינדקס רלוונטי.** האינדקסים הקיימים תואמים לדפוסי הקריאה בפועל:
- `ix_reminder_deliveries_status_scheduled_for` תואם בדיוק ל-`WHERE status='pending' AND scheduled_for<=now()` ב-`ClaimDueDeliveriesAsync` (`Backend/Tanakh.Infrastructure/Reminders/ReminderDispatcherService.cs:220-222`).
- `ix_subscribers_phone_number` (ייחודי) תואם לחיפושי מנוי לפי טלפון ב-`SubscriptionService.cs`.
- `ix_subscriber_otp_codes_phone_number_used_expires_at` תואם לשאילתת ה-guard נגד ניצול לרעה ב-`SubscriptionService.cs:40-42`.
- דפי הניהול (`AdminLogsController`, `AdminSmsController`, `AdminUsersController`) כולם עם `page`/`limit` בפועל בקוד (לא רק בתיעוד) — `Backend/Tanakh.Infrastructure/Services/AdminService.cs:168,175`, `242,248`, `302,310` — כל אחד מבצע `CountAsync` + `ToListAsync` עם `Skip/Take`, לא שליפה מלאה.

**N+1 שנמצא — לא ב-DB הראשי אלא בשירות הרקע (ראו סעיף "שירות התזכורות תחת עומס" למטה, LOAD-07/LOAD-10).**

לוח הבקרה של המנהל (`AdminStatsController.GetOverviewAsync` → `AdminService.GetOverviewAsync`) מבצע כ-10 שאילתות `CountAsync` נפרדות ברצף (`Backend/Tanakh.Infrastructure/Services/AdminService.cs:124-146`) — "צ'אטי" אך לא בעייתי: משתמש מנהל יחיד, לא בנתיב הציבורי, לא רץ בעומס.

### 1.2 Connection Pool

**LOAD-06 🟡 בינוני** — ה-DbContext רשום עם `AddDbContextPool<AppDbContext>` (`Backend/Tanakh.Api/Program.cs:35-51`), עם `EnableRetryOnFailure()` ו-`CommandTimeout(30)` שניות (`Program.cs:42-43`), אך **בלי `poolSize` מפורש** ל-`AddDbContextPool` ו**בלי `Maximum Pool Size`/`Pooling` בשום מקום בקוד**. בוצע חיפוש גורף אחר `Pool Size`/`Maximum Pool`/`MaxPoolSize`/`Pooling` בכל `Backend` — 0 תוצאות. משמעות בפועל: מספר החיבורים בפועל ל-PostgreSQL נקבע כולו על ידי מחרוזת החיבור (`ConnectionStrings:AppDb`, `[REDACTED]`, מגיעה ממשתנה סביבה) ואם היא לא מציינת `Maximum Pool Size` — ברירת המחדל של ספריית Npgsql חלה (לפי תיעוד הספרייה, לא אומת מהריפו). **לא ניתן לאמת מהריפו לבד מהו מספר החיבורים בפועל בפרודקשן** — זהו נתון חסר קריטי להערכת קיבולת (ראו סעיף הקיבולת בתחתית).

**דליפת חיבורים**: לא נמצאה. כל שימוש ב-`AppDbContext` הוא דרך DI (scoped, מוזרק לקונטרולרים/שירותים) או `using IServiceScope scope = scopeFactory.CreateScope()` בשירותי הרקע (`ReminderDispatcherService.cs:65`, `ReminderPlannerService.cs:70`, `RetentionHostedService.cs`) — משוחרר אוטומטית עם סיום ה-scope. אין `new NpgsqlConnection`/`new AppDbContext` ידני שנפתח בלי `using` בשום מקום שנבדק.

### 1.3 פעולות חוסמות (Blocking Operations)

**LOAD-02 🟠 גבוה — `TanakhTextService` בונה מחדש את כל מילון התנ"ך (23,206 פסוקים) על כל בקשת פרק בודדת, ולא רק פעם אחת.**

`Backend/Tanakh.Api/Data/TanakhData.json` הוא **22,195,818 בתים (~22.2MB)**, ומכיל 929 רשומות פרק (`structures`), עם סה"כ **23,206 פסוקים** (נספר ישירות מהקובץ). `CacheProvider.GetFullTanakhFromCacheAsync` (`Backend/Tanakh.Infrastructure/CacheProvider.cs:34-65`) אכן קורא וטוען את הקובץ **פעם אחת בלבד** מהדיסק, וקושר אותו ל-`ITanakhCache` (`MemoryTanakhCache`, TTL 12 שעות, `Size=1` — `Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:15,44-46`). עד כאן תקין.

אבל השכבה מעליו — `TanakhTextService.GetChapterAsync` (`Backend/Tanakh.Api/Services/TanakhTextService.cs:24-39`) — קוראת בכל בקשה בודדת ל-`BuildChapterDictionaryAsync` (`TanakhTextService.cs:41-88`), אשר **עבור כל אחת מ-929 רשומות ה-`Structure` ב-`TanakhContainer` הנתון מהקאש**, מריצה `Regex.Replace(verse, @"<[^>]+>", "")` על **כל פסוק** בפרק (שורה 69: `.Select(verse => Regex.Replace(verse, @"<[^>]+>", "")).ToList()`), בונה אובייקט `Book` חדש לכל אחד מ-929 הפרקים, ומכניס את כולם ל-`Dictionary<string, Book>` חדש — **רק כדי להחזיר רשומה אחת מתוכם** (שורה 29: `dataDictionary.TryGetValue(chosenSection, ...)`). כלומר: קריאת ה-API הכי נפוצה באתר (קריאת פרק — הפעולה המרכזית של האתר) גוררת עיבוד Regex סינכרוני על 23,206 פסוקים ובניית 929 אובייקטים + Dictionary חדש **בכל קריאה**, ולא תוצאה מחושבת פעם אחת ונשמרת בקאש. זהו עומס CPU מיותר לגמרי שחוזר על עצמו בכל בקשת פרק, על הנתיב הכי חם באתר. בעומס רגיל זה כנראה נסבל (עשרות מילישניות לבקשה), אך תחת ספייק פי 3-5 (שיתוף ויראלי) זה הופך את השרת הבודד ל-CPU-bound הרבה יותר מהר ממה שצריך — ואין שום rate limiting על נתיבי `TanakhController` (ה-rate limiting היחיד ב-`Program.cs:109-155` חל רק על login/OTP/הרשמה, לא על קריאת תוכן).

**תיקון**: לשמור בקאש (`ITanakhCache`) את תוצאת `BuildChapterDictionaryAsync` עצמה (ה-`Dictionary<string, Book>` המוגמר), ולא רק את ה-`TanakhContainer` הגולמי — כך העיבוד ירוץ פעם אחת בלבד לכל 12 שעות במקום בכל בקשה. Effort: M.

**בדיקת בריאות הנתונים** (`TanakhDataHealthCheck.cs:20`, `CacheProvider.DataFilesExist()`) בודקת קיום קבצים בלבד (`File.Exists`), לא פותחת/קוראת אותם — זול, תקין, לא בעייתי.

### 1.4 קריאות לשירותים חיצוניים

**LOAD-03 🟠 גבוה — `JewishCalendarService` יוצר `HttpClient` גולמי חדש בכל קריאה, ללא timeout מוגדר וללא קאש.**

`Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:55`:
```csharp
HttpClient httpClient = new HttpClient();
```
בניגוד ל-`Sms4FreeSmsSender`/`SmsBalanceService` שרשומים כ-typed clients דרך `AddHttpClient<...>()` עם timeout מפורש (`Backend/Tanakh.Api/Program.cs:159-166`: 15s ל-SMS, 10s ליתרה), `JewishCalendarService` **לא** רשום כ-HttpClient מוקלד — הוא פשוט מייצר `new HttpClient()` בכל קריאה בפונקציה `FillJewishCalendarAsync` (שורה 53-69). זו אנטי-פטרן ידועה (סיכון תשישות סוקטים בעומס), **ואין override ל-`Timeout`** — ברירת המחדל של `HttpClient` היא 100 שניות. **ואין שום קאש** — כל קריאה מביאה מחדש את לוח השנה השנתי המלא מ-hebcal.com (בניגוד ל-`SmsBalanceService` שיש לו קאש בן 5 דקות, `Backend/Tanakh.Infrastructure/Services/SmsBalanceService.cs:15,34`, ולמרות שנתוני לוח שנה עברי שנתי הם נתון סטטי לכל אורך השנה).

יש שני קוראים לפונקציה זו:
1. **נתיב ציבורי**: `GET /JewishCalendar/getJewishCalendar` (`Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25`) — נקרא מהפרונט אך ורק מ-`entrance.component.ts:32` (`Frontend/src/app/services/api-call.service.ts:14-16`, `getHolidays()`), כלומר **בכל טעינה של עמוד הכניסה** — עמוד הבית של כל מבקר חדש (הראוט `""` מפנה ל-`entrance`, `Frontend/src/app/app.routes.ts:5`). בספייק ויראלי (בדיוק התרחיש שבתדריך) — גל של מבקרים חדשים = גל של קריאות בו-זמניות ל-hebcal.com, כל אחת עם `HttpClient` חדש משלה וללא timeout אפקטיבי קצר, כלומר אם hebcal.com מאט/נתקע — כל בקשה כזו יכולה לתפוס thread/connection עד 100 שניות.
2. **שירות הרקע**: `ReminderDispatcherService.RunDispatchCycleAsync` שורה 81 — קריאה אחת לכל מחזור דיספצ'ר (כל 60 שניות). אם hebcal.com נתקע, **כל מחזור השליחה נחסם** עד ל-timeout (עד 100 שניות), מעכב את שליחת כל התזכורות שהיו אמורות להישלח באותו מחזור.

**תיקון**: לרשום כ-typed `HttpClient` עם timeout קצר (5-10 שניות) דרך `AddHttpClient<IJewishCalendarService, JewishCalendarService>()`, ולהוסיף קאש שנתי (המידע תקף לכל השנה הקלנדרית) בדומה לתבנית של `SmsBalanceService`. Effort: S (timeout) – M (קאש).

**Sms4FreeSmsSender**: timeout מוגדר (15s, ניתן לקונפיגורציה דרך `Sms:TimeoutSeconds`), כשל רשת/timeout נתפס במפורש (`Sms4FreeSmsSender.cs:90-96`) וזורם לתוך מנגנון ה-retry הרגיל של התזכורות (לא מפיל את הבקשה של המשתמש — זהו קורא מתוך שירות רקע, לא מנתיב HTTP של משתמש) — **תקין, אין ממצא**.

### 1.5 זיכרון

כל התנ"ך (`TanakhData.json`, 22.2MB) + מבנה (`TanakhStructure.json`, 11.7KB) נטענים לזיכרון פעם אחת דרך `CacheProvider` ונשמרים ב-`IMemoryCache` (`MemoryTanakhCache`, TTL 12 שעות) — טביעת רגל זיכרון קבועה וידועה מראש (סדר גודל של כמה עשרות MB לאחר דה-סריאליזציה ל-אובייקטי C#, לא נמדד בפועל). `IMemoryCache` מוגבל ל-`SizeLimit=100` (`Program.cs:53-56`), ורק 2 רשומות (`fullTanakh`, `tanakhStructure`) נכנסות אליו עם `Size=1` כל אחת — רחוק מהגבול, אין סיכון לגידול לא חסום מהמטמון הזה. `AppSettingsService` משתמש ב-`IMemoryCache` הזה גם הוא (`Size=1` לכל רשומה, `AppSettingsService.cs:104`) לשתי רשומות בלבד (`maintenance`, `banner`) — גם כאן אין סיכון גידול לא חסום.

**אין עדות למטמון בלתי-חסום (unbounded cache) או לדליפת זיכרון** בקוד שנבדק. הממצא המשמעותי בציר הזיכרון הוא לא "כמה זיכרון תפוס" אלא "כמה עבודת CPU/הקצאות מבוזבזות בכל בקשה" — ראו LOAD-02 לעיל: כל בקשת פרק מקצה מחדש 929 אובייקטי `Book` + `Dictionary` + עשרות אלפי מחרוזות מעובדות-Regex ומשליכה כמעט את כולם לאשפה מיד לאחר מכן — לחץ GC מיותר תחת עומס מקביל, גם אם לא "דליפה" קלאסית.

### 1.6 גדלי תגובה (Response Sizes)

- `GET /Tanakh/books/{section}` ו-`GET /Tanakh/books/main/{book}` — מבוססים על `TanakhStructure.json` (11.7KB סה"כ לכל 39 הספרים) — לא בעייתי בשום גודל.
- `GET /Tanakh/books/{book}/{chapter}` — התגובה מכילה את כל פסוקי הפרק. חישוב לפי הקבצים בפועל: 22,195,818 בתים / 929 פרקים ≈ **23.9KB בממוצע לפרק** (JSON לא דחוס). פרקים ארוכים משמעותית מהממוצע (לדוגמה תהלים קיט, עם 176 פסוקים לעומת ממוצע של כ-25 פסוקים לפרק — פי ~7) צפויים להניב תגובות של **מאות KB בודדות** (לא נמדד ישירות לכל פרק בפועל — השערה מבוססת יחס, דורש אימות אם רוצים מספר מדויק). ראו LOAD-05 להלן — תגובות אלו נשלחות **ללא דחיסה וללא קאשינג HTTP** למרות שהתוכן כמעט קבוע.
- נקודות הקצה של האדמין (`AdminLogsController`, `AdminSmsController`, `AdminUsersController`) — כולן עם `page`/`limit` בקוד בפועל, לא רשימות מלאות. תקין.

---

## 2. קאשינג

**מה קיים היום**: קאש in-process בלבד (`Microsoft.Extensions.Caching.Memory.IMemoryCache`, לא Redis, לא מטמון מבוזר — אומת שוב: 0 תוצאות לחיפוש `Redis`/`IDistributedCache` בכל `Backend`). שתי שכבות שימוש:
1. `ITanakhCache`/`MemoryTanakhCache` — טקסט/מבנה התנ"ך הגולמיים, TTL 12 שעות (`Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:15`).
2. `AppSettingsService` — מצב תחזוקה/באנר, TTL 5 דקות, ומנוקה מיידית בכל כתיבה (`Backend/Tanakh.Infrastructure/Services/AppSettingsService.cs:17,48,69,81`).
3. `SmsBalanceService` — יתרת SMS4FREE, TTL 5 דקות (`Backend/Tanakh.Infrastructure/Services/SmsBalanceService.cs:15`).

**LOAD-05 🟠 גבוה — אין `Cache-Control`/`ETag`/דחיסת HTTP על ה-API של תוכן התנ"ך, למרות שזהו התוכן הכי קבוע באתר.**

נבדק `TanakhController.cs` במלואו (57 שורות) — אין `[ResponseCache]`, אין הגדרת `Cache-Control` בשום action. נבדק `Program.cs` במלואו — **אין `AddResponseCompression`/`UseResponseCompression`** בשום מקום (חיפוש גורף החזיר 0 תוצאות). כלומר כל תגובת JSON (כולל תגובת פרק שיכולה להגיע למאות KB, סעיף 1.6) יוצאת מה-.NET **ללא gzip/brotli וללא כותרות קאש דפדפן/CDN**, על אף שתוכן התנ"ך:
- נטען מקובץ סטטי בריפו (לא DB), משתנה רק עם דיפלוי חדש.
- הוא בדיוק סוג התוכן שאמור לקבל `Cache-Control: public, max-age=<ארוך>, immutable` + `ETag` — כל בקשה חוזרת לאותו פרק (מכל משתמש, לא רק מאותו דפדפן) הייתה יכולה להיחסם ב-CDN/דפדפן ולא להגיע בכלל לשרת היחיד.

זוהי **הזדמנות הקאשינג הגדולה ביותר שלא מנוצלת**: בהינתן שהאתר מיועד ל"כמה אלפים ביום" הקוראים מתוך אותם 929 פרקים בלבד, קאש HTTP משותף (CDN, אם קיים בפריסה — ראו `docs/audit/07-infra-and-deploy.md` סעיף 1.4 לגבי Cloudflare Pages כרמז יעד אירוח לפרונט בלבד, לא ל-API) יכול לחסל את רוב התעבורה הזו לפני שהיא בכלל מגיעה לשרת ה-.NET היחיד. כרגע **כל** קריאת פרק, מכל משתמש, בכל פעם, מגיעה עד ה-backend ומחוללת את החישוב המתואר ב-LOAD-02.

בצד הלקוח יש מיטיגציה **חלקית**: `Frontend/ngsw-config.json:30-42` מגדיר `dataGroup` בשם `tanakh-content` על `https://localhost:5001/Tanakh/books/**` עם אסטרטגיית `"performance"`, `maxAge: "365d"`, `maxSize: 1000` — כלומר ה-Angular Service Worker כן שומר תגובות פרק בקאש מקומי לדפדפן/PWA למשך שנה. אבל זה עוזר רק למשתמש חוזר **באותו דפדפן** (לא CDN, לא משותף בין משתמשים), ולא עוזר כלל לגל של **מבקרים חדשים** (בדיוק תרחיש הספייק) שכל אחד מהם מבצע fetch ראשון נטול-קאש. בנוסף, כתובת ה-`dataGroup` עדיין קשיחה ל-`localhost:5001` (כפי שכבר תועד ב-`docs/audit/07-infra-and-deploy.md` סעיף 3.2) — כלומר לא תפעל כלל מול דומיין פרודקשן אמיתי עד שתתעדכן כחלק מהדיפלוי.

**תיקון**: להוסיף `Cache-Control`/`ETag` על תגובות `TanakhController` (Effort S), ולהפעיל `AddResponseCompression` (Effort S).

**קאש-headers על נכסים סטטיים בפרונט**: נבדק `Frontend/src/assets/_headers` (הקובץ היחיד שנמצא לקונפיגורציית תגובות Cloudflare Pages) — מכיל אך ורק כלל `X-Robots-Tag: noindex, nofollow` לנתיב `/admin-x9k2/*` (שורות 8-9). **אין בו אף כלל `Cache-Control` מפורש** לקבצי ה-JS/CSS המגובבים (hashed) שיוצרים מ-`ng build` (`outputHashing: "all"`, `Frontend/angular.json:83`). Cloudflare Pages מיישם ברירות מחדל סבירות לנכסים סטטיים באופן עצמאי, אך זה **לא מאומת מהריפו** (אין קובץ קונפיגורציה נוסף) — לא נכלל כממצא נפרד מכיוון שהוא תלוי בפלטפורמת אירוח שטרם נבחרה סופית (ראו `docs/audit/07-infra-and-deploy.md`), אך שווה לוודא בעת חיבור הדומיין הסופי.

---

## 3. פרונטאנד תחת עומס / מובייל

**Bundle גודל בפועל** (נמדד ישירות מ-`Frontend/dist/tanakh/browser/`, build קיים בריפו — לא הורץ build חדש):
- סה"כ JS+CSS: **~1.21MB** (1,239,724 בתים).
- Chunk הראשי: `main-YOBPW2AL.js` — 232.7KB. Chunk הגדול הבא: `chunk-Dw0ye5Qx.js` — 212.4KB. `styles-*.css` — 119.4KB.
- תקציב Angular (`Frontend/angular.json:60-81`): initial `maximumError: 500kb`, allScript `maximumError: 1.1mb` — **נאכף ב-build**, ולפי המספרים לעיל השרת בגבולות התקציב שהוגדר (לא נבדק בפועל אילו chunks נטענים ב-initial מול lazy — ראו הראוטים למטה).

**Lazy loading**: אומת ישירות מול `Frontend/src/app/app.routes.ts:1-44` — **כל הראוטים** (`entrance`, `home`, `settings`, `books/:section`, `books/:section/:book`, `books/:section/:book/:chapterNumber/:keepReading`, וכן פאנל האדמין כולו) טעונים דרך `loadComponent`/`loadChildren` (lazy, לא eager). זה עקבי ומדויק מול `docs/audit/01-frontend.md`. **אין ממצא — lazy loading מיושם נכון ובאופן עקבי.**

**Lighthouse — נתונים קיימים אך לא רלוונטיים לפרודקשן**: `Frontend/.lighthouseci/lhr-*.json` (2 ריצות, `/home` ו-`/settings`) מציגים ציון performance 0.54, FCP/LCP של 26-51 שניות ומשקל כולל של ~8.5MB. **נתונים אלו אינם מייצגים פרודקשן**: `Frontend/lighthouserc.json:4` מריץ אותם נגד `npx ng serve --configuration development` — שרת פיתוח לא-ממוטב (ללא minification/tree-shaking מלא) ולא build פרודקשן. אין בריפו מדידת Lighthouse כנגד ה-build האמיתי (`dist/tanakh`) — זהו פער מדידה, לא ממצא ביצועים בפועל. המספרים בפועל ל-production הם ה-bundle sizes שנמדדו ישירות לעיל.

**LOAD-09 🟡 בינוני — עמוד הכניסה (הדף הראשון שכל מבקר חדש רואה) טוען כ-590KB תמונות רקע לא ממוטבות, ו-favicon של 570KB משמש גם כ-og:image.**

`Frontend/src/app/components/entrance/entrance.component.scss` טוען כרקע (`background-image: url(...)`) שלוש תמונות:
- שורה 14: `/assets/pngwing.png` — **315.1KB**
- שורה 68: `/assets/tora.png` — **202.6KB**
- שורה 183: `/assets/IsraelFlag.jpg` — **74.9KB**

סה"כ **~592KB** תמונות PNG/JPG לא דחוסות/לא מומרות ל-WebP/AVIF, נטענות כרקע בעמוד שהוא ברירת המחדל של כל כניסה לאתר (`{ path: "", redirectTo: "entrance", pathMatch: "full" }` — `Frontend/src/app/app.routes.ts:5`). בנוסף, `Frontend/src/index.html:11` מגדיר `<link rel="icon" type="image/png" href="assets/favicon.png">` כאשר `favicon.png` שוקל **569.7KB** (!) — קובץ בגודל favicon אמור להיות בסדר גודל של קילובייטים בודדים, לא חצי מגה-בייט. אותו קובץ 570KB משמש גם כ-`og:image`/`twitter:image` (`index.html:19,23`) — בדיוק התרחיש שהתדריך מתאר (שיתוף קישור בקבוצת וואטסאפ/פייסבוק): כל שרת תצוגה מקדימה (link-preview crawler) שמייצר תצוגה לקישור המשותף יוריד תמונה של 570KB. על חיבור סלולרי איטי, סכום התמונות בעמוד הראשון (592KB רקעים + עד 570KB favicon/og:image, לפי הקשר הטעינה) מוסיף שניות משמעותיות לטעינה הראשונה — ממש בעמוד שבו הכי חשוב שיהיה מהיר (כניסה ראשונה של משתמש חדש/ספייק ויראלי). Effort: S (דחיסה + המרה ל-WebP + הקטנת ה-favicon לגודל אייקון אמיתי, למשל 32×32/180×180).

**LOAD-08 🟡 בינוני — ה-retry interceptor בפרונט מכפיל עומס עד פי 3 בדיוק כשהשרת עמוס.**

`Frontend/src/app/core/interceptors/retry.interceptor.ts:4-17`:
```ts
return next(req).pipe(
  retry({
    count: 2,
    delay: (error: HttpErrorResponse, retryCount) => {
      if (error.status >= 400 && error.status < 500) throw error;
      return timer(Math.pow(2, retryCount) * 500);
    },
  }),
);
```
מיושם על כל בקשת `GET` (שורה 6: `if (req.method !== 'GET') return next(req);`). הלוגיקה תקינה בכוונתה (לא חוזר על 4xx, backoff מעריכי) — אבל **כן** חוזר על 5xx/כשלי רשת, כולל `503`. ה-503 המדויק הזה הוא בדיוק מה שהשרת מחזיר כש-middleware מצב התחזוקה פעיל (`Program.cs:276`) **וגם** הסוג הסביר ביותר של תגובה כשהשרת עמוס/נופל תחת עומס. המשמעות: ברגע שבו השרת הכי פחות מסוגל לספוג עומס נוסף, **כל** לקוח עם בקשת GET כושלת מנסה שוב פעמיים נוספות (500ms, 1s), ובכך מכפיל בפועל את נפח הבקשות שמגיע לשרת בדיוק בשיא העומס/בתחילת נפילה — מנגנון שמחריף מפולת עומס במקום למתן אותה, ללא circuit breaker/jitter שיעצור את זה. Effort: S–M (לדוגמה: לא לנסות שוב על 503, או להוסיף jitter/הגבלת ניסיונות גלובלית).

**רשת בקשות בטעינה ראשונה**: לא נמדד ישירות (ללא הרצת שרת חי, לפי כללי המשימה) — לא ניתן לתת מספר מדויק של בקשות/כפילויות ללא כלי רשת בפועל (למשל DevTools Network על build פרודקשן אמיתי). מצוין כאן כפער מדידה, לא כממצא.

---

## 4. חיבורים במקביל, לוקיישנים וזמן

**WebSocket/SSE**: לא נמצא. חיפוש גורף אחר `WebSocket`/`SignalR`/`EventSource`/`Server-Sent` בכל `Backend`/`Frontend/src` לא החזיר תוצאות רלוונטיות לתקשורת real-time. כל התקשורת היא HTTP רגיל (בקשה-תגובה).

**Polling אגרסיבי בפרונט**: נבדקו כל מופעי `setInterval`/`interval(` תחת `Frontend/src`:
- `Frontend/src/app/core/app-update.service.ts:24` — בדיקת עדכון Service Worker כל 6 שעות. לא בעייתי.
- `Frontend/src/app/admin/admin-date-range.service.ts:23` — רענון אוטומטי בפאנל האדמין. משתמש יחיד (מנהל), לא בנתיב הציבורי.
- `Frontend/src/app/components/subscribe/subscribe.component.ts:296`, `Frontend/src/app/components/read-permission/read-permission.component.ts:44` — טיימרים מקומיים ל-UI (אנימציית טעינה), לא קוראים לשרת.
- `Frontend/src/app/core/tts/web-speech-provider.service.ts:108` — keep-alive ל-Web Speech API בדפדפן, אין קריאת רשת.

**אין polling שמכפיל עומס לינארית עם מספר המשתמשים** — אין ממצא בציר הזה.

**אזורי זמן**: אומת מול `docs/audit/04-database.md` וקוד בפועל — `subscribers.preferred_time` הוא `time without time zone` (שעת קיר מקומית מוצהרת) ו-`subscribers.timezone` הוא IANA zone id (ברירת מחדל `Asia/Jerusalem`), בעוד ש-`reminder_deliveries.scheduled_for` הוא `timestamptz` (מנורמל ל-UTC ב-DB). ההמרה בין השניים עוברת דרך `LocalTimeResolver`/`NextOccurrenceResolver` (`Backend/Tanakh.Domain/Scheduling/`), עם טיפול מפורש ומתועד ב-DST (מעברי שעון קיץ/חורף בישראל) — כולל בדיקות יחידה ייעודיות (`Backend/Tanakh.Tests/LocalTimeResolverTests.cs`). **זהו תכנון תקין ל-timezone-aware scheduling — אין ממצא.** (לצורך שקיפות: `ReminderPlannerService`/`ReminderDispatcherService` עצמם משתמשים תמיד ב-`DateTimeOffset.UtcNow` לחישובי "עכשיו" — עקבי ונכון.)

---

## 5. שירות התזכורות תחת עומס

עיצוב הליבה (outbox pattern, `FOR UPDATE SKIP LOCKED`, `idempotency_key` ייחודי, reaper לשורות תקועות) **תקין ומעוצב היטב לריצה בטוחה במקביל** — אומת ישירות בקוד ותואם את התיאור ב-`docs/audit/05-services-and-integrations.md`:
- לא ניתן לשלוח תזכורת פעמיים בריצה כפולה של המתזמן: ה-INSERT הוא `ON CONFLICT (idempotency_key) DO NOTHING` גולמי (`ReminderPlannerService.cs:105-111`), לא "בדוק ואז הכנס".
- לא ניתן לשלוח תזכורת פעמיים ע"י שני מופעי דיספצ'ר (רלוונטי גם אם היום רץ מופע יחיד בלבד): `ClaimDueDeliveriesAsync` (`ReminderDispatcherService.cs:216-231`) משתמש ב-`UPDATE ... FOR UPDATE SKIP LOCKED` — מנעול שורה אטומי ברמת ה-DB, לא נעילה ברמת האפליקציה.
- ריצות לא יכולות "לחפוף" בצורה מסוכנת: כל מחזור דיספצ'ר טוען רק שורות שהוא עצמו תפס (`status='sending'`), ו-reaper (`ReapStuckSendingRowsAsync`, `ReminderDispatcherService.cs:205-212`) משחרר שורות שנתקעו מעל 10 דקות בחזרה ל-`pending` למקרה של קריסה באמצע עיבוד.

**האם השירות מתחרה על משאבי DB עם בקשות משתמש חיות? כן** — `ReminderPlannerService`/`ReminderDispatcherService` פותחים `IServiceScope` ושואבים `AppDbContext` מאותו pool בדיוק כמו כל controller (`AddDbContextPool<AppDbContext>`, `Program.cs:35`) — אין pool נפרד לשירותי הרקע. בעומס הצפוי (מאות מנויים) זה זניח; ראו LOAD-06 לגבי חוסר הידיעה על גודל ה-pool בפועל.

**LOAD-07 🟡 בינוני — N+1 בתוך `ReminderDispatcherService`: שאילתת מנוי בודדת + `SaveChangesAsync` בודד לכל שורה בבאץ'.**

בתוך `RunDispatchCycleAsync` (`ReminderDispatcherService.cs:83-107`), לאחר תפיסת עד `BatchSize=100` שורות (`RemindersOptions.cs:16`) בבת אחת, הלולאה `foreach (ReminderDelivery delivery in deliveries)` (שורה 89) מבצעת עבור **כל שורה בנפרד**:
1. `ProcessDeliveryAsync` → `dbContext.Subscribers.FirstOrDefaultAsync(s => s.Id == delivery.SubscriberId, ...)` (שורות 123-124) — שאילתת SELECT נפרדת למנוי, במקום `WHERE subscriber_id IN (...)` אחת מראש לכל ה-100 מזהים.
2. `await dbContext.SaveChangesAsync(cancellationToken)` (שורה 101) — הרצה נפרדת ל-DB לכל שורה, במקום `SaveChangesAsync` יחיד בסוף המחזור.

בעומס הנוכחי (מאות מנויים, מחזור כל 60 שניות) זה מתורגם ל**עד 200 round-trips נוספים ל-DB לכל מחזור דיספצ'ר** (100 SELECT + 100 SaveChanges) — לא קריטי היום (השהיה של מילישניות בודדות כל אחד, רץ ברקע ולא חוסם משתמש), אבל לא יתרחב בחן ל-thousands-scale, ורץ **כל 60 שניות לנצח**. Effort: M (bulk-load subscribers מראש לפי `IN`, ולבצע `SaveChangesAsync` אחד בסוף הלולאה).

**LOAD-10 🟢 נמוך — `ReminderPlannerService` מכניס שורה אחת בכל פעם, ברצף, לכל מנוי פעיל.**

`RunPlanningCycleAsync` (`ReminderPlannerService.cs:68-119`) טוענת את כל המנויים הפעילים בשאילתה אחת (שורות 73-76 — תקין), אבל אז ה-`foreach` (שורה 80) מבצע `ExecuteSqlInterpolatedAsync` **נפרד לכל מנוי בודד** (שורות 105-111) — round-trip DB אחד לכל מנוי, ברצף (לא מקבילי, לא batched). רץ פעם ביום בלבד — בקנה מידה של מאות מנויים זה עניין של פחות משנייה כוללת, לא דחוף. אם בסיס המנויים יגדל לעשרות אלפים, זה יהפוך לריצה של דקות שלמות (עדיין רק פעם ביום, לא חוסם משתמשים, אך שווה תיקון עתידי — bulk multi-row INSERT יחיד). Effort: M.

**קצב שליחה**: `Reminders:SendRatePerSecond` (ברירת מחדל 10, `RemindersOptions.cs:22`) מיושם ע"י `Task.Delay` בין שליחות מוצלחות (`ReminderDispatcherService.cs:103-106`) — מגן על שירות ה-SMS4FREE החיצוני מפני בקשות מהירות מדי, לא רלוונטי לעומס על השרת עצמו.

---

## 6. חוסן וכשל

**כשל DB זמני**: `EnableRetryOnFailure()` מוגדר על ה-DbContext (`Program.cs:42`) — כשלים חולפים בחיבור ל-PostgreSQL אמורים להיספג ברמת הדרייבר (Npgsql) ולא להפיל מיידית כל בקשה. אין קוד בריפו שבודק/מאמת את מדיניות ה-retry המדויקת (מספר ניסיונות/backoff) — זו ברירת המחדל של הספרייה, לא מוגדרת בקוד באופן מפורש (`npgsqlOptions.EnableRetryOnFailure()` נקרא בלי פרמטרים — `Program.cs:42`).

**LOAD-04 🟠 גבוה — `/health/ready` לא בודק חיבור ל-DB בכלל.**

`Backend/Tanakh.Api/Program.cs:167-168` רושם health check יחיד:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<TanakhDataHealthCheck>("tanakh-data", tags: new[] { "ready" });
```
ו-`app.MapHealthChecks("/health/ready", ...)` (`Program.cs:306-309`) בודק רק תגים `"ready"` — כלומר רק את `TanakhDataHealthCheck`, שבודק **קיום קבצים על הדיסק** (`Backend/Tanakh.Infrastructure/HealthChecks/TanakhDataHealthCheck.cs:20-22`), **לא חיבור ל-DB**. אין `AddDbContextCheck<AppDbContext>()`/`AddNpgSql(...)` בשום מקום בקוד (חיפוש גורף החזיר 0 תוצאות). המשמעות בפועל: אם ה-PostgreSQL היחיד שהאפליקציה תלויה בו נופל/לא נגיש (כולל תרחיש "Neon עדיין לא מסופק" שתועד ב-`docs/audit/04-database.md` סעיף 1 — פרויקט Neon בפועל **טרם קיים** לפי `.github/workflows/backend-backup.yml`), `/health/ready` **ימשיך להחזיר 200 תקין**, בעוד שכל בקשת משתמש אמיתית (הרשמה, שמירת התקדמות, פאנל ניהול, שליחת תזכורות) תיכשל בפועל עם 500. כל מנגנון ניטור/אורקסטרציה חיצוני שסומך על `/health/ready` (למשל להחלטה על restart/failover) יקבל "בריא" שקרי בדיוק כשהמערכת לא באמת עובדת. Effort: S — הוספת `.AddDbContextCheck<AppDbContext>()` עם תגית `"ready"`.

**LOAD-01 🔴 קריטי — אין עדות בריפו למנגנון החייאה (process supervisor) אחרי קריסה, על שרת יחיד ללא רשת ביטחון.**

`Backend/Dockerfile` (18 שורות, נקרא במלואו) מגדיר `ENTRYPOINT ["dotnet", "Tanakh.Api.dll"]` (שורה 18) — **ללא הוראת `HEALTHCHECK`**, וכמובן שקובץ Dockerfile לא יכול להגדיר בעצמו מדיניות `--restart` (זו תלויה ב-orchestrator/פלטפורמת האירוח שמריצה את הקונטיינר, לא בתוכן ה-Dockerfile). לפי `docs/audit/07-infra-and-deploy.md` סעיף 1, **אין עדיין יעד אירוח מאושר בפועל בריפו**: אין `render.yaml`, אין `fly.toml`, אין systemd unit, ו-`Backend/docker-compose.yml` מגדיר אך ורק שירות `postgres` מקומי לפיתוח (**לא** שירות ל-API עצמו, כך שאין בו `restart: unless-stopped` שחל על תהליך ה-API — יש כזה רק על שירות ה-postgres המקומי, `Backend/docker-compose.yml:5`, שלא רלוונטי לפרודקשן). כלומר: **לא נמצא בריפו שום מנגנון מאומת שהיה מחייה מחדש את תהליך ה-.NET אחרי קריסה** (חריגה לא-מטופלת מחוץ ל-pipeline של הבקשות, Out-Of-Memory, וכו').

על שרת יחיד (ההנחה המרכזית של מסמך זה) — קריסת התהליך = **נפילה מוחלטת של כל האתר** עד שמישהו ידני יבחין ויפעיל מחדש, ללא שום autoscaling/instance נוסף שיספוג את התנועה. זהו הממצא היחיד במסמך שמדורג 🔴 קריטי: לא בגלל שהוא ודאי יקרה בעומס הצפוי, אלא כי **אין שום ראיה בריפו שקיים תיקון לו כשהוא כן יקרה** — וזה בדיוק המאפיין הייחודי של שרת יחיד. Effort: תלוי בפלטפורמת האירוח הסופית שטרם נבחרה (S אם הפלטפורמה מספקת supervisor מובנה כמו Render/Fly, M אם צריך להגדיר ידנית `systemd` עם `Restart=always` או Docker עם `--restart=unless-stopped` על VPS גולמי).

**Graceful shutdown / דיפלוי**: **LOAD-11 🟢 נמוך** — לא נמצא override ל-`HostOptions.ShutdownTimeout` או שימוש ב-`IHostApplicationLifetime` בכל `Backend` (חיפוש גורף החזיר 0 תוצאות) — כלומר חל ברירת המחדל הסטנדרטית של ASP.NET Core (generic host) לכיבוי מסודר. לא אומת מהריפו לבד מהו הערך המדויק (זו ברירת מחדל של הפריימוורק, לא מוגדרת בקוד כאן) — בהינתן דיפלוי עתידי על שרת יחיד (restart = downtime מלא, אין מופע שני שסופג תנועה בזמן ה-restart), שווה לוודא בכוונה מהו חלון הזמן שניתן לבקשות פעילות לסיים לפני שהתהליך נהרג, ולתאם עם משך זמן ה-restart בפועל של הפלטפורמה שתיבחר. לא ממצא דחוף, מצוין לשקיפות.

**טיפול בחריגות**: `GlobalExceptionHandler` (`Backend/Tanakh.Api/GlobalExceptionHandler.cs`) תופס כל חריגה לא מטופלת, מחזיר `500` מסודר (`ProblemDetails`), ומנסה לכתוב שורת `ErrorLog` — אם הכתיבה ל-DB עצמה נכשלת (למשל כי ה-DB זה מה שנפל), הכשל **נבלע ונרשם ללוג בלבד** (`GlobalExceptionHandler.cs:71-74`) ולא גורם לתגובת השגיאה עצמה להיכשל בשרשרת. **תקין, מתוכנן היטב — אין ממצא.**

**מצב תחזוקה**: middleware ייעודי (`Program.cs:262-284`) מחזיר `503` מבוקר לכל הנתיבים חוץ מ-admin/system/health כאשר מצב תחזוקה מופעל — מנגנון כיבוי מבוקר קיים ותקין לצורך תחזוקה מתוכננת (בניגוד לקריסה בלתי-מתוכננת, שאין לה מענה כאמור ב-LOAD-01).

---

## 7. הערכת קיבולת

**אין מספיק נתונים למספר מדויק — הנה הנימוק וההשערה, וכן מה נדרש כדי לקבל תשובה מהימנה.**

הפרמטר הקובע ביותר, גודל ה-connection pool בפועל ל-PostgreSQL, **אינו ניתן לאימות מהריפו** (LOAD-06) — הוא נקבע ע"י מחרוזת חיבור שערכה `[REDACTED]` וממשתנה סביבה, ולא נמצא override מפורש בקוד. בלי המספר הזה, כל חישוב "מספר בקשות בו-זמניות מקסימלי לפני שה-pool נגמר" הוא ניחוש.

מה שכן ידוע וניתן לנמק ממנו:

1. **הצוואר-בקבוק הראשון לא יהיה ה-DB** — הוא CPU, בגלל LOAD-02. הפעולה הכי נפוצה באתר (`GET /Tanakh/books/{book}/{chapter}`) מבצעת היום עיבוד Regex + הקצאות על פני **כל** 929 הפרקים/23,206 הפסוקים בכל קריאה בודדת, ללא קאשינג של התוצאה וללא קאשינג HTTP (LOAD-05) שהיה יכול לחסום חלק ניכר מהתעבורה לפני שהיא מגיעה לשרת. בשרת יחיד (מספר ליבות מוגבל, לא נמדד/לא ידוע מהריפו) — זהו הראשון שיתחיל להשפיל את זמני התגובה תחת ריבוי בקשות מקבילות, ולא ה-DB.
2. **מכפיל הספייק מסוכן במיוחד כאן** — LOAD-02 (עלות CPU לא-מקוצרת) חסר לחלוטין הגנת rate limiting (ה-rate limiting היחיד במערכת מכסה admin login / OTP / signup — לא קריאת תוכן), ומצטרף אליו LOAD-08 (retry אוטומטי בפרונט שמכפיל x3 בדיוק כשמתחילים 5xx). כלומר בתרחיש "קישור משותף בקבוצת וואטסאפ גדולה" — הצפי הוא **לא** מפולת מסודרת אלא הסלמה: עומס → 5xx חלקיים → הפרונט מכפיל את אותה תנועה פי ~3 → מחריף את העומס. זהו התרחיש הכי מסוכן שזוהה במסמך.
3. **מה שנראה מתוכנן טוב יותר משוער**: שירות התזכורות (SKIP LOCKED, idempotency, reaper), אכיפת CORS מפורשת, rate limiting על OTP/הרשמה, retry-on-failure ב-DB, וטיפול DST נכון — כל אלו מפחיתים סיכון בצירים שלהם, אך לא פותרים את שני הסעיפים הקודמים.

**מה שנדרש כדי לתת מספר אמיתי (וזה בדיוק מה שסקריפטי ה-load-test הנפרדים שמוזכרים בתדריך אמורים לספק)**:
- זמן תגובה בפועל (p50/p95/p99) של `GET /Tanakh/books/{book}/{chapter}` תחת עומס מקביל הולך וגדל (10 / 50 / 200 / 500 בקשות/שנייה בו-זמנית), למדידת נקודת ההשפלה בפועל של LOAD-02.
- מספר הליבות/זיכרון בפועל של שרת הפרודקשן (לא תועד/לא ידוע מהריפו — תלוי בבחירת VPS/פלטפורמה שטרם סוכמה, `docs/audit/07-infra-and-deploy.md`).
- הערך בפועל של `Maximum Pool Size` במחרוזת החיבור האמיתית של פרודקשן (LOAD-06) — כרגע לא ידוע גם למי שיש גישה לריפו בלבד.
- זמן תגובה בפועל של hebcal.com בשעות עומס, כדי לכייל טוב יותר את ה-timeout המומלץ ל-LOAD-03.

**מסקנה**: בלי המדידות האלה, "כמה משתמשים בו-זמנית האתר יעמוד בפניהם" הוא לא ניחוש שראוי לכתוב כמספר — אבל **מה שישבר ראשון**, בסבירות גבוהה, הוא **לא** ה-DB (שנראה מתוכנן בסדר טוב יחסית לעומס הזה) אלא **חישוב ה-CPU החוזר-על-עצמו של `TanakhTextService` בשילוב היעדר קאשינג HTTP והיעדר rate limiting על נתיבי הקריאה** (LOAD-02 + LOAD-05), ומוחרף ע"י ההכפלה האוטומטית של תנועה כושלת מהפרונט (LOAD-08) בדיוק ברגע שהעומס הזה מתחיל להזיק.
