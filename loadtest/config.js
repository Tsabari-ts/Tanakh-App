// loadtest/config.js
//
// מקור-אמת יחיד לכתובת הבסיס, לנקודות הקצה שנבדקו, ולנתוני בדיקה
// (ספרים/פרקים אמיתיים) שבהם משתמשים כל תסריטי ה-k6 בתיקייה הזו.
// מיובא ע"י smoke.js / load.js / spike.js / soak.js.
//
// כל נתיב צוין כאן עם מיקום מדויק (קובץ:שורה) שבו אומת מול קוד ה-controller
// בפועל תחת Backend/Tanakh.Api/Controllers (לא מתוך האודיט בלבד).
// ראו גם docs/audit/03-backend.md סעיף 4 להשוואה, ו-README.md לפירוט מלא.

// ---------------------------------------------------------------------------
// כתובת בסיס: תמיד דרך משתנה סביבה, עם ברירת מחדל בטוחה שמצביעה על
// localhost בלבד. לעולם אל תזינו כאן כתובת production אמיתית.
// ---------------------------------------------------------------------------
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// ---------------------------------------------------------------------------
// טוקן "ניהול" (manage token) של מנוי בדיקה אמיתי, המשמש לאימות (ידני, לא
// attribute-based) בנקודות הקצה של SubscriptionsController.GetPreferencesAsync/
// UpdatePreferencesAsync/UnsubscribeAsync ו-ReadingProgressController.UpsertAsync.
// הטוקן חתום HMAC (Hashing:Pepper) ואי אפשר לזייף אותו בלי המפתח הסודי של
// השרת - ראו Backend/Tanakh.Infrastructure/Services/UnsubscribeTokenService.cs:24-31.
//
// כברירת מחדל המשתנה ריק, ואז כל התסריטים מדלגים אוטומטית על נקודות הקצה
// התלויות בטוקן (הן קריאה של העדפות מנוי, והן כתיבת reading-progress).
// כדי לכלול אותן: הריצו את זרימת ההרשמה (subscribe) פעם אחת ידנית מול
// מסד נתונים חד-פעמי/פיתוח בלבד, שמרו את ה-manageToken שמוחזר, והעבירו
// אותו כאן. פירוט מלא ב-README.md, סעיף "טוקן בדיקה".
export const TEST_MANAGE_TOKEN = __ENV.TEST_MANAGE_TOKEN || '';

// דגל נוח: האם לכלול את נקודות הקצה התלויות-בטוקן בתמהיל התעבורה.
export const ENABLE_TOKEN_ENDPOINTS = TEST_MANAGE_TOKEN.length > 0;

// ---------------------------------------------------------------------------
// נקודות קצה ציבוריות, אנונימיות, קריאה-בלבד (ללא כתיבה למסד נתונים,
// ללא הגבלת קצב, ללא תלות בטוקן) - אלה "לב" התעבורה האמיתית של האתר.
// ---------------------------------------------------------------------------

// GET /Tanakh/books/{book}/{chapter} - טקסט הפרק בעברית + ניווט הבא/קודם.
// Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56 (TanakhTextService.GetChapterAsync).
// זו נקודת הקצה הכבדה/הנפוצה ביותר בפועל - זו הפעולה שכל קורא בפועל עושה
// כדי לקרוא תנ"ך. הנתונים נבנים פעם אחת לזיכרון (CacheProvider, תפוגה 12
// שעות - Backend/Tanakh.Infrastructure/Caching/MemoryTanakhCache.cs:9,15),
// כך שאחרי "חימום" הקריאה היא בעיקרה חיפוש במילון בזיכרון, לא IO על דיסק.
// רשימת (ספר, פרק) אמיתיים שנבדקו מול Backend/Tanakh.Api/Data/TanakhStructure.json
// (כותרות הספרים וטווחי הפרקים תואמים בדיוק לנתונים שם).
export const CHAPTERS = [
  { book: 'Genesis', chapter: 1 },
  { book: 'Genesis', chapter: 25 },
  { book: 'Genesis', chapter: 50 },
  { book: 'Exodus', chapter: 1 },
  { book: 'Exodus', chapter: 20 },
  { book: 'Leviticus', chapter: 1 },
  { book: 'Numbers', chapter: 1 },
  { book: 'Deuteronomy', chapter: 1 },
  { book: 'Deuteronomy', chapter: 34 },
  { book: 'Joshua', chapter: 1 },
  { book: 'Psalms', chapter: 1 },
  { book: 'Psalms', chapter: 23 },
  { book: 'Psalms', chapter: 119 },
];

