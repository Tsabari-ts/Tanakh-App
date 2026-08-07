# בדיקות עומס (Load Testing) — Tanakh Backend

תיקייה זו מכילה תסריטי בדיקת-עומס עבור ה-API (`Backend/Tanakh.Api`), לקראת
עלייה לאוויר על **שרת production יחיד**, בציפייה לעד כמה מאות משתמשים
בו-זמנית, עם תרחיש ספייק לכ-1000 משתמשים (למקרה שקישור לאתר מופץ בקבוצה
גדולה). כל הנתיבים שבתסריטים אומתו ידנית מול קוד ה-controller בפועל תחת
`Backend/Tanakh.Api/Controllers/` (לא רק מול `docs/audit/03-backend.md`) —
ראו את טבלת הנתיבים המלאה למטה, ואת ההערות עם ציוני `קובץ:שורה` בתוך
`loadtest/config.js` ובתוך כל תסריט.

---

## ⚠️ אזהרה חשובה — קראו לפני הרצה

> **לעולם אל תריצו תסריט מכאן מול כתובת production אמיתית.**
> כל התסריטים משתמשים ב-`BASE_URL` שברירת המחדל שלו היא
> `http://localhost:5000` בלבד. אם תרצו להריץ מול סביבת staging/dev —
> הזינו זאת מפורשות דרך `-e BASE_URL=...`, ווודאו פעמיים שזו לא כתובת
> ה-production האמיתית של המשתמשים.
>
> **אף תסריט לא כותב ל-DB כברירת מחדל.** שני נתיבים בלבד יכולים לבצע כתיבה
> (`POST /api/v1/reading-progress` ב-`load.js`/`soak.js`), והם **כבויים
> כברירת מחדל** — פעילים רק אם מספקים ידנית משתנה סביבה `TEST_MANAGE_TOKEN`
> (ראו סעיף "טוקן בדיקה" למטה). אם מדליקים אותם — הריצו רק מול מסד נתונים
> חד-פעמי/פיתוח בר-השלכה (disposable dev DB), **לעולם לא מול מסד production**.
>
> נקודות הקצה של הרשמה (`POST /api/v1/subscriptions/otp/request`,
> `POST /api/v1/subscriptions`) **אינן נכללות בשום תסריט אוטומטי כאן בכלל**
> — הן כותבות רשומת מנוי אמיתית, שולחות SMS אמיתי (אלא אם
> `Sms:DryRun=true`), ומוגבלות ב-rate limiting ל-5 בקשות/שעה לכתובת IP
> (`Backend/Tanakh.Api/Program.cs:146-155`) כך שממילא אי אפשר לבדוק בהן
> עומס אמיתי — ובנוסף `SubscribeAsync` דורש קוד OTP אמיתי שהתקבל ב-SMS,
> שלא ניתן לאוטומט מתוך k6/Artillery בלי גישה לתיבת ה-SMS. ראו פירוט
> בטבלה למטה, שורת "לא נכלל".

---

## 1. התקנה

### k6 (הכלי הראשי)

- Windows (winget): `winget install k6 --source winget`
- Windows (Chocolatey): `choco install k6`
- אימות התקנה: `k6 version`

תיעוד רשמי: https://k6.io/docs/get-started/installation/

### Artillery (חלופה, אופציונלי)

רק אם אי אפשר להתקין k6 (למשל מגבלות הרשאות בסביבה). לא נדרש אם משתמשים ב-k6.

```
npm install -g artillery
artillery version
```

---

## 2. הרצת כל תרחיש

בכל הפקודות: `BASE_URL` הוא כתובת ה-API הרצה (בפיתוח מקומי, ראו
`Backend/Tanakh.Api/Properties/launchSettings.json:25` — פרופיל `Tanakh`
מאזין על `http://localhost:5000`/`https://localhost:5001`). אם לא מציינים
`BASE_URL`, כל התסריטים נופלים אוטומטית ל-`http://localhost:5000`.

### smoke.js — בדיקת שפיות (5 VUs, דקה אחת)

מריץ פעם אחת (בלולאה, כי הריצה נמדדת בזמן) על כל נקודות הקצה הציבוריות
המרכזיות. מטרה: לוודא שהאפליקציה בכלל עונה לפני שמריצים עומס אמיתי.

```
k6 run loadtest/smoke.js
k6 run -e BASE_URL=http://localhost:5000 loadtest/smoke.js
```

**ציפייה**: 0 שגיאות. אם `smoke.js` נכשל — אין טעם להמשיך לתרחישים הכבדים
יותר; קודם מתקנים את מה שגרם לכשל שם.

### load.js — עומס "שיא רגיל" (ramp ל-300 VUs, החזקה 5 דקות)

