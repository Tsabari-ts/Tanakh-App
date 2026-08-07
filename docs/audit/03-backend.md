# מפת Backend — Tanakh

מסמך זה ממפה את שכבת ה-Backend של הפרויקט (`Backend/`) על בסיס קריאת קוד בלבד. כל טענה מלווה בציון קובץ ושורה מדויקים ביחס לשורש המאגר. המסמך אינו כולל ניתוח מלא של סכימת מסד הנתונים (מסמך נפרד) ואינו כולל המלצות — מיפוי עובדתי בלבד.

---

## 1. שפה, פריימוורק, גרסה ואתחול השרת

- הפרויקט כתוב ב-C# על גבי **.NET 10** (`TargetFramework=net10.0`), כ-ASP.NET Core Web API (SDK מסוג `Microsoft.NET.Sdk.Web`) — `Backend/Tanakh.Api/Tanakh.Api.csproj:1,4`.
- בכל ארבעת הפרויקטים (`Tanakh.Api`, `Tanakh.Domain`, `Tanakh.Infrastructure`, `Tanakh.Tests`) מוגדר `ImplicitUsings=enable` ו-`Nullable=enable`; ב-`Tanakh.Api`, `Tanakh.Domain` ו-`Tanakh.Infrastructure` מוגדר גם `TreatWarningsAsErrors=true` — `Backend/Tanakh.Api/Tanakh.Api.csproj:5,9,10`, `Backend/Tanakh.Domain/Tanakh.Domain.csproj:5,6,7`, `Backend/Tanakh.Infrastructure/Tanakh.Infrastructure.csproj:25,26,27`.
- נקודת הכניסה היא `Backend/Tanakh.Api/Program.cs` — קובץ בסגנון top-level statements, עם `var builder = WebApplication.CreateBuilder(args);` בשורה `Backend/Tanakh.Api/Program.cs:33` ו-`public partial class Program;` בסוף הקובץ (`Backend/Tanakh.Api/Program.cs:313`) לצורך שימוש ב-`Program` כטיפוס בבדיקות (`Backend/Tanakh.Tests`).
- **כתובות/פורטים בזמן פיתוח** מוגדרים ב-`Backend/Tanakh.Api/Properties/launchSettings.json`:
  - פרופיל `IIS Express`: `applicationUrl: http://localhost:6522`, `sslPort: 44308` — `Backend/Tanakh.Api/Properties/launchSettings.json:7-8`.
  - פרופיל `Tanakh` (הרצה ישירה עם `dotnet run`): `applicationUrl: https://localhost:5001;http://localhost:5000` — `Backend/Tanakh.Api/Properties/launchSettings.json:25`.
  - שני הפרופילים קובעים `ASPNETCORE_ENVIRONMENT=Development` — `Backend/Tanakh.Api/Properties/launchSettings.json:17,27`.
  - **לא נמצא** קוד ב-`Program.cs` שקורא ל-`app.Run("...")` עם כתובת מפורשת או ל-`UseUrls`/`UseKestrel` — כלומר מחוץ לסביבת הפיתוח (למשל ב-production) הכתובת שאליה הענן מאזין נקבעת אך ורק על-ידי משתנה הסביבה הסטנדרטי של ASP.NET Core (`ASPNETCORE_URLS`) או ברירת המחדל של Kestrel, לא על-ידי קוד במאגר זה.
  - `Backend/Tanakh.Api/appsettings.json` הוא קובץ הבסיס (מכיל רק `Logging` ו-`AllowedHosts`) — `Backend/Tanakh.Api/appsettings.json:1-10`; `Backend/Tanakh.Api/appsettings.Development.json` מוסיף/דורס `ConnectionStrings:AppDb`, `Reminders:PublicBaseUrl`/`ApiBaseUrl` ו-`Cors:AllowedOrigins` לסביבת פיתוח — `Backend/Tanakh.Api/appsettings.Development.json:9-18`.
- שני דגלי שורת-פקודה מטופלים ב-`Program.cs` **לפני** בניית ה-pipeline הרגיל:
  - `--hash-admin-password <pw>` — כלי עזר חד-פעמי המדפיס גיבוב סיסמת מנהל, פועל בכל סביבה — `Backend/Tanakh.Api/Program.cs:186-197`.
  - `--seed` / `--reset-db` — זריעת/איפוס נתוני פיתוח, חסום מחוץ ל-`Development` — `Backend/Tanakh.Api/Program.cs:199-221`.

---

## 2. טבלת תלויות (NuGet PackageReference)

### 2.1 Tanakh.Api — `Backend/Tanakh.Api/Tanakh.Api.csproj`

| חבילה | גרסה | פרויקט | שימוש בפועל בקוד |
|---|---|---|---|
| Microsoft.AspNetCore.OpenApi | 10.0.10 | Tanakh.Api | `builder.Services.AddOpenApi();` — `Backend/Tanakh.Api/Program.cs:170`; `app.MapOpenApi();` (Development בלבד) — `Backend/Tanakh.Api/Program.cs:226` |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 | Tanakh.Api | חבילת כלי-עיצוב (`PrivateAssets=all`) המפעילה את `dotnet ef` (migrations); אין `using` ישיר בקוד — `Backend/Tanakh.Api/Tanakh.Api.csproj:20-23` |
| Microsoft.OpenApi | 2.11.0 | Tanakh.Api | תלות תומכת (transitively) לצינור יצירת מסמך ה-OpenAPI של `AddOpenApi`/Scalar; לא נמצא `using Microsoft.OpenApi` ישיר בקוד — `Backend/Tanakh.Api/Tanakh.Api.csproj:24` |
| Microsoft.VisualStudio.Threading.Analyzers | 18.7.23 | Tanakh.Api | אנלייזר בלבד (`PrivateAssets=all`), אוכף כללי VSTHRD (async/await); מוזכר בהערת קוד — `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:147-150` |
| Scalar.AspNetCore | 2.16.16 | Tanakh.Api | `using Scalar.AspNetCore;` ו-`app.MapScalarApiReference();` (ממשק תיעוד API בסביבת Development) — `Backend/Tanakh.Api/Program.cs:11,227` |

### 2.2 Tanakh.Domain — `Backend/Tanakh.Domain/Tanakh.Domain.csproj`

| חבילה | גרסה | פרויקט | שימוש בפועל בקוד |
|---|---|---|---|
| Microsoft.VisualStudio.Threading.Analyzers | 18.7.23 | Tanakh.Domain | אנלייזר בלבד (`PrivateAssets=all`), ללא שימוש ב-`using` — `Backend/Tanakh.Domain/Tanakh.Domain.csproj:11-14` |