// GET /Tanakh/books/{section} - רשימת הספרים בחטיבה (torah/prophets/writings).
// Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30.
// שימו לב: הערך חייב להיות lowercase באנגלית של שם החטיבה כפי שמופיע ב-JSON
// ("Torah"/"Prophets"/"Writings") כי ההשוואה בקוד היא
// x.section?.ToLower() == section (ה-section שמגיע מהנתיב *לא* עובר ToLower)
// - Backend/Tanakh.Infrastructure/Services/TanakhStructureService.cs:23-24.
export const SECTIONS = ['torah', 'prophets', 'writings'];

// GET /Tanakh/books/main/{book} - מטא-דאטה של ספר בודד לפי כותרת מדויקת.
// Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39.
export const BOOK_TITLES = ['Genesis', 'Exodus', 'Leviticus', 'Numbers', 'Deuteronomy', 'Joshua', 'Psalms'];

// GET /api/v1/system/maintenance - סטטוס מצב תחזוקה (נבדק בכל טעינת אפליקציה
// בצד הלקוח). Backend/Tanakh.Api/Controllers/SystemController.cs:28-33.
export const MAINTENANCE_PATH = '/api/v1/system/maintenance';

// GET /api/v1/system/banner - באנר הודעה ציבורי (מסונן לפי תפוגה).
// Backend/Tanakh.Api/Controllers/SystemController.cs:35-50.
export const BANNER_PATH = '/api/v1/system/banner';

// GET /api/v1/system/flags - דגלי-פיצ'ר ציבוריים.
// Backend/Tanakh.Api/Controllers/SystemController.cs:52-58.
export const FLAGS_PATH = '/api/v1/system/flags';

// GET /health/live - liveness, ללא בדיקת תלויות. Backend/Tanakh.Api/Program.cs:295-298.
export const HEALTH_LIVE_PATH = '/health/live';

// GET /JewishCalendar/getJewishCalendar - Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25.
// אזהרה: המימוש (Backend/Tanakh.Infrastructure/Services/JewishCalendarService.cs:53-58)
// יוצר HttpClient חדש ומבצע קריאה יוצאת אמיתית ל-hebcal.com בכל בקשה, ללא
// שום מטמון בקוד עצמו. בגלל זה נקודת הקצה הזו *לא* נכללת בתמהיל של load.js/
// spike.js/soak.js - עומס עליה בפועל יהיה עומס על שרת צד-שלישי (hebcal.com),
// לא על השרת שלנו, ולכן לא ילמד אותנו דבר על היכולת של השרת שלנו, ועלול
// להזיק לשירות חיצוני. משאירים אותה רק ב-smoke.js (קריאה בודדת) לבדיקת
// זמינות בסיסית.
export const JEWISH_CALENDAR_PATH = '/JewishCalendar/getJewishCalendar';

// ---------------------------------------------------------------------------
// נקודות קצה התלויות בטוקן (opt-in, ראו TEST_MANAGE_TOKEN למעלה).
// ---------------------------------------------------------------------------

// POST /api/v1/reading-progress - **כותבת** ל-DB (upsert progress).
// Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:34-56.
// גוף הבקשה: ReadingProgressRequest{Token,Book,Chapter} -
// Backend/Tanakh.Api/Model/ReadingProgressRequest.cs:3-12 (camelCase ב-JSON,
// כברירת המחדל של ASP.NET Core Web API - אין הגדרת naming policy מפורשת
// ב-Backend/Tanakh.Api/Program.cs, כך ש-System.Text.Json Web Defaults חל).
export const READING_PROGRESS_PATH = '/api/v1/reading-progress';

// GET /api/v1/subscriptions/me?token=... - קריאה בלבד, אך דורשת טוקן תקין.
// Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-127.
export const SUBSCRIPTIONS_ME_PATH = '/api/v1/subscriptions/me';

// ---------------------------------------------------------------------------
// עוזר קטן: בחירה אקראית משקללת. weights הוא מערך של [label, weight].
// ---------------------------------------------------------------------------
export function weightedChoice(weights) {
  const total = weights.reduce((sum, [, w]) => sum + w, 0);
  let r = Math.random() * total;
  for (const [label, w] of weights) {
    if (r < w) return label;
    r -= w;
  }
  return weights[weights.length - 1][0];
}

export function randomFrom(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}