מדמה את התרחיש שהאתר צריך לעמוד בו ביום-יום: עד כמה מאות משתמשים
בו-זמנית, בתמהיל תעבורה משוקלל (בעיקר קריאת פרקים).

```
k6 run loadtest/load.js
k6 run -e BASE_URL=http://localhost:5000 loadtest/load.js
```

כדי לכלול גם את הפעולות התלויות-בטוקן (שמירת התקדמות קריאה, קריאת מסך
"ההגדרות שלי") — ראו סעיף "טוקן בדיקה" למטה, ואז:

```
k6 run -e BASE_URL=http://localhost:5000 -e TEST_MANAGE_TOKEN=<הטוקן שלכם> loadtest/load.js
```

### spike.js — ספייק (קפיצה ל-~1000 VUs, דקה אחת)

מדמה קישור שהופץ בקבוצה גדולה. קורא **רק** לנקודות קצה קריאה-בלבד
מהזיכרון (ללא כתיבה, ללא rate limiting) — ראו הסבר מלא בראש הקובץ.

```
k6 run loadtest/spike.js
k6 run -e BASE_URL=http://localhost:5000 loadtest/spike.js
```

### soak.js — השרייה (100 VUs, 30 דקות רצוף)

לחשיפת דליפות זיכרון/חיבורים שריצה קצרה לא תספיק לחשוף.

```
k6 run loadtest/soak.js
```

לניתוח מגמה לאורך זמן (ולא רק סיכום מצטבר בסוף) — ייצאו תוצאות לקובץ:

```
k6 run -e BASE_URL=http://localhost:5000 --out json=soak-results.json loadtest/soak.js
```

### artillery.yml — חלופה ל-load.js

```
BASE_URL=http://localhost:5000 artillery run loadtest/artillery.yml
```

ב-PowerShell:

```
$env:BASE_URL="http://localhost:5000"
artillery run loadtest/artillery.yml
```

---

## 3. טוקן בדיקה (TEST_MANAGE_TOKEN)

שתי נקודות קצה תלויות ב"טוקן ניהול" חתום (`ReadingProgressController`,
`SubscriptionsController.GetPreferencesAsync/UpdatePreferencesAsync/UnsubscribeAsync`)
— הטוקן חתום HMAC עם `Hashing:Pepper` הסודי של השרת
(`Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:24-31`),
כך שאי אפשר "לזייף" אחד בלי לדעת את הסוד. אין דרך אוטומטית וזולה לייצר
טוקן חוקי מתוך k6/Artillery.

**כדי לבדוק את הנתיבים האלה תחת עומס, יש להצטייד ב-manage token אמיתי
של מנוי בדיקה, ידנית, מראש:**

1. הריצו את השרת מול **מסד נתונים חד-פעמי/פיתוח בלבד** (לעולם לא production).
2. הריצו זרימת הרשמה מלאה פעם אחת ידנית (או דרך Swagger/Scalar בסביבת
   Development — `Backend/Tanakh.Api/Program.cs:227`):
   - `POST /api/v1/subscriptions/otp/request` עם מספר טלפון בדיקה תקין,
   - קבלת קוד ה-OTP (אם `Sms:DryRun=true` הוא לא נשלח בפועל אלא רק נכתב
     ל-`sms_log` — `Backend/Tanakh.Infrastructure/Services/Sms4FreeSmsSender.cs:99-123`),
   - `POST /api/v1/subscriptions` עם הקוד שהתקבל.
3. התגובה מחזירה `{ "manageToken": "..." }`
   (`Backend/Tanakh.Api/Model/SubscriptionResponse.cs:7`) — שמרו אותו.
4. הריצו את `load.js`/`soak.js` עם `-e TEST_MANAGE_TOKEN=<הטוקן>`.

אם לא מספקים את המשתנה, `load.js`/`soak.js` מדלגים אוטומטית על הנתיבים
האלה (הבקשות המתוזמנות עבורם נופלות בחזרה לקריאת פרק רגילה) — הריצה
עדיין תקינה ומלאה, פשוט בלי לכסות את הנתיבים התלויים-בטוקן.

---

## 4. איך קוראים את התוצאות של k6

בסוף כל ריצה k6 מדפיס סיכום כזה (בקירוב):

```
     ✓ chapter read: status 200

     checks.........................: 100.00% ✓ 12345 ✗ 0
     http_req_duration..............: avg=45ms  min=2ms  med=30ms  p(90)=80ms  p(95)=120ms  max=900ms
     http_req_failed.................: 0.00%   ✓ 0     ✗ 12345
     http_reqs.......................: 12345   205.75/s
     vus_max..........................: 300
```

מה חשוב להסתכל עליו:

- **`http_req_failed`** — אחוז הבקשות שנכשלו (סטטוס `>=400`, timeout,
  connection refused). זה השדה הכי חשוב לבדיקת "האם השרת שרד". אם הריצה
  מדפיסה `THRESHOLDS` באדום ליד `http_req_failed` — הבדיקה נכשלה.