פרויקט `Tanakh.Domain` מכיל רק interfaces/entities/לוגיקה טהורה ואינו תלוי ב-ASP.NET Core או ב-EF Core (נאכף ע"י `Backend/Tanakh.Tests/ArchitectureTests.cs:12-25`, ראו סעיף קבצים).

### 2.3 Tanakh.Infrastructure — `Backend/Tanakh.Infrastructure/Tanakh.Infrastructure.csproj`

| חבילה | גרסה | פרויקט | שימוש בפועל בקוד |
|---|---|---|---|
| EFCore.NamingConventions | 10.0.1 | Tanakh.Infrastructure | `options.UseSnakeCaseNamingConvention();` — `Backend/Tanakh.Api/Program.cs:45`, `Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:22`, `Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:33` |
| Microsoft.EntityFrameworkCore.Relational | 10.0.10 | Tanakh.Infrastructure | `Migration`/`MigrationBuilder` בכל קבצי המיגרציה, למשל `Backend/Tanakh.Infrastructure/Migrations/20260730141256_InitialSubscribers.cs:2,9,12` |
| Microsoft.Extensions.Caching.Memory | 10.0.10 | Tanakh.Infrastructure | `using Microsoft.Extensions.Caching.Memory;` ב-`Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:1`, וכן ב-`Backend/Tanakh.Infrastructure/Services/AppSettingsService.cs:2`, `Backend/Tanakh.Infrastructure/Services/SmsBalanceService.cs:1` |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.10 | Tanakh.Infrastructure | `IConfiguration` מוזרק ל-`DatabaseSeeder` — `Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:4,18,20` |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 10.0.10 | Tanakh.Infrastructure | `IHealthCheck`/`HealthCheckResult` — `Backend/Tanakh.Infrastructure/HealthChecks/TanakhDataHealthCheck.cs:1,7,16` |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.10 | Tanakh.Infrastructure | `BackgroundService` — `Backend/Tanakh.Infrastructure/Retention/RetentionHostedService.cs:23`, `Backend/Tanakh.Infrastructure/Reminders/ReminderPlannerService.cs:24`, `Backend/Tanakh.Infrastructure/Reminders/ReminderDispatcherService.cs:27`; `IHostEnvironment` — `Backend/Tanakh.Infrastructure/CacheProvider.cs:21` |
| Microsoft.Extensions.Logging.Abstractions | 10.0.10 | Tanakh.Infrastructure | `ILogger<T>` בעשרות מחלקות, למשל `Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:2,18` |
| Microsoft.Extensions.Options | 10.0.10 | Tanakh.Infrastructure | `IOptions<T>` בעשרות מחלקות, למשל `Backend/Tanakh.Infrastructure/Services/HashingService.cs:1,14` |
| Microsoft.VisualStudio.Threading.Analyzers | 18.7.23 | Tanakh.Infrastructure | אנלייזר בלבד — `Backend/Tanakh.Infrastructure/Tanakh.Infrastructure.csproj:16-19` |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | Tanakh.Infrastructure | `options.UseNpgsql(connectionString, ...)` — `Backend/Tanakh.Api/Program.cs:40`, `Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:21` |

### 2.4 Tanakh.Tests — `Backend/Tanakh.Tests/Tanakh.Tests.csproj`

| חבילה | גרסה | פרויקט | שימוש בפועל בקוד |
|---|---|---|---|
| coverlet.collector | 6.0.4 | Tanakh.Tests | אספן כיסוי-קוד להרצת `dotnet test`; ללא `using` ישיר בקוד — `Backend/Tanakh.Tests/Tanakh.Tests.csproj:11` |
| Microsoft.NET.Test.Sdk | 17.14.1 | Tanakh.Tests | SDK הרצת בדיקות (מאפשר `dotnet test`); ללא `using` ישיר — `Backend/Tanakh.Tests/Tanakh.Tests.csproj:12` |
| NetArchTest.Rules | 1.3.2 | Tanakh.Tests | `using NetArchTest.Rules;` ו-`Types.InAssembly(...).ShouldNot().HaveDependencyOnAny(...)` — `Backend/Tanakh.Tests/ArchitectureTests.cs:3,15-22` |
| xunit | 2.9.3 | Tanakh.Tests | תשתית הבדיקות (`[Fact]`, `Assert`), מיובאת גלובלית דרך `<Using Include="Xunit" />` — `Backend/Tanakh.Tests/Tanakh.Tests.csproj:19`; שימוש לדוגמה `Backend/Tanakh.Tests/ArchitectureTests.cs:12,24` |
| xunit.runner.visualstudio | 3.1.4 | Tanakh.Tests | מתאם הרצה ל-Visual Studio/`dotnet test`; ללא `using` ישיר — `Backend/Tanakh.Tests/Tanakh.Tests.csproj:15` |

הפניות בין-פרויקטיות (`ProjectReference`): `Tanakh.Api` → `Tanakh.Domain`, `Tanakh.Infrastructure` (`Backend/Tanakh.Api/Tanakh.Api.csproj:14-15`); `Tanakh.Infrastructure` → `Tanakh.Domain` (`Backend/Tanakh.Infrastructure/Tanakh.Infrastructure.csproj:4`); `Tanakh.Tests` → כל שלושת הפרויקטים (`Backend/Tanakh.Tests/Tanakh.Tests.csproj:22-26`).

---

## 3. מפת קבצים

### 3.1 `Tanakh.Api/Controllers` (13 קבצים)

כל 13 ה-controllers מתועדים בטבלת ה-endpoints בסעיף 4. תיאור כללי: כל controller אחראי על תחום פונקציונלי אחד — אימות מנהל (`AdminAuthController`), פעולות ידניות (`AdminController`), ייצוא CSV (`AdminExportController`), יומן שגיאות (`AdminLogsController`), SMS (`AdminSmsController`), סטטיסטיקות (`AdminStatsController`), הגדרות מערכת/דגלי-פיצ'ר/תחזוקה (`AdminSystemController`), ניהול משתמשים (`AdminUsersController`), לוח שנה עברי (`JewishCalendarController`), התקדמות קריאה (`ReadingProgressController`), הרשמה/ניהול מנוי (`SubscriptionsController`), מצב מערכת ציבורי (`SystemController`) וטקסט/מבנה התנ"ך (`TanakhController`).

### 3.2 `Tanakh.Api/Auth`

| קובץ | תיאור |
|---|---|
| `Backend/Tanakh.Api/Auth/AdminCookieAuthDefaults.cs` | מחלקה סטטית עם קבוע יחיד `SchemeName = "AdminCookie"` — שם ה-scheme של האימות מבוסס-העוגייה למנהל (`Backend/Tanakh.Api/Auth/AdminCookieAuthDefaults.cs:5`). |

### 3.3 `Tanakh.Api/Services`

| קובץ | תיאור |
|---|---|
| `Backend/Tanakh.Api/Services/ITanakhTextService.cs` | ממשק לשליפת טקסט פרק בודד (`GetChapterAsync`) — `Backend/Tanakh.Api/Services/ITanakhTextService.cs:9`. |
| `Backend/Tanakh.Api/Services/TanakhTextService.cs` | מימוש: בונה מילון פרקים מתוך קובץ הנתונים המלא של התנ"ך (דרך `CacheProvider`), מסיר תגי HTML מהפסוקים ומחשב קישורי "פרק הבא/קודם" — `Backend/Tanakh.Api/Services/TanakhTextService.cs:13,24,41`. |

### 3.4 `Tanakh.Api/Model` (16 קבצים — DTOs של בקשה/תגובה)

| קובץ | תיאור |
|---|---|
| `TanakhContext.cs` | מכיל `TanakhContext` (תגובת פרק) ו-`Book` (מטא-דאטה + פסוקי הפרק) — `Backend/Tanakh.Api/Model/TanakhContext.cs:3,9`. |
| `ReadingProgressRequest.cs` | בקשת עדכון התקדמות קריאה: `Token`, `Book`, `Chapter` — `Backend/Tanakh.Api/Model/ReadingProgressRequest.cs:3`. |
| `SubscriptionResponse.cs` | תגובת הרשמה: `ManageToken` שהלקוח שומר לצורך פעולות עתידיות ללא התחברות — `Backend/Tanakh.Api/Model/SubscriptionResponse.cs:7`. |
| `UpdatePreferencesRequest.cs` | עדכון העדפות מנוי: `Token`, `PreferredTime`, `SkipShabbatHolidays`, `Action` ("pause"/"resume") — `Backend/Tanakh.Api/Model/UpdatePreferencesRequest.cs:3`. |
| `ManageTokenRequest.cs` | עטיפה פשוטה של `Token` בלבד, לביטול הרשמה — `Backend/Tanakh.Api/Model/ManageTokenRequest.cs:3`. |
| `AdminLoginRequest.cs` | `Username`, `Password` להתחברות מנהל — `Backend/Tanakh.Api/Model/AdminLoginRequest.cs:3`. |
| `AdminRequeueRequest.cs` | `DeliveryId` לתזמון מחדש של משלוח שנכשל — `Backend/Tanakh.Api/Model/AdminRequeueRequest.cs:5`. |
| `AdminUnsubscribeRequest.cs` | `PhoneNumber` לביטול הרשמה ידני ע"י מנהל — `Backend/Tanakh.Api/Model/AdminUnsubscribeRequest.cs:3`. |
| `AdminVerifyOtpRequest.cs` | `Code` — קוד האימות בכניסת מנהל — `Backend/Tanakh.Api/Model/AdminVerifyOtpRequest.cs:3`. |
| `AdminUserActionRequest.cs` | `Action` ("block"/"unblock") לפעולת מנהל על משתמש — `Backend/Tanakh.Api/Model/AdminUserActionRequest.cs:3`. |
| `AdminCleanupLogsRequest.cs` | `OlderThanDays` לניקוי יומן שגיאות ישן — `Backend/Tanakh.Api/Model/AdminCleanupLogsRequest.cs:3`. |
| `AdminSetMaintenanceRequest.cs` | `Enabled`, `Message` להפעלת/כיבוי מצב תחזוקה — `Backend/Tanakh.Api/Model/AdminSetMaintenanceRequest.cs:3`. |
| `AdminSetBannerRequest.cs` | `Text`, `ExpiresAt` לבאנר הודעה ציבורי — `Backend/Tanakh.Api/Model/AdminSetBannerRequest.cs:5`. |
| `AdminSetFeatureFlagRequest.cs` | `Enabled` להגדרת דגל-פיצ'ר — `Backend/Tanakh.Api/Model/AdminSetFeatureFlagRequest.cs:3`. |
| `SubscriptionRequest.cs` | בקשת הרשמה מלאה: מספר טלפון, שם, שעה מועדפת, אזור זמן, דילוג שבת/חג, הסכמה, קוד OTP, גרסאות תנאים/פרטיות וטקסט ההסכמה — `Backend/Tanakh.Api/Model/SubscriptionRequest.cs:3`. |
| `RequestOtpRequest.cs` | `PhoneNumber` לבקשת שליחת קוד אימות — `Backend/Tanakh.Api/Model/RequestOtpRequest.cs:3`. |

### 3.5 קבצים נוספים ב-`Tanakh.Api` (מחוץ לתיקיות שהתבקשו, לשלמות)

| קובץ | תיאור |
|---|---|
| `Backend/Tanakh.Api/Program.cs` | נקודת הכניסה — הרשמת שירותים, אימות/הרשאה, rate limiting, middleware pipeline, health checks. |
| `Backend/Tanakh.Api/GlobalExceptionHandler.cs` | מטפל חריגות גלובלי — ראו סעיף 8. |
| `Backend/Tanakh.Api/RateLimiterPolicyNames.cs` | קבועי שמות מדיניות ה-rate limiting (`AdminLogin`, `SubscriptionOtpRequest`, `SubscriptionCreate`) — `Backend/Tanakh.Api/RateLimiterPolicyNames.cs:5-7`. |
| `Backend/Tanakh.Api/Csv/CsvWriter.cs` | כותב CSV מינימלי (RFC 4180-ish) עם escaping לפסיקים/מרכאות/שורות חדשות, לשימוש `AdminExportController` — `Backend/Tanakh.Api/Csv/CsvWriter.cs:10,27`. |
| `Backend/Tanakh.Api/Data/TanakhData.json`, `TanakhStructure.json` | קבצי נתוני התנ"ך הסטטיים (טקסט ומבנה) הנטענים בזמן ריצה ע"י `CacheProvider`. |

### 3.6 `Tanakh.Domain` — שורש (interfaces כלליים, ללא תיקיית משנה)

| קובץ | תיאור |
|---|---|
| `IReadingProgressService.cs` | חוזה לשמירת/שליפת התקדמות קריאה לפי מנוי — `Backend/Tanakh.Domain/IReadingProgressService.cs:9`. |
| `ISubscriberAnonymizationService.cs` | חוזה לאנונימיזציה של מנוי (מחיקה "רכה") — `Backend/Tanakh.Domain/ISubscriberAnonymizationService.cs:7`. |
| `IDatabaseSeeder.cs` | חוזה לאיפוס סכימה וזריעת נתוני פיתוח — `Backend/Tanakh.Domain/IDatabaseSeeder.cs:6`. |
| `IUnsubscribeTokenService.cs` | חוזה להנפקה/אימות של טוקן HMAC חתום ללא תפוגה לזיהוי מנוי — `Backend/Tanakh.Domain/IUnsubscribeTokenService.cs:9`. |
| `INextChapterResolver.cs` | חוזה לחישוב הפרק הבא שיש לשלוח למנוי בתזכורת — `Backend/Tanakh.Domain/INextChapterResolver.cs:12`. |
| `IHashingService.cs` | חוזה לגיבוב HMAC-מלוח (pepper) לערכים הדורשים חיפוש ללא שמירת טקסט גלוי — `Backend/Tanakh.Domain/IHashingService.cs:5`. |
| `IAdminPasswordHasher.cs` | חוזה לגיבוב/אימות סיסמת המנהל (PBKDF2) — `Backend/Tanakh.Domain/IAdminPasswordHasher.cs:6`. |
| `ISmsSender.cs` | חוזה לשליחת SMS + טיפוס `SmsSendResult` — `Backend/Tanakh.Domain/ISmsSender.cs:11,18`. |
| `ISmsBalanceService.cs` | חוזה לבדיקת יתרת SMS4FREE — `Backend/Tanakh.Domain/ISmsBalanceService.cs:8`. |
| `IAdminService.cs` | חוזה מרכזי ללוח הבקרה של המנהל — dashboards, KPI, ניהול משתמשים/SMS/שגיאות — `Backend/Tanakh.Domain/IAdminService.cs:70`. |
| `IAppSettingsService.cs` | חוזה להגדרות מפתח-ערך גלובליות (מצב תחזוקה, באנר) — `Backend/Tanakh.Domain/IAppSettingsService.cs:16`. |
| `OtpVerificationResult.cs` | Enum: `Valid`/`Invalid`/`Locked` — `Backend/Tanakh.Domain/OtpVerificationResult.cs:3`. |
| `ISubscriptionService.cs` | חוזה מרכזי להרשמה/OTP/ביטול/עדכון העדפות מנוי — `Backend/Tanakh.Domain/ISubscriptionService.cs:14`. |

### 3.7 `Tanakh.Domain/Auditing`

| קובץ | תיאור |
|---|---|
| `IHasCreatedAt.cs` | ממשק לישויות עם עמודת `created_at` — `Backend/Tanakh.Domain/Auditing/IHasCreatedAt.cs:8`. |
| `IHasUpdatedAt.cs` | ממשק לישויות עם עמודת `updated_at`, מוחתם אוטומטית ב-`AppDbContext.SaveChangesAsync` — `Backend/Tanakh.Domain/Auditing/IHasUpdatedAt.cs:7`. |

### 3.8 `Tanakh.Domain/Caching`

| קובץ | תיאור |
|---|---|
| `ITanakhCache.cs` | חוזה מטמון גנרי (`TryGet`/`Set`) המנותק ממימוש קונקרטי — `Backend/Tanakh.Domain/Caching/ITanakhCache.cs:5`. |

### 3.9 `Tanakh.Domain/Entities` (18 קבצים)

| קובץ | תיאור |
|---|---|
| `Subscriber.cs` | ישות המנוי — טלפון (E.164, nullable לאחר אנונימיזציה), שם, שעה/אזור-זמן מועדפים, סטטוס, השהיה — `Backend/Tanakh.Domain/Entities/Subscriber.cs:6`. |
| `SubscriberStatus.cs` | Enum `Active`/`Unsubscribed` — `Backend/Tanakh.Domain/Entities/SubscriberStatus.cs:11`. |
| `ReadingProgress.cs` | מיקום קריאה נוכחי לכל (מנוי, חטיבה) — `Backend/Tanakh.Domain/Entities/ReadingProgress.cs:9`. |
| `ReadingSection.cs` | Enum `Torah`/`Neviim`/`Ketuvim` — `Backend/Tanakh.Domain/Entities/ReadingSection.cs:3`. |
| `ReadingSectionMapper.cs` | ממפה שמות חטיבה מקובץ המבנה ("Torah"/"Prophets"/"Writings") ל-`ReadingSection` — `Backend/Tanakh.Domain/Entities/ReadingSectionMapper.cs:8`. |
| `ReminderDelivery.cs` | רשומת משלוח תזכורת בודד (תזמון, ניסיונות, סטטוס, תוכן שנשלח, `IdempotencyKey`) — `Backend/Tanakh.Domain/Entities/ReminderDelivery.cs:11`. |
| `DeliveryStatus.cs` | Enum `Pending`/`Sending`/`Sent`/`Failed`/`Skipped` — `Backend/Tanakh.Domain/Entities/DeliveryStatus.cs:3`. |
| `OtpCode.cs` | קוד OTP לזהות-מנהל יחידה (כניסה למנהל) — `Backend/Tanakh.Domain/Entities/OtpCode.cs:9`. |
| `SubscriberOtpCode.cs` | קוד OTP לפי מספר טלפון (הרשמת מנוי ציבורי) — `Backend/Tanakh.Domain/Entities/SubscriberOtpCode.cs:10`. |
| `SmsMessageType.cs` | Enum `Reminder`/`Otp`/`Test` — `Backend/Tanakh.Domain/Entities/SmsMessageType.cs:3`. |
| `SmsLog.cs` | יומן שטוח של כל שליחת SMS (ללא תלות בסוג הקורא) — `Backend/Tanakh.Domain/Entities/SmsLog.cs:12`. |
| `ErrorLevel.cs` | Enum `Info`/`Warn`/`Error`/`Fatal` — `Backend/Tanakh.Domain/Entities/ErrorLevel.cs:3`. |
| `ErrorLog.cs` | יומן שגיאות בלתי-מטופלות, נכתב אוטומטית ע"י `GlobalExceptionHandler` — `Backend/Tanakh.Domain/Entities/ErrorLog.cs:10`. |
| `AppSetting.cs` | זוג מפתח/ערך (JSON) להגדרות singleton כמו `maintenance`/`banner` — `Backend/Tanakh.Domain/Entities/AppSetting.cs:10`. |
| `FeatureFlag.cs` | דגל-פיצ'ר בעל שם, ניתן להוספה/הסרה חופשית — `Backend/Tanakh.Domain/Entities/FeatureFlag.cs:9`. |
| `ConsentRecord.cs` | רשומת הסכמה (append-only) לפי תיקון 13 לחוק הגנת הפרטיות — `Backend/Tanakh.Domain/Entities/ConsentRecord.cs:11`. |
| `ConsentType.cs` | Enum `Marketing`/`Analytics`/`Functional` — `Backend/Tanakh.Domain/Entities/ConsentType.cs:3`. |
| `AuditLogEntry.cs` | רשומת ביקורת (append-only) לפעולות רגישות — `Backend/Tanakh.Domain/Entities/AuditLogEntry.cs:11`. |

### 3.10 `Tanakh.Domain/Scheduling`

| קובץ | תיאור |
|---|---|
| `LocalTimeResolver.cs` | ממיר שעה מקומית (עם טיפול במעברי שעון קיץ/חורף) ל-`DateTimeOffset` ב-UTC — `Backend/Tanakh.Domain/Scheduling/LocalTimeResolver.cs:9`. |
| `NextOccurrenceResolver.cs` | מחשב את המופע הבא (UTC) של שעה מקומית יומית קבועה — `Backend/Tanakh.Domain/Scheduling/NextOccurrenceResolver.cs:9`. |

### 3.11 `Tanakh.Domain/Sms`

| קובץ | תיאור |
|---|---|
| `SmsSegmentCalculator.cs` | מחשב מספר מקטעי SMS (GSM-7 מול UCS-2) לפי תוכן ההודעה — `Backend/Tanakh.Domain/Sms/SmsSegmentCalculator.cs:11,19`. |

### 3.12 `Tanakh.Domain/Validation`

| קובץ | תיאור |
|---|---|
| `TimeZoneValidator.cs` | בודק שמחרוזת אזור-זמן היא מזהה IANA תקין — `Backend/Tanakh.Domain/Validation/TimeZoneValidator.cs:9`. |
| `IsraeliMobilePhoneValidator.cs` | מקור-אמת יחיד לתקינות מספר נייד ישראלי — נרמול, בדיקת קווי-קרקע, המרה ל-E.164 ומיסוך ליומן — `Backend/Tanakh.Domain/Validation/IsraeliMobilePhoneValidator.cs:11,38,75`. |

### 3.13 `Tanakh.Infrastructure/Caching`

| קובץ | תיאור |
|---|---|
| `MemoryTanakhCache.cs` | מימוש `ITanakhCache` מעל `IMemoryCache` עם תפוגה של 12 שעות — `Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:9,15`. |

### 3.14 `Tanakh.Infrastructure/Data`

| קובץ | תיאור |
|---|---|
| `AppDbContext.cs` | ה-`DbContext` הראשי (pooled) — מכיל את כל ה-`DbSet`ים ומחתים אוטומטית `CreatedAt`/`UpdatedAt` ב-`SaveChanges(Async)` — `Backend/Tanakh.Infrastructure/Data/AppDbContext.cs:11,20-40,47-57`. |
| `AppDbContextFactory.cs` | `IDesignTimeDbContextFactory` לצורך `dotnet ef` — משתמש בחיבור נפרד ובעל הרשאות גבוהות יותר (`ConnectionStrings__MigrationsDb`) — `Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:12,16`. |
| `Configurations/*.cs` (11 קבצים) | `IEntityTypeConfiguration<T>` לכל ישות — ממפים שם טבלה, אילוצי CHECK, אינדקסים ייחודיים והמרות ערך. שמות הטבלאות: `subscribers`, `reading_progress`, `reminder_deliveries`, `consent_records`, `audit_log`, `otp_codes`, `subscriber_otp_codes`, `sms_log`, `error_log`, `app_settings`, `feature_flags` — למשל `Backend/Tanakh.Infrastructure/Data/Configurations/SubscriberConfiguration.cs:12-27,42-43`. תיעוד סכימה מפורט נמצא במסמך נפרד. |
| `Conversions/SnakeCaseEnumConverter.cs` | ממיר generic הממפה ערכי enum ל-snake_case בטקסט (לשימוש עם אילוצי CHECK) — `Backend/Tanakh.Infrastructure/Data/Conversions/SnakeCaseEnumConverter.cs:14`. |

### 3.15 `Tanakh.Infrastructure/HealthChecks`

| קובץ | תיאור |
|---|---|
| `TanakhDataHealthCheck.cs` | בודק שקבצי `TanakhData.json`/`TanakhStructure.json` קיימים על הדיסק, ללא בדיקת DB — `Backend/Tanakh.Infrastructure/HealthChecks/TanakhDataHealthCheck.cs:7,20-22`. |

### 3.16 `Tanakh.Infrastructure/Migrations`

התיקייה קיימת ומכילה 14 מיגרציות EF Core (28 קבצים: `.cs` + `.Designer.cs` לכל אחת) בטווח תאריכים `20260730141256` עד `20260806185702`, בתוספת `AppDbContextModelSnapshot.cs`. תיעוד סכימת מסד הנתונים המלא נמצא במסמך נפרד; כאן מצוינת רק קיום התיקייה כנדרש.

### 3.17 `Tanakh.Infrastructure/Model`

| קובץ | תיאור |
|---|---|
| `TanakhContainer.cs` | טיפוסי deserialization התואמים את מבנה תגובת Sefaria (`/api/texts/{ref}`) — כל שדה nullable — `Backend/Tanakh.Infrastructure/Model/TanakhContainer.cs:8,20`. |
| `TanakhStructure.cs` | טיפוסי deserialization למבנה ספרי התנ"ך (חטיבה/כותרת/פרקים) — `Backend/Tanakh.Infrastructure/Model/TanakhStructure.cs:9,14`. |
| `JewishCalendarContainer.cs` | טיפוסי deserialization התואמים את תגובת hebcal.com — `Backend/Tanakh.Infrastructure/Model/JewishCalendarContainer.cs:11`. |

### 3.18 `Tanakh.Infrastructure/Options` (6 קבצים)

| קובץ | תיאור |
|---|---|
| `TanakhDataOptions.cs` | `TanakhData:DataDirectory` — נתיב חלופי לקבצי נתוני התנ"ך — `Backend/Tanakh.Infrastructure/Options/TanakhDataOptions.cs:3,9`. |
| `RemindersOptions.cs` | הגדרות מתזמן/שולח התזכורות (cron, קצב שליחה, תבנית SMS וכו') — `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:5`. |
| `RetentionOptions.cs` | הגדרות מדיניות שימור/מחיקת נתונים (תיקון 13) — `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:8`. |
| `HashingOptions.cs` | `Hashing:Pepper` — מפתח HMAC משותף לגיבוב ולחתימת טוקנים — `Backend/Tanakh.Infrastructure/Options/HashingOptions.cs:3,11`. |
| `AdminOptions.cs` | פרטי חשבון המנהל היחיד (`Username`, `PasswordHash`, `Phone`, `LowBalanceThreshold`) — `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:3`. |
| `SmsOptions.cs` | פרטי חיבור ל-SMS4FREE (`Key`/`User`/`Pass`/`Sender`), כתובות API ומצב `DryRun` — `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:3`. |

### 3.19 `Tanakh.Infrastructure/Reminders`

התיקייה קיימת ומכילה 2 קבצים; תיעוד מפורט נמצא במסמך שירותים/אינטגרציות נפרד. רשימת קבצים בלבד:
- `Backend/Tanakh.Infrastructure/Reminders/ReminderPlannerService.cs`
- `Backend/Tanakh.Infrastructure/Reminders/ReminderDispatcherService.cs`

### 3.20 `Tanakh.Infrastructure/Retention`

| קובץ | תיאור |
|---|---|
| `RetentionHostedService.cs` | `BackgroundService` הרץ במרווח `RetentionOptions.RunInterval` — מוחק באצוות `reminder_deliveries` ישנים ומאנונם מנויים מבוטלים שחלף זמן השימור שלהם — `Backend/Tanakh.Infrastructure/Retention/RetentionHostedService.cs:23,39,57`. |

### 3.21 `Tanakh.Infrastructure/Seeding`

| קובץ | תיאור |
|---|---|
| `DatabaseSeeder.cs` | מממש `IDatabaseSeeder` — `ResetSchemaAsync` (Down עד "0" ואז Up מלא) ו-`SeedAsync` (זריעת 3 מנויים ורשומות התקדמות/משלוח לדוגמה, אידמפוטנטי) — `Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:15,26,44`. |

### 3.22 `Tanakh.Infrastructure/Services` (15 קבצים)

| קובץ | תיאור |
|---|---|
| `ITanakhStructureService.cs` / `TanakhStructureService.cs` | שליפת מבנה ספרי התנ"ך (הכל/לפי חטיבה/לפי כותרת) מתוך `CacheProvider` — `Backend/Tanakh.Infrastructure/Services/TanakhStructureService.cs:9`. |
| `ReadingProgressService.cs` | מימוש `IReadingProgressService` מול `AppDbContext` (upsert לפי מנוי+חטיבה) — `Backend/Tanakh.Infrastructure/Services/ReadingProgressService.cs:13`. |
| `UnsubscribeTokenService.cs` | מימוש `IUnsubscribeTokenService` — טוקן HMAC-SHA256 חתום, Base64Url, עם מזהה-מפתח לרוטציה עתידית — `Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:10,24,33`. |
| `NextChapterResolver.cs` | מימוש `INextChapterResolver` — קובע את הפרק הבא לשליחה לפי התקדמות קיימת, ברירת מחדל, או מעבר ספר/מחזור — `Backend/Tanakh.Infrastructure/Services/NextChapterResolver.cs:14,30`. |
| `IJewishCalendarService.cs` / `JewishCalendarService.cs` | קובע האם רגע נתון חל בין הדלקת נרות להבדלה, מול hebcal.com — `Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:11,18,53-58`. |
| `HashingService.cs` | מימוש `IHashingService` — HMAC-SHA256 עם `Hashing:Pepper` — `Backend/Tanakh.Infrastructure/Services/HashingService.cs:10,19`. |
| `SubscriberAnonymizationService.cs` | מנקה `PhoneNumber`/`DisplayName` (ל-NULL) מבלי למחוק את הרשומה — `Backend/Tanakh.Infrastructure/Services/SubscriberAnonymizationService.cs:11,20`. |
| `AdminPasswordHasher.cs` | מימוש `IAdminPasswordHasher` — PBKDF2-HMACSHA256, 210,000 איטרציות — `Backend/Tanakh.Infrastructure/Services/AdminPasswordHasher.cs:8,11`. |
| `Sms4FreeSmsSender.cs` | מימוש `ISmsSender` מול ספק SMS4FREE (או מצב `DryRun`), עם מיפוי קודי סטטוס וכתיבה ל-`sms_log` — `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:17,29-30,51` |
| `SmsBalanceService.cs` | מימוש `ISmsBalanceService` עם מטמון בן 5 דקות — `Backend/Tanakh.Infrastructure/Services/SmsBalanceService.cs:15,34` |
| `AdminService.cs` | מימוש `IAdminService` — הלוגיקה המרכזית ללוח הבקרה (KPI, ניהול משתמשים/SMS/שגיאות, ייצוא) — `Backend/Tanakh.Infrastructure/Services/AdminService.cs:16` |
| `AppSettingsService.cs` | מימוש `IAppSettingsService` — קריאה/כתיבה של שורות `app_settings` (מצב תחזוקה, באנר) עם מטמון בן 5 דקות — `Backend/Tanakh.Infrastructure/Services/AppSettingsService.cs:13` |
| `SubscriptionService.cs` | מימוש `ISubscriptionService` — OTP, הרשמה/עדכון/ביטול מנוי, רישום הסכמה — `Backend/Tanakh.Infrastructure/Services/SubscriptionService.cs:14` |

---

## 4. טבלת נקודות קצה (API Endpoints)

הערה: לכל ה-controllers תחת `api/v1/admin/*` (מלבד `login`/`verify-otp` ב-`AdminAuthController`) חלה הרשאת `[Authorize(Policy = "AdminOnly")]` — ראו סעיף 5.

### 4.1 `AdminAuthController` — `[Route("api/v1/admin/auth")]` — `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:27-29`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| POST | `/api/v1/admin/auth/login` | `LoginAsync` | גוף `AdminLoginRequest{Username,Password}` | `200 {otpRequired:true}` או `401 Problem(invalid_credentials)` | אנונימי; מוגבל ב-rate limiting למדיניות `AdminLogin` (5/15 דק' לכתובת IP) | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:54-96` |
| POST | `/api/v1/admin/auth/verify-otp` | `VerifyOtpAsync` | גוף `AdminVerifyOtpRequest{Code}` | `200 OK` (מגדיר עוגיית סשן) או `401 Problem(otp_invalid/otp_locked)` | אנונימי | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:98-144` |
| GET | `/api/v1/admin/auth/session` | `Session` | — | `200 OK` (בדיקת סשן ללא side-effect) | `[Authorize(Policy="AdminOnly")]` | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:149-151` |
| POST | `/api/v1/admin/auth/logout` | `LogoutAsync` | — | `200 OK` (מבטל את העוגייה) | `[Authorize(Policy="AdminOnly")]` | `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:153-160` |

### 4.2 `AdminController` — `[Route("api/v1/admin")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminController.cs:13-16`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| POST | `/api/v1/admin/actions/unsubscribe` | `UnsubscribeAsync` | גוף `AdminUnsubscribeRequest{PhoneNumber}` | `200 OK` / `404` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminController.cs:25-30` |
| POST | `/api/v1/admin/actions/requeue` | `RequeueAsync` | גוף `AdminRequeueRequest{DeliveryId}` | `200 OK` / `404` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminController.cs:32-37` |

### 4.3 `AdminExportController` — `[Route("api/v1/admin/export")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminExportController.cs:14-16`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/export/{resource}` | `ExportAsync` | נתיב `resource` (`users`/`sms-log`/`error-log`); שאילתה `search,status,level,from,to` | קובץ CSV (`text/csv; charset=utf-8`, עם BOM) / `404` אם `resource` לא מוכר | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminExportController.cs:29-56` |

### 4.4 `AdminLogsController` — `[Route("api/v1/admin/logs")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminLogsController.cs:13-15`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/logs` | `GetLogsAsync` | שאילתה `level,from,to,search,page(1),limit(25)` | `PagedResult<ErrorLogItem>` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminLogsController.cs:29-46` |
| GET | `/api/v1/admin/logs/top` | `GetTopErrorsAsync` | — | רשימת `TopError` (5 המובילים) | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminLogsController.cs:48-53` |
| PATCH | `/api/v1/admin/logs/{id:guid}/resolve` | `ResolveAsync` | נתיב `id` | `200 OK` / `404` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminLogsController.cs:55-60` |
| POST | `/api/v1/admin/logs/cleanup` | `CleanupAsync` | גוף `AdminCleanupLogsRequest{OlderThanDays}` | `200 {deleted}` / `400 invalid_older_than_days` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminLogsController.cs:62-72` |

### 4.5 `AdminSmsController` — `[Route("api/v1/admin/sms")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminSmsController.cs:12-14`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/sms/balance` | `GetBalanceAsync` | — | `{Ok,Balance,Error,lowBalanceThreshold}` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSmsController.cs:30-41` |
| GET | `/api/v1/admin/sms/log` | `GetLogAsync` | שאילתה `status,from,to,page(1),limit(25)` | `PagedResult<SmsLogItem>` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSmsController.cs:45-64` |
| GET | `/api/v1/admin/sms/stats` | `GetStatsAsync` | — | `SmsStats` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSmsController.cs:66-71` |
| POST | `/api/v1/admin/sms/test` | `SendTestAsync` | — | `200 OK` (שולח SMS בדיקה למספר המנהל המוגדר) | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSmsController.cs:73-78` |

### 4.6 `AdminStatsController` — `[Route("api/v1/admin/stats")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminStatsController.cs:10-12`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/stats/overview` | `GetOverviewAsync` | שאילתה `from,to` (ברירת מחדל: 7 ימים אחרונים) | `AdminOverview` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminStatsController.cs:22-31` |

### 4.7 `AdminSystemController` — `[Route("api/v1/admin/system")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:18-20`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/system/health` | `GetHealthAsync` | — | זמן עלייה, חיבור DB, שטח דיסק פנוי, `BUILD_VERSION` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:32-61` |
| GET | `/api/v1/admin/system/maintenance` | `GetMaintenanceAsync` | — | `MaintenanceStatus` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:63-68` |
| PUT | `/api/v1/admin/system/maintenance` | `SetMaintenanceAsync` | גוף `AdminSetMaintenanceRequest{Enabled,Message}` | `200 OK` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:70-76` |
| GET | `/api/v1/admin/system/banner` | `GetBannerAsync` | — | `BannerStatus?` (הערך הגולמי, ללא סינון תפוגה) | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:81-86` |
| PUT | `/api/v1/admin/system/banner` | `SetBannerAsync` | גוף `AdminSetBannerRequest{Text,ExpiresAt}` | `200 OK` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:88-94` |
| DELETE | `/api/v1/admin/system/banner` | `ClearBannerAsync` | — | `200 OK` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:96-102` |
| GET | `/api/v1/admin/system/flags` | `GetFlagsAsync` | — | רשימת `FeatureFlag` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:104-111` |
| PUT | `/api/v1/admin/system/flags/{name}` | `SetFlagAsync` | נתיב `name`; גוף `AdminSetFeatureFlagRequest{Enabled}` | `200 OK` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:113-130` |
| DELETE | `/api/v1/admin/system/flags/{name}` | `DeleteFlagAsync` | נתיב `name` | `200 OK` / `404` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:132-145` |

### 4.8 `AdminUsersController` — `[Route("api/v1/admin/users")]`, `[Authorize(Policy="AdminOnly")]` — `Backend/Tanakh.Api/Controllers/AdminUsersController.cs:12-14`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/users` | `GetUsersAsync` | שאילתה `search,status,from,to,page(1),limit(25)` | `PagedResult<UserListItem>` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminUsersController.cs:26-39` |
| PATCH | `/api/v1/admin/users/{id:guid}` | `UpdateStatusAsync` | נתיב `id`; גוף `AdminUserActionRequest{Action:"block"/"unblock"}` | `200 OK` / `400 invalid_action` / `404` | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminUsersController.cs:41-59` |
| DELETE | `/api/v1/admin/users/{id:guid}` | `DeleteAsync` | נתיב `id` | `200 OK` / `404` (בפועל: אנונימיזציה, לא מחיקה פיזית) | AdminOnly | `Backend/Tanakh.Api/Controllers/AdminUsersController.cs:61-66` |

### 4.9 `JewishCalendarController` — `[Route("[controller]")]` (⇒ `/JewishCalendar`, **לא** תחת `api/v1`) — `Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:8-10`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/JewishCalendar/getJewishCalendar` | `GetJewishCalendarAsync` | — | `bool` — האם כרגע בין הדלקת נרות להבדלה | אנונימי | `Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25` |

### 4.10 `ReadingProgressController` — `[Route("api/v1/reading-progress")]` — `Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:15-17`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| POST | `/api/v1/reading-progress` | `UpsertAsync` | גוף `ReadingProgressRequest{Token,Book,Chapter}` | `200 OK` / `400 Problem` | אנונימי; מזוהה ע"י אימות ידני של `Token` (`IUnsubscribeTokenService.TryValidate`) — לא attribute-based auth | `Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:34-56` |

### 4.11 `SubscriptionsController` — `[Route("api/v1/subscriptions")]` — `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:13-15`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| POST | `/api/v1/subscriptions/otp/request` | `RequestOtpAsync` | גוף `RequestOtpRequest{PhoneNumber}` | `200 OK` / `400 phone_required/phone_landline/phone_invalid` | אנונימי; rate limiting `SubscriptionOtpRequest` (5/15 דק' לכתובת IP) | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:29-47` |
| POST | `/api/v1/subscriptions` | `SubscribeAsync` | גוף `SubscriptionRequest` (טלפון, שם, שעה, אזור-זמן, דילוג שבת/חג, הסכמה, קוד OTP, גרסאות מסמכים משפטיים) | `200 SubscriptionResponse{ManageToken}` / `400` (סיבות שונות) | אנונימי; rate limiting `SubscriptionCreate` (5/שעה לכתובת IP) | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:50-109` |
| GET | `/api/v1/subscriptions/me` | `GetPreferencesAsync` | שאילתה `token` | `SubscriberPreferences` / `400 invalid_token` / `404` | אנונימי; מזוהה ע"י `token` (`IUnsubscribeTokenService`) | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-127` |
| POST | `/api/v1/subscriptions/me` | `UpdatePreferencesAsync` | גוף `UpdatePreferencesRequest{Token,PreferredTime,SkipShabbatHolidays,Action}` | `200 OK` / `400 invalid_token` | אנונימי; מזוהה ע"י `Token` בגוף | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:129-147` |
| POST | `/api/v1/subscriptions/me/unsubscribe` | `UnsubscribeAsync` | גוף `ManageTokenRequest{Token}` | `200 OK` / `400 invalid_token` | אנונימי; מזוהה ע"י `Token` בגוף | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:150-160` |

### 4.12 `SystemController` — `[Route("api/v1/system")]` — `Backend/Tanakh.Api/Controllers/SystemController.cs:15-17`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/api/v1/system/maintenance` | `GetMaintenanceAsync` | — | `MaintenanceStatus` | אנונימי | `Backend/Tanakh.Api/Controllers/SystemController.cs:28-33` |
| GET | `/api/v1/system/banner` | `GetBannerAsync` | — | `BannerStatus?` (מסונן לפי תפוגה — `null` אם פג/חסר) | אנונימי | `Backend/Tanakh.Api/Controllers/SystemController.cs:35-50` |
| GET | `/api/v1/system/flags` | `GetFlagsAsync` | — | `Dictionary<string,bool>` | אנונימי | `Backend/Tanakh.Api/Controllers/SystemController.cs:52-58` |

### 4.13 `TanakhController` — `[Route("[controller]")]` (⇒ `/Tanakh`, **לא** תחת `api/v1`) — `Backend/Tanakh.Api/Controllers/TanakhController.cs:10-12`

| שיטה | נתיב מלא | Action | פרמטרים/גוף | מחזיר | הרשאה | קובץ:שורה |
|---|---|---|---|---|---|---|
| GET | `/Tanakh/books/{section}` | `GetBookListAsync` | נתיב `section` (למשל `torah`) | רשימת `BaseStructure` | אנונימי | `Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30` |
| GET | `/Tanakh/books/main/{book}` | `getBookChapterAsync` | נתיב `book` | רשימת `BaseStructure` | אנונימי | `Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39` |
| GET | `/Tanakh/books/{book}/{chapter}` | `GetChapterAsync` | נתיב `book`, `chapter` | `TanakhContext` / `404` | אנונימי | `Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56` |

### 4.14 נקודות קצה שאינן controller (נרשמות ישירות ב-`Program.cs`)

| שיטה | נתיב מלא | מטרה | הרשאה | קובץ:שורה |
|---|---|---|---|---|
| GET | `/health/live` | Liveness — האם התהליך פעיל (ללא בדיקת תלויות, `Predicate = _ => false`) | אנונימי | `Backend/Tanakh.Api/Program.cs:295-298` |
| GET | `/health/ready` | Readiness — בודק רק את `tanakh-data` health check (תגית `ready`) | אנונימי | `Backend/Tanakh.Api/Program.cs:306-309` |

---

## 5. אימות והרשאה (Authentication & Authorization)

קיימות שתי מנגנוני זהות **נפרדים לחלוטין** באפליקציה — אין JWT ואין ASP.NET Core Identity:

**5.1 מנהל (Admin) — עוגיית סשן:**
- ה-scheme רשום בשם `"AdminCookie"` (`AdminCookieAuthDefaults.SchemeName` — `Backend/Tanakh.Api/Auth/AdminCookieAuthDefaults.cs:5`), מוגדר ב-`builder.Services.AddAuthentication(...).AddCookie(...)` — `Backend/Tanakh.Api/Program.cs:84-106`.
- מאפייני העוגייה: שם `tanakh_admin`, `HttpOnly=true`, `SecurePolicy=Always`, `SameSite=Strict`, תוקף 8 שעות ללא הארכה גולשת (`SlidingExpiration=false`) — `Backend/Tanakh.Api/Program.cs:87-92`.
- מכיוון שזהו API (לא אתר עם עמוד login), אירועי redirect-to-login/access-denied הוחלפו בתגובות JSON `401`/`403` — `Backend/Tanakh.Api/Program.cs:96-105`.
- זרימת כניסה: `POST /api/v1/admin/auth/login` בודק שם משתמש (השוואת זמן-קבוע) וסיסמה מול `Admin:Username`/`Admin:PasswordHash` (הגיבוב מבוצע מראש בעזרת `dotnet run -- --hash-admin-password`, PBKDF2-HMACSHA256 210,000 איטרציות — `Backend/Tanakh.Infrastructure/Services/AdminPasswordHasher.cs:8,11,15`), ואז מפיק קוד OTP בן 6 ספרות, שומר אותו מגובב (`IHashingService`) בטבלת `otp_codes` (תוקף 5 דקות, עד 3 ניסיונות) ושולח אותו ב-SMS למספר `Admin:Phone` — `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:54-96`.
- `POST /api/v1/admin/auth/verify-otp` מאמת את הקוד (השוואת זמן-קבוע), ובהצלחה קורא ל-`HttpContext.SignInAsync` עם `ClaimsIdentity` המכיל `ClaimTypes.Name` ו-`ClaimTypes.Role="admin"`, תחת ה-scheme `AdminCookie` — `Backend/Tanakh.Api/Controllers/AdminAuthController.cs:98-144`.
- **תפקיד/הרשאה יחיד**: `"admin"` בלבד — אין טבלת משתמשי-מנהל, קיים חשבון מנהל אחד המוגדר בקונפיגורציה (`AdminOptions`). מדיניות ההרשאה `"AdminOnly"` מוגדרת ב-`builder.Services.AddAuthorization(...)` ודורשת `ClaimTypes.Role == "admin"` — `Backend/Tanakh.Api/Program.cs:107-108`.
- האכיפה נעשית באמצעות attribute `[Authorize(Policy = "AdminOnly")]` — ברמת controller ב-`AdminController`, `AdminExportController`, `AdminLogsController`, `AdminSmsController`, `AdminStatsController`, `AdminSystemController`, `AdminUsersController`, וברמת action ב-`AdminAuthController.Session`/`LogoutAsync`.
- כל תגובה תחת `/api/v1/admin` (בין אם הצליחה ובין אם לא) מקבלת כותרת `X-Robots-Tag: noindex, nofollow` — מידלוור מותאם ב-`Backend/Tanakh.Api/Program.cs:249-256`.

**5.2 מנוי ציבורי (Subscriber) — טוקן "ניהול" (Manage Token) חתום, ללא session:**
- אין ASP.NET Core auth/attribute כלשהו על נקודות הקצה הציבוריות של `SubscriptionsController`/`ReadingProgressController` — הזיהוי נעשה ידנית בתוך גוף ה-action.
- הטוקן מונפק פעם אחת ב-`SubscriptionsController.SubscribeAsync` (`ISubscriptionService.SubscribeAsync` → `IUnsubscribeTokenService.Issue`) ונשמר בצד הלקוח (localStorage, לפי הערת קוד) — `Backend/Tanakh.Api/Model/SubscriptionResponse.cs:7`, `Backend/Tanakh.Domain/IUnsubscribeTokenService.cs:11`.
- מימוש הטוקן (`UnsubscribeTokenService`): מחרוזת `{keyId}.{payload}.{signature}` (Base64Url), חתומה ב-HMACSHA256 עם מפתח `Hashing:Pepper`, ללא תפוגה בכוונה (כדי שקישורי תזכורת ישנים ימשיכו לעבוד) — `Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:24-31,61-62`. אימות דרך `TryValidate` נעשה בכל action רלוונטי (`SubscriptionsController.GetPreferencesAsync/UpdatePreferencesAsync/UnsubscribeAsync`, `ReadingProgressController.UpsertAsync`).
- לפני הרשמה, בעלות על מספר הטלפון מאומתת בנפרד באמצעות OTP חד-פעמי (6 ספרות, טבלת `subscriber_otp_codes`, תוקף 10 דקות, עד 3 ניסיונות, עד 5 בקשות לשעה למספר) — `Backend/Tanakh.Infrastructure/Services/SubscriptionService.cs:38-73,75-106`.

**5.3 הגבלת קצב (Rate Limiting):** שלוש מדיניות `FixedWindowLimiter` מוגדרות ב-`Backend/Tanakh.Api/Program.cs:109-155`, מבוססות על כתובת ה-IP של המבקש (`RemoteIpAddress`), מוחלות באמצעות `[EnableRateLimiting("...")]`:
- `AdminLogin` — 5 בקשות / 15 דקות — על `AdminAuthController.LoginAsync`.
- `SubscriptionOtpRequest` — 5 בקשות / 15 דקות — על `SubscriptionsController.RequestOtpAsync`.
- `SubscriptionCreate` — 5 בקשות / שעה — על `SubscriptionsController.SubscribeAsync`.
בקשות שנדחו מקבלות `429 Too Many Requests` — `Backend/Tanakh.Api/Program.cs:111-115`.

---

## 6. Middlewares (סדר רישום ב-`Program.cs`)

הסדר להלן הוא סדר הקריאה בפועל בקובץ `Backend/Tanakh.Api/Program.cs`:

1. **ענף סביבה** (`Backend/Tanakh.Api/Program.cs:223-233`):
   - ב-`Development`: `app.UseDeveloperExceptionPage()` (עמוד שגיאה מפורט), `app.MapOpenApi()` (מסמך OpenAPI), `app.MapScalarApiReference()` (ממשק תיעוד Scalar) — שורות 225-227.
   - אחרת (כולל production): `app.UseExceptionHandler()` (מפעיל את `GlobalExceptionHandler`), `app.UseHsts()` — שורות 231-232.
2. `app.UseHttpsRedirection()` — הפניית HTTP ל-HTTPS — `Backend/Tanakh.Api/Program.cs:235`.
3. `app.UseRouting()` — `Backend/Tanakh.Api/Program.cs:237`.
4. `app.UseCors(...)` — רשימת מקורות מורשים בלבד (מ-`Cors:AllowedOrigins`), עם `AllowAnyMethod().AllowAnyHeader().AllowCredentials()` (נדרש credentials-CORS מפורש בגלל אימות עוגיות) — `Backend/Tanakh.Api/Program.cs:243-245`.
5. מידלוור inline מותאם: מוסיף כותרת `X-Robots-Tag: noindex, nofollow` לכל תגובה שנתיבה מתחיל ב-`/api/v1/admin` — `Backend/Tanakh.Api/Program.cs:249-256`.
6. מידלוור inline מותאם: **מצב תחזוקה** — אם מופעל (`IAppSettingsService.GetMaintenanceAsync`), מחזיר `503` עם גוף JSON `{maintenance:true, message}` לכל בקשה שאינה תחת `/api/v1/admin`, `/api/v1/system` או `/health` — `Backend/Tanakh.Api/Program.cs:262-284`.
7. `app.UseRateLimiter()` — `Backend/Tanakh.Api/Program.cs:286`.
8. `app.UseAuthentication()` — `Backend/Tanakh.Api/Program.cs:288`.
9. `app.UseAuthorization()` — `Backend/Tanakh.Api/Program.cs:289`.
10. `app.MapControllers()` — מיפוי נקודות הקצה של ה-controllers — `Backend/Tanakh.Api/Program.cs:291`.
11. `app.MapHealthChecks("/health/live", ...)` — `Backend/Tanakh.Api/Program.cs:295-298`.
12. `app.MapHealthChecks("/health/ready", ...)` — `Backend/Tanakh.Api/Program.cs:306-309`.

בנוסף, `builder.Services.AddProblemDetails(...)` (רשום לפני `app.Build()`, שורה 171) מוסיף שדה `traceId` לכל תגובת `ProblemDetails` (הן משגיאות והן מ-`Problem()`/`ValidationProblem()` בבקרים) — `Backend/Tanakh.Api/Program.cs:171-178`.

---

## 7. משתני סביבה והגדרות תצורה

מנגנון הקונפיגורציה הוא ה-`IConfiguration` הסטנדרטי של ASP.NET Core (`appsettings.json` ← `appsettings.{Environment}.json` ← משתני סביבה). לפי מוסכמת המסגרת, משתנה סביבה בשם `Section__Key` (קו-תחתון כפול) ממופה אוטומטית למפתח קונפיגורציה `Section:Key` — כך למשל `ConnectionStrings__AppDb` הופך ל-`ConnectionStrings:AppDb` הנקרא בקוד. אין ערכים אמיתיים מוצגים במסמך זה.

| משתנה/מפתח קונפיגורציה | מטרה | קובץ:שורה שקורא | ברירת מחדל בקוד |
|---|---|---|---|
| `ConnectionStrings:AppDb` (סביבת ריצה, דרך `ConnectionStrings__AppDb`) | מחרוזת חיבור ל-PostgreSQL של האפליקציה (pooled) | `Backend/Tanakh.Api/Program.cs:37-38` | אין — זורק חריגה אם חסר |
| `ConnectionStrings__MigrationsDb` (env var ישיר) | מחרוזת חיבור נפרדת, בעלת הרשאות גבוהות יותר, ל-`dotnet ef` design-time | `Backend/Tanakh.Infrastructure/Data/AppDbContextFactory.cs:16-18` | אין — זורק חריגה אם חסר |
| `ConnectionStrings:MigrationsDb` | אותה מחרוזת חיבור, לשימוש `--reset-db` בזמן ריצה | `Backend/Tanakh.Infrastructure/Seeding/DatabaseSeeder.cs:28-29` | אין — זורק חריגה אם חסר |
| `Cors:AllowedOrigins` | רשימת מקורות מורשים ל-CORS | `Backend/Tanakh.Api/Program.cs:243-244` | מערך ריק |
| `TanakhData:DataDirectory` | נתיב חלופי לתיקיית קבצי נתוני התנ"ך | `Backend/Tanakh.Infrastructure/Options/TanakhDataOptions.cs:9` | `null` (נופל ל-`ContentRootPath/Data`, `Backend/Tanakh.Infrastructure/CacheProvider.cs:24-25`) |
| `Sms:Key`, `Sms:User`, `Sms:Pass` | פרטי חיבור ל-SMS4FREE (סוד) | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:11-12` | `""` (מחרוזת ריקה) |
| `Sms:Sender` | שם השולח ב-SMS | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:20` | `""` |
| `Sms:ApiUrl` | כתובת API לשליחת SMS | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:25` | `https://api.sms4free.co.il/ApiSMS/v2/SendSMS` |
| `Sms:BalanceApiUrl` | כתובת API לבדיקת יתרה | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:29` | `https://api.sms4free.co.il/ApiSMS/AvailableSMS` |
| `Sms:TimeoutSeconds` | Timeout ל-HttpClient של שליחת SMS | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:31`, `Backend/Tanakh.Api/Program.cs:162-163` | `15` |
| `Sms:DryRun` | אם `true` — לא נשלח SMS אמיתי, רק נרשם ליומן | `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:36`, `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:51` | `true` |
| `Hashing:Pepper` | מפתח HMAC לגיבוב (`IHashingService`) ולחתימת טוקן הניהול | `Backend/Tanakh.Infrastructure/Options/HashingOptions.cs:11` | `""` (זורק חריגה בזמן ריצה אם ריק — `Backend/Tanakh.Infrastructure/Services/HashingService.cs:21-24`) |
| `Retention:ReminderDeliveriesRetentionDays` | ימי שימור רשומות `reminder_deliveries` | `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:12` | `90` |
| `Retention:UnsubscribedSubscriberRetentionMonths` | חודשי שימור לפני אנונימיזציה של מנוי מבוטל | `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:14` | `12` |
| `Retention:RunInterval` | תדירות הרצת סבב השימור | `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:17` | 24 שעות |
| `Retention:BatchSize` | גודל אצווה למחיקה/עדכון | `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:21` | `5000` |
| `Retention:DelayBetweenBatches` | השהיה בין אצוות | `Backend/Tanakh.Infrastructure/Options/RetentionOptions.cs:23` | 200 מ"ש |
| `Reminders:PlannerCron` | ביטוי cron יומי לתזמון (`דקה שעה * * *` בלבד) | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:10` | `"5 0 * * *"` |
| `Reminders:DispatchIntervalSeconds` | תדירות בדיקת משלוחים ממתינים | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:12` | `60` |
| `Reminders:MaxLatenessMinutes` | חלון סבילות לאיחור לפני דילוג | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:14` | `60` |
| `Reminders:BatchSize` | גודל אצווה לתפיסת משלוחים | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:16` | `100` |
| `Reminders:MaxAttempts` | מספר ניסיונות שליחה מרבי | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:18` | `3` |
| `Reminders:RetryBackoffMinutes` | מרווחי backoff בין ניסיונות (דקות) | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:20` | `[1,5,25]` |
| `Reminders:SendRatePerSecond` | קצב שליחה מרבי | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:22` | `10` |
| `Reminders:DefaultTimezone` | אזור-זמן ברירת מחדל | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:24` | `Asia/Jerusalem` |
| `Reminders:DefaultStartBook` | ספר התחלה ברירת מחדל | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:26` | `Genesis` |
| `Reminders:DefaultStartChapter` | פרק התחלה ברירת מחדל | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:28` | `1` |
| `Reminders:SmsTemplate` | תבנית טקסט הודעת התזכורת | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:33-34` | טקסט עברי קבוע (ראו קובץ) |
| `Reminders:PublicBaseUrl` | כתובת בסיס של הפרונטאנד (לקישור בתזכורת) | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:39` | `""` |
| `Reminders:ApiBaseUrl` | כתובת בסיס של ה-API עצמו | `Backend/Tanakh.Infrastructure/Options/RemindersOptions.cs:42` | `""` |
| `Admin:Username` | שם המשתמש של המנהל היחיד | `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:7` | `""` |
| `Admin:PasswordHash` | גיבוב סיסמת המנהל (לא סיסמה גלויה) | `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:11` | `""` |
| `Admin:Phone` | מספר טלפון לשליחת OTP כניסה | `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:14` | `""` |
| `Admin:LowBalanceThreshold` | סף להתראת יתרת SMS נמוכה | `Backend/Tanakh.Infrastructure/Options/AdminOptions.cs:19` | `50` |
| `Logging:LogLevel:*` | רמות לוג לפי קטגוריה | `Backend/Tanakh.Api/appsettings.json:2-7` | `Default=Information`, `Microsoft=Warning`, `Microsoft.Hosting.Lifetime=Information` |
| `AllowedHosts` | רשימת hosts מורשים (הגדרת ASP.NET Core סטנדרטית) | `Backend/Tanakh.Api/appsettings.json:9` | `"*"` |
| `ASPNETCORE_ENVIRONMENT` | קובע סביבת ריצה (`Development`/אחר) — משפיע על `app.Environment.IsDevelopment()` ברחבי `Program.cs` | `Backend/Tanakh.Api/Properties/launchSettings.json:17,27` | `Development` (בפרופילי הרצה מקומיים בלבד) |
| `BUILD_VERSION` | גרסת build המוצגת בבדיקת בריאות המנהל | `Backend/Tanakh.Api/Controllers/AdminSystemController.cs:56` | `"dev"` אם לא מוגדר |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | פרטי אתחול קונטיינר Postgres מקומי (superuser, לא תפקיד הריצה של האפליקציה) | `Backend/.env.example:5-8`, `Backend/docker-compose.yml:7-11` | אין ברירת מחדל בקוד ל-.NET (לא נקרא ע"י האפליקציה עצמה — רק ע"י docker-compose) |

---

## 8. טיפול בשגיאות ולוגים (Error Handling & Logging)

- **מטפל חריגות גלובלי**: `Backend/Tanakh.Api/GlobalExceptionHandler.cs` מממש `IExceptionHandler` ורשום ב-`builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` — `Backend/Tanakh.Api/Program.cs:179`; מופעל בפועל רק מחוץ ל-`Development` דרך `app.UseExceptionHandler()` — `Backend/Tanakh.Api/Program.cs:231` (ב-`Development` פועל במקום זאת `app.UseDeveloperExceptionPage()` — `Backend/Tanakh.Api/Program.cs:225`).
- בתוך `TryHandleAsync`: רושם שגיאה ל-`ILogger<GlobalExceptionHandler>` כולל `TraceId` — `Backend/Tanakh.Api/GlobalExceptionHandler.cs:28-30`; קובע קוד סטטוס `500`; כותב שורת `ErrorLog` חדשה (`Level=Error`, `Message`, `StackTrace`, `Endpoint`, `StatusCode`) ל-`AppDbContext` הנשלף מ-`RequestServices` (המטפל רשום כ-singleton ולכן אינו יכול לקבל `AppDbContext` scoped בבנאי) — `Backend/Tanakh.Api/GlobalExceptionHandler.cs:54-69`; כשל בכתיבה ל-DB נבלע ונרשם ללוג בלבד, כדי לא לחסום את תגובת השגיאה עצמה — `Backend/Tanakh.Api/GlobalExceptionHandler.cs:71-74`.
- התגובה ללקוח נבנית דרך `IProblemDetailsService` בפורמט RFC 9110/`ProblemDetails`, עם `Status=500`, `Title="An unexpected error occurred."` — `Backend/Tanakh.Api/GlobalExceptionHandler.cs:36-46`.
- `builder.Services.AddProblemDetails(...)` מוסיף שדה `traceId` (מ-`Activity.Current?.Id` או `HttpContext.TraceIdentifier`) לכל `ProblemDetails` שמוחזר בכל האפליקציה — לא רק מחריגות בלתי-מטופלות, אלא גם מקריאות מפורשות ל-`Problem(...)` בבקרים — `Backend/Tanakh.Api/Program.cs:171-178`.
- **יומן השגיאות עצמו** (`error_log`/`ErrorLog`) נגיש/ניתן לניהול דרך `AdminLogsController` (רשימה מסוננת, 5 השגיאות השכיחות ביותר, סימון "נפתר", ניקוי לפי גיל) וניתן לייצוא CSV דרך `AdminExportController` — ראו סעיף 4.
- **תצורת לוגים**: מוגדרת רק ב-`Backend/Tanakh.Api/appsettings.json:2-7` (`Logging:LogLevel:Default=Information`, `Microsoft=Warning`, `Microsoft.Hosting.Lifetime=Information`). לא נמצאה בקוד/ב-`.csproj` כל תלות בספריית לוגים חיצונית (כגון Serilog, NLog, Application Insights) — הלוגים יוצאים דרך ספקי ה-logging המובנים הסטנדרטיים של ASP.NET Core (`ILogger<T>`/`Microsoft.Extensions.Logging`) בלבד, ללא sink מפורש נוסף שנמצא בקוד.
- שירותים נוספים כותבים ליומנים דומים באופן עצמאי: `Sms4FreeSmsSender` כותב לכל קריאה שורת `sms_log` (הצלחה/כשל, קוד סטטוס, תגובה גולמית) — `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:99-123`; `AdminAuthController`/`AdminSystemController`/`AdminService` כותבים שורות `AuditLogEntry` ל-`audit_log` עבור פעולות רגישות (כניסת מנהל, שינויי הגדרות מערכת, חסימת/מחיקת משתמש וכו').

---

## לא ידוע / דורש אימות

1. **אכיפת `Sms:DryRun` בזמן ריצה**: ההערה בקובץ `Backend/Tanakh.Infrastructure/Options/SmsOptions.cs:33-35` טוענת ש-"`DryRun` must be true in every non-production environment, **enforced at startup**, not just by convention" — אך חיפוש מקיף בקוד (`GetEnvironmentVariable`, בדיקות ב-`Program.cs`, ולידציית Options כלשהי) לא העלה שום קוד שאכן אוכף זאת בפועל בזמן עליית השרת. ייתכן שהאכיפה קיימת במנגנון מחוץ למאגר זה (למשל בצינור ה-CI/CD או בקובץ `appsettings` שלא ניגש אליו כאן) — לא אומת.
2. **כתובת בינדינג בפועל ב-production**: לא נמצא בקוד (`Program.cs`, `appsettings*.json`, `launchSettings.json`) ערך מפורש לכתובת/פורט ההאזנה מחוץ לסביבת הפיתוח. ההנחה שהדבר נשלט על-ידי `ASPNETCORE_URLS`/ברירת מחדל של Kestrel/קונטיינר לא אומתה מול קובץ תצורת פריסה בפועל (לא נבדק `Backend/Dockerfile` לעומק כחלק ממשימה זו).
3. **`Microsoft.OpenApi` (2.11.0) ו-`Microsoft.EntityFrameworkCore.Design`**: לא נמצא `using` ישיר בקוד עבור חבילות אלה — השימוש בהן הוא עקיף/בזמן-build בלבד, כפי שצוין בטבלת התלויות; לא אומת אילו API-ים מדויקים בתוכן נצרכים בפועל על-ידי `AddOpenApi`/`dotnet ef` "מתחת למכסה המנוע".
4. תיעוד מפורט של `Tanakh.Infrastructure/Reminders/*` (הפלנר והדיספאצ'ר) הושמט בכוונה מהמסמך הזה בהתאם להנחיה — מכוסה במסמך שירותים/אינטגרציות נפרד.
5. תיעוד מלא של סכימת מסד הנתונים (טבלאות, עמודות, אילוצים, אינדקסים, מיגרציות בפירוט) הושמט בכוונה — מכוסה במסמך נפרד ייעודי לבסיס הנתונים.
