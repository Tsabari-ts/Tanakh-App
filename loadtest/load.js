// loadtest/load.js
//
// תרחיש עומס "רגיל בשיא": ramp-up ל-300 משתמשים וירטואליים בו-זמנית,
// החזקה ב-300 למשך 5 דקות, ואז ירידה - מדמה את התרחיש הריאלי שהאתר
// נועד לו (שרת production יחיד, עד כמה מאות משתמשים בו-זמנית).
//
// תמהיל התעבורה משוקלל לפי מה שסביר שמשתמש אמיתי עושה באתר קריאת תנ"ך:
// בעיקר קריאת פרקים (הפעולה המרכזית), לעיתים רשימות ספרים/חטיבות, לעיתים
// בדיקות מצב-מערכת (טעינת אפליקציה), ורק אם TEST_MANAGE_TOKEN הוזן -
// גם שמירת התקדמות קריאה (כתיבה) וקריאת מסך "ההגדרות שלי" של מנוי.
// ראו README.md, סעיף "טוקן בדיקה", להסבר איך ומתי משתמשים בכך.
//
// הרצה: k6 run loadtest/load.js
// עם BASE_URL/טוקן: k6 run -e BASE_URL=http://localhost:5000 -e TEST_MANAGE_TOKEN=... loadtest/load.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import {
  BASE_URL,
  CHAPTERS,
  SECTIONS,
  BOOK_TITLES,
  MAINTENANCE_PATH,
  BANNER_PATH,
  FLAGS_PATH,
  READING_PROGRESS_PATH,
  SUBSCRIPTIONS_ME_PATH,
  TEST_MANAGE_TOKEN,
  ENABLE_TOKEN_ENDPOINTS,
  weightedChoice,
  randomFrom,
} from './config.js';

export const options = {
  stages: [
    { duration: '1m', target: 300 }, // ramp-up הדרגתי כדי לא לזעזע את השרת עם קפיצה מיידית
    { duration: '5m', target: 300 }, // ההחזקה בפועל - זה מה שנמדד מול הסף
    { duration: '1m', target: 0 },   // ramp-down מסודר
  ],
  thresholds: {
    // --- שיעור שגיאות כללי ---
    // שרת production יחיד שמשרת עד כמה מאות משתמשים אמורה להיות כמעט
    // תמיד קרוב-ל-0% שגיאות בעומס הזה (זה עדיין בטווח הצפוי, לא ספייק).
    // 1% הוא רף סביר שמאפשר טעויות רשת חולפות בודדות בלי להיכשל על "רעש".
    http_req_failed: ['rate<0.01'],

    // --- p95 לפי סוג נקודת קצה (tags) ---
    // קריאות פרק/מבנה: אחרי חימום המטמון (12 שעות, MemoryTanakhCache) אלו
    // בעיקרן חיפושים בזיכרון + סריאליזציה - לא אמורות לעבור כמה עשרות
    // מילישניות בעומס סביר על מכונה יחידה. 300ms הוא רף נדיב שמשאיר מרווח
    // ל-GC/עומס CPU בו-זמני, ועדיין תופס בעיה אמיתית.
    'http_req_duration{endpoint:chapter_read}': ['p(95)<300'],
    'http_req_duration{endpoint:section_list}': ['p(95)<300'],
    'http_req_duration{endpoint:book_structure}': ['p(95)<300'],
    // קריאות מצב-מערכת חוזרות על עצמן בכל טעינת אפליקציה בצד הלקוח -
    // גם הן אמורות להיות זולות (מטמון 5 דקות ב-AppSettingsService).
    'http_req_duration{endpoint:system_read}': ['p(95)<300'],
    // כתיבה (upsert) ל-Postgres דרך רשת/socket - יותר יקרה מקריאה מהזיכרון,
    // אבל עדיין שאילתה בודדת פשוטה על שרת יחיד; 800ms p95 משאיר מרווח
    // סביר בלי להסתיר בעיה אמיתית בקוד/באינדקס.
    'http_req_duration{endpoint:progress_write}': ['p(95)<800'],
    'http_req_duration{endpoint:subscription_read}': ['p(95)<500'],

    // רף כללי גיבוי, למקרה שתגית ספציפית לא נדגמה מספיק בריצה קצרה.
    http_req_duration: ['p(95)<500'],
  },
};

