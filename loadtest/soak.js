// loadtest/soak.js
//
// תרחיש "השרייה" (soak): 100 משתמשים וירטואליים קבועים למשך 30 דקות רצוף,
// עם תמהיל תעבורה חוזר-על-עצמו (לא ריצה חד-פעמית) - נועד לחשוף דליפות
// זיכרון/דליפות חיבורים (DB connections, HttpClient handles וכו') שריצה
// קצרה כמו load.js (5 דקות) לא תספיק לחשוף.
//
// איך מזהים דליפה בתוצאות:
// - **p95/p99 של http_req_duration שעולה בהדרגה** ככל שהריצה מתקדמת (לא
//   קופצת פתאום, אלא "זוחלת" מעלה עם הזמן) - זה הסימן הקלאסי לדליפת
//   זיכרון/חיבורים: כל בקשה מותירה קצת יותר "בלגן" מהקודמת, וה-GC/pool
//   עובדים קשה יותר ויותר ככל שעובר הזמן.
// - **http_req_failed שמתחיל אפסי ועולה לקראת סוף הריצה** (למשל אחרי 20+
//   דקות) - יכול להעיד על אזילת pool של חיבורי Postgres/thread-pool.
// - k6 מדפיס בסוף הריצה רק סיכום מצטבר (p95 על פני כל 30 הדקות) - זה
//   *לא מספיק* כדי לראות "זחילה". כדי לראות מגמה לאורך זמן:
//     1) הריצו עם --out json=results.json (או --out csv=results.csv) ואז
//        חתכו את הקובץ לחלונות של 5 דקות והשוו p95 בין החלונות; או
//     2) אם יש לכם k6 Cloud/Grafana - צפו בגרף ה-trend של http_req_duration
//        לאורך זמן ישירות (moving p95).
// - במקביל לריצה, כדאי לצפות ב-RSS/working-set של תהליך ה-.NET בשרת עצמו
//   (למשל `docker stats` אם רץ בקונטיינר, או Task Manager/`dotnet-counters`)
//   - עלייה מתמדת בזיכרון תהליך במקביל לעלייה ב-p95 היא אישוש חזק לדליפה.
//
// הרצה: k6 run loadtest/soak.js
// עם BASE_URL/פלט לניתוח מגמה: k6 run -e BASE_URL=http://localhost:5000 --out json=soak-results.json loadtest/soak.js

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
  TEST_MANAGE_TOKEN,
  ENABLE_TOKEN_ENDPOINTS,
  weightedChoice,
  randomFrom,
} from './config.js';

export const options = {
  stages: [
    { duration: '2m', target: 100 },  // ramp-up מתון
    { duration: '30m', target: 100 }, // ההשרייה עצמה - 30 דקות של עומס קבוע
    { duration: '2m', target: 0 },    // ramp-down
  ],
  thresholds: {
    // אותם ספים כמו load.js (עומס נמוך משמעותית - 100 VUs - אז הציפייה
    // לזמני תגובה ולשיעור שגיאות נשארת "בריאה" לכל אורך ה-30 דקות; אם
    // הריצה נכשלת רק לקראת הסוף, זה בדיוק סימן הדליפה שמחפשים).
    http_req_failed: ['rate<0.01'],
    'http_req_duration{endpoint:chapter_read}': ['p(95)<300'],
    'http_req_duration{endpoint:system_read}': ['p(95)<300'],
    'http_req_duration{endpoint:progress_write}': ['p(95)<800'],
    http_req_duration: ['p(95)<500'],
  },
};

function readingProgressBody(chapter) {
  // ReadingProgressRequest{Token,Book,Chapter} - Backend/Tanakh.Api/Model/ReadingProgressRequest.cs:3-12
  return JSON.stringify({ token: TEST_MANAGE_TOKEN, book: chapter.book, chapter: chapter.chapter });
}

export default function () {
  const pick = weightedChoice([
    ['chapter_read', 65],
    ['section_list', 10],
    ['book_structure', 10],
    ['system_read', 10],
    // כתיבה חוזרת ל-DB היא בדיוק סוג הפעולה שחושפת דליפת connection pool
    // לאורך זמן; פעילה רק אם TEST_MANAGE_TOKEN סופק (ראו README).
    ['progress_write', 5],
  ]);

  if (pick === 'chapter_read' || (!ENABLE_TOKEN_ENDPOINTS && pick === 'progress_write')) {
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
    // POST /api/v1/reading-progress - **כותב** ל-DB בכל קריאה, שוב ושוב
    // לאורך 30 דקות - Backend/Tanakh.Api/Controllers/ReadingProgressController.cs:34-56
    const chapter = randomFrom(CHAPTERS);
    const res = http.post(`${BASE_URL}${READING_PROGRESS_PATH}`, readingProgressBody(chapter), {
      headers: { 'Content-Type': 'application/json' },
      tags: { endpoint: 'progress_write' },
    });
    check(res, { 'progress write: status 200': (r) => r.status === 200 });
  }

  sleep(1 + Math.random() * 2);
}