- **`http_req_duration` / `p(95)`** — 95 אחוז מהבקשות היו מהירות מהערך
  הזה. זה המדד המרכזי לחוויית משתמש (לא הממוצע — חריגים בודדים לא צריכים
  להסתיר בעיה שמשפיעה על 1 מכל 20 משתמשים).
- **`http_reqs` (.../s)** — התפוקה בפועל (בקשות לשנייה) שהשרת הצליח לספוג.
- **תגיות (`{endpoint:...}`)** — אם מריצים עם threshold על תגית ספציפית
  (כמו `http_req_duration{endpoint:chapter_read}`), k6 מדפיס שורת סיכום
  נפרדת לכל תגית — כך אפשר להבדיל בין "קריאת פרק איטית" (בעיה אמיתית,
  כי זה אמור להיות זול) לבין "כתיבת התקדמות איטית" (פחות מפתיע, יש שם
  round-trip ל-Postgres).

אם threshold נכשל, k6 מסיים עם exit code שונה מ-0 — שימושי לשילוב ב-CI,
אך **אין** להריץ תסריטים אלו אוטומטית ב-CI מול production.

---

## 5. ספי הצלחה/כישלון (thresholds) — והנימוק שמאחוריהם

ההנחה: שרת production **יחיד**, עד כמה מאות משתמשים בו-זמנית ביום-יום,
עם אפשרות לספייק זמני לכ-1000. זו לא רמת SLA ארגונית — הציפיות כאן
מכוונות לגודל האתר בפועל, לא ליעדים גנריים.

| תרחיש | עומס | סף שגיאות (fail אם חורג) | סף p95 (קריאה) | סף p95 (כתיבה) | נימוק קצר |
|---|---|---|---|---|---|
| `smoke.js` | 5 VUs, 1 דקה | `rate<=0` (כל שגיאה = כשל) | 2000ms | — | עומס זניח; כל שגיאה בהיקף הזה מעידה על תקלה בסיסית, לא על עומס |
| `load.js` | 300 VUs, 5 דק' החזקה | `rate<0.01` (1%) | 300ms (קריאות מהזיכרון) / 500ms (כללי) | 800ms | קריאת פרק היא בעיקרה חיפוש במילון בזיכרון אחרי חימום מטמון (`MemoryTanakhCache`, תפוגה 12 שעות) — אמורה להישאר זולה גם ב-300 VUs על שרת יחיד; כתיבה ל-Postgres יקרה יותר אך עדיין שאילתה בודדת פשוטה |
| `spike.js` | ~1000 VUs, 1 דקה | `rate<0.20` — זהו הגדרת "כשל טוטלי" המספרית (מעל חמישית מהבקשות נכשלות) | 5000ms | — (אין כתיבה בתרחיש זה) | ספייק פי 3.3 מהעומס השגרתי על שרת יחיד; מותר להתכופף (תגובות איטיות, כמה 5xx) בלי להיחשב "נפילה מלאה". 5-20% שגיאות עדיין "עובר" פורמלית אך ראוי לבדיקה ידנית |
| `soak.js` | 100 VUs, 30 דק' רצוף | `rate<0.01` | 300ms | 800ms | עומס נמוך מ-load.js בכוונה — אם התוצאות מידרדרות בכל זאת לאורך 30 הדקות (ולא רק ברגע ה-warm-up), זה מצביע על דליפה, לא על עומס גולמי |

**שיעור שגיאות "כמעט 0%"** ב-`load.js`/`soak.js`: באתר תוכן ציבורי בעומס
צפוי (לא ספייק), אין סיבה טובה לשגיאות בכלל — 1% משאיר מרווח לרעש רשת
חולף בלבד, לא לבאגים אמיתיים.

**תפוקה צפויה**: אין כאן סף מפורש על `http_reqs/s` כי זה תלוי לגמרי
בחומרה של השרת בפועל (לא ידועה כאן) — אבל כסדר-גודל: 300 VUs עם think-time
של ~1.5-3 שניות בין בקשות (`load.js`) אמורים לייצר בסביבות 100-200 בקשות/שנייה
בממוצע; אם ה-`http_reqs/s` בפועל נמוך משמעותית מזה תוך כדי ש-VUs נשארים
בשיא, זה כשלעצמו סימן לצוואר-בקבוק (השרת "בולע" בקשות לאט יותר משהמשתמשים
שולחים אותן).

---

## 6. טבלת נקודות הקצה שנבדקות — ומה דילגנו עליו ולמה

כל שורה מאומתת מול קוד ה-controller בפועל (לא רק `docs/audit/03-backend.md`).

### נכללות בתסריטים (ציבורי, אנונימי, קריאה-בלבד, ללא rate limit)