function readingProgressBody(subscriberChapter) {
  // ReadingProgressRequest{Token,Book,Chapter} - camelCase ב-JSON (ברירת
  // המחדל של ASP.NET Core Web API, אין naming policy מפורש ב-Program.cs).
  // Backend/Tanakh.Api/Model/ReadingProgressRequest.cs:3-12
  return JSON.stringify({
    token: TEST_MANAGE_TOKEN,
    book: subscriberChapter.book,
    chapter: subscriberChapter.chapter,
  });
}

export default function () {
  const pick = weightedChoice([
    ['chapter_read', 60],
    ['section_list', 15],
    ['book_structure', 10],
    ['system_read', 10],
    // שתי הפעולות הבאות תלויות-טוקן ולכן פעילות רק אם TEST_MANAGE_TOKEN
    // סופק; אחרת ה-fallback למטה מפנה אותן לקריאת פרק רגילה.
    ['progress_write', 3],
    ['subscription_read', 2],
  ]);

  if (pick === 'chapter_read' || (!ENABLE_TOKEN_ENDPOINTS && (pick === 'progress_write' || pick === 'subscription_read'))) {
    const chapter = randomFrom(CHAPTERS);
    // GET /Tanakh/books/{book}/{chapter} - Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56
    const res = http.get(`${BASE_URL}/Tanakh/books/${chapter.book}/${chapter.chapter}`, {
      tags: { endpoint: 'chapter_read' },
    });
    check(res, { 'chapter read: status 200': (r) => r.status === 200 });
  } else if (pick === 'section_list') {
    const section = randomFrom(SECTIONS);
    // GET /Tanakh/books/{section} - Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30
    const res = http.get(`${BASE_URL}/Tanakh/books/${section}`, { tags: { endpoint: 'section_list' } });
    check(res, { 'section list: status 200': (r) => r.status === 200 });
  } else if (pick === 'book_structure') {
    const book = randomFrom(BOOK_TITLES);
    // GET /Tanakh/books/main/{book} - Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39
    const res = http.get(`${BASE_URL}/Tanakh/books/main/${book}`, { tags: { endpoint: 'book_structure' } });
    check(res, { 'book structure: status 200': (r) => r.status === 200 });
  } else if (pick === 'system_read') {
    const path = randomFrom([MAINTENANCE_PATH, BANNER_PATH, FLAGS_PATH]);
    const res = http.get(`${BASE_URL}${path}`, { tags: { endpoint: 'system_read' } });
    check(res, { 'system read: status 200': (r) => r.status === 200 });
  } else if (pick === 'progress_write') {
    // POST /api/v1/reading-progress - **כותב** ל-DB. פעיל רק כש-
    // TEST_MANAGE_TOKEN הוזן ומצביע על מנוי אמיתי בסביבת בדיקה חד-פעמית.
    // Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:34-56
    const chapter = randomFrom(CHAPTERS);
    const res = http.post(`${BASE_URL}${READING_PROGRESS_PATH}`, readingProgressBody(chapter), {
      headers: { 'Content-Type': 'application/json' },
      tags: { endpoint: 'progress_write' },
    });
    check(res, { 'progress write: status 200': (r) => r.status === 200 });
  } else if (pick === 'subscription_read') {
    // GET /api/v1/subscriptions/me?token=... - קריאה בלבד, תלוית-טוקן.
    // Backend/Tanakh.Api/Controllers/SubscriptionsController.cs:112-127
    const res = http.get(`${BASE_URL}${SUBSCRIPTIONS_ME_PATH}?token=${encodeURIComponent(TEST_MANAGE_TOKEN)}`, {
      tags: { endpoint: 'subscription_read' },
    });
    check(res, { 'subscription read: status 200': (r) => r.status === 200 });
  }

  // זמן-חשיבה: משתמש אמיתי קורא פרק/מסתכל במסך לפני הבקשה הבאה - בלי זה
  // כל VU היה מפציץ בקשות ברצף, מה שלא מדמה תעבורה אמיתית.
  sleep(1 + Math.random() * 2);
}
