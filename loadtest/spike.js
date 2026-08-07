// loadtest/spike.js
//
// תרחיש "ספייק": קפיצה חדה ל-~1000 משתמשים וירטואליים למשך דקה (התרחיש של
// "מישהו שיתף קישור לקבוצת וואטסאפ גדולה"), ואז ירידה חדה בחזרה. זהו 3-5x
// מעל תרחיש ה-load.js (300), במכוון - לבדוק את נקודת השבירה על שרת
// production יחיד, לא רק את התפקוד התקין.
//
// שימו לב: בכוונה נבדקות כאן *רק* נקודות הקצה הקריאות-בלבד/מהזיכרון (קריאת
// פרק, רשימות ספרים/חטיבות) - לא נקודות קצה שכותבות ל-DB ולא נקודות קצה
// עם הגבלת קצב (rate limiting) כמו הרשמה/OTP, כי אלה יחזירו המון 429
// (Backend/Tanakh.Api/Program.cs:109-155) שרק "יצבעו" את התוצאות בכשלים
// צפויים-מראש ולא ילמדו אותנו כלום על יכולת השרת בעומס.
//
// הרצה: k6 run loadtest/spike.js
// עם BASE_URL מותאם: k6 run -e BASE_URL=http://localhost:5000 loadtest/spike.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, CHAPTERS, SECTIONS, BOOK_TITLES, weightedChoice, randomFrom } from './config.js';

export const options = {
  stages: [
    { duration: '10s', target: 1000 }, // הקפיצה החדה עצמה - העלייה המהירה היא הבדיקה
    { duration: '1m', target: 1000 },  // החזקה בשיא
    { duration: '20s', target: 0 },    // ירידה מהירה - לבדוק שהשרת מתאושש ולא נשאר "תקוע"
  ],
  thresholds: {
    // "כשל טוטלי" מוגדר כאן כאחוז שגיאות מעל 20% - כלומר יותר מחמישית
    // מהבקשות נכשלות (סטטוס>=400/timeout/connection refused). מתחת לרף
    // הזה השרת "מתכופף אבל לא נשבר" - תגובות איטיות/כמה 5xx בודדים
    // מתקבלים על הדעת בספייק כזה על שרת יחיד; מעליו זו למעשה נפילה.
    // אם התוצאה בפועל היא 5-20% שגיאות - הריצה "עוברת" פורמלית אך שווה
    // בדיקה ידנית (זה כבר לא "בריא", גם אם לא "מת").
    http_req_failed: ['rate<0.20'],

    // p95 בספייק מותר להידרדר משמעותית לעומת load.js (300ms->5000ms) -
    // אנחנו בודקים הישרדות, לא UX תקין. מעבר לזה זה כבר לא "עדיין עובד".
    http_req_duration: ['p(95)<5000'],
  },
};

export default function () {
  const pick = weightedChoice([
    ['chapter_read', 70],
    ['section_list', 15],
    ['book_structure', 15],
  ]);

  if (pick === 'chapter_read') {
    const chapter = randomFrom(CHAPTERS);
    // GET /Tanakh/books/{book}/{chapter} - Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56
    const res = http.get(`${BASE_URL}/Tanakh/books/${chapter.book}/${chapter.chapter}`, {
      tags: { endpoint: 'chapter_read' },
      timeout: '10s',
    });
    check(res, { 'chapter read: status 200': (r) => r.status === 200 });
  } else if (pick === 'section_list') {
    const section = randomFrom(SECTIONS);
    // GET /Tanakh/books/{section} - Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30
    const res = http.get(`${BASE_URL}/Tanakh/books/${section}`, {
      tags: { endpoint: 'section_list' },
      timeout: '10s',
    });
    check(res, { 'section list: status 200': (r) => r.status === 200 });
  } else {
    const book = randomFrom(BOOK_TITLES);
    // GET /Tanakh/books/main/{book} - Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39
    const res = http.get(`${BASE_URL}/Tanakh/books/main/${book}`, {
      tags: { endpoint: 'book_structure' },
      timeout: '10s',
    });
    check(res, { 'book structure: status 200': (r) => r.status === 200 });
  }

  // בספייק אמיתי משתמשים לא ממתינים הרבה בין לחיצות (כולם מגיעים ומקליקים
  // כמעט מיד אחרי פתיחת הקישור המשותף) - זמן חשיבה קצר יותר מ-load.js.
  sleep(0.2 + Math.random() * 0.5);
}