| שיטה | נתיב | קובץ:שורה | נכלל ב- |
|---|---|---|---|
| GET | `/Tanakh/books/{book}/{chapter}` | `Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56` | smoke, load, spike, soak, artillery — **הכי כבד/נפוץ בפועל** |
| GET | `/Tanakh/books/{section}` | `Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30` | smoke, load, spike, soak, artillery |
| GET | `/Tanakh/books/main/{book}` | `Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39` | smoke, load, spike, soak, artillery |
| GET | `/api/v1/system/maintenance` | `Backend/Tanakh.Api/Controllers/SystemController.cs:28-33` | smoke, load, soak, artillery |
| GET | `/api/v1/system/banner` | `Backend/Tanakh.Api/Controllers/SystemController.cs:35-50` | smoke, load, soak, artillery |
| GET | `/api/v1/system/flags` | `Backend/Tanakh.Api/Controllers/SystemController.cs:52-58` | smoke, load, soak, artillery |
| GET | `/health/live` | `Backend/Tanakh.Api/Program.cs:295-298` | smoke בלבד (בדיקת תשתית, לא תעבורת משתמשים אמיתית) |
| GET | `/JewishCalendar/getJewishCalendar` | `Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25` | smoke בלבד — ראו אזהרה למטה |

### נכללות, אך כבויות כברירת מחדל (opt-in, דורשות `TEST_MANAGE_TOKEN`)

| שיטה | נתיב | קובץ:שורה | כותב ל-DB? | נכלל ב- |
|---|---|---|---|---|
| POST | `/api/v1/reading-progress` | `Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:34-56` | **כן** (upsert) | load, soak — opt-in בלבד |
| GET | `/api/v1/subscriptions/me` | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-127` | לא (קריאה) | load — opt-in בלבד |

### לא נכללות בשום תסריט — ונימוק

| שיטה | נתיב | קובץ:שורה | למה לא נכלל |
|---|---|---|---|
| POST | `/api/v1/subscriptions/otp/request` | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:29-47` | כותב OTP אמיתי + שולח SMS (אלא אם `DryRun`); מוגבל ל-5/15 דק' לכתובת IP (`RateLimiterPolicyNames.SubscriptionOtpRequest`, `Backend/Tanakh.Api/Program.cs:134-144`) — לא ניתן לבדוק עומס אמיתי, וכל תסריט עומס פשוט יקבל בעיקר `429` |
| POST | `/api/v1/subscriptions` | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:50-109` | כותב מנוי אמיתי; דורש קוד OTP תקין שהתקבל בפועל ב-SMS (לא ניתן לאוטומט בלי גישה לתיבת ה-SMS); מוגבל ל-5/שעה לכתובת IP (`RateLimiterPolicyNames.SubscriptionCreate`, `Backend/Tanakh.Api/Program.cs:146-155`) |
| POST | `/api/v1/subscriptions/me/unsubscribe` | `Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:150-160` | כותב (מבטל מנוי אמיתי) — הרצה חוזרת-ונשנית תבטל את מנוי הבדיקה עצמו, מה שהופך אותו ללא-שמיש להמשך הריצה; לא נדרש להדגמת עומס (זו פעולה נדירה יחסית מבחינת נפח) |
| כל `/api/v1/admin/*` (13 נקודות קצה תחת `AdminAuthController`/`AdminController`/`AdminExportController`/`AdminLogsController`/`AdminSmsController`/`AdminStatsController`/`AdminSystemController`/`AdminUsersController`) | ראו `docs/audit/03-backend.md` סעיפים 4.1-4.8 | דורשות עוגיית סשן `AdminCookie` + זרימת OTP-של-מנהל (`Backend/Tanakh.Api/Program.cs:84-108`); זו לא תעבורת משתמשים ציבורית (מנהל יחיד באתר) — לא רלוונטי לבדיקת עומס של "עד כמה מאות משתמשים" |
| GET | `/JewishCalendar/getJewishCalendar` | `Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25` | **לא** נכלל ב-load/spike/soak: המימוש (`Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:53-58`) פונה בפועל ל-`hebcal.com` (צד שלישי) בכל בקשה, ללא שום מטמון בקוד — עומס עליה בפועל הוא עומס על שרת חיצוני, לא על השרת שלנו, ועלול להזיק לשירות של צד ג'. נכלל רק ב-`smoke.js` (קריאה בודדת) לבדיקת זמינות בסיסית |

---

## 7. קבצים בתיקייה זו

- `config.js` — מקור-אמת משותף (BASE_URL, רשימות ספרים/פרקים אמיתיים,
  נתיבים, עוזרי בחירה אקראית) שמיובא על-ידי כל תסריט k6.
- `smoke.js`, `load.js`, `spike.js`, `soak.js` — תסריטי k6.
- `artillery.yml` — חלופה ל-`load.js` באמצעות Artillery.
