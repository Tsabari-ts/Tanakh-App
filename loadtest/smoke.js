// loadtest/smoke.js
//
// "עשן": 5 משתמשים וירטואליים, דקה אחת, כל אחד עובר פעם אחת (בלולאה, כי
// הריצה נמדדת בזמן ולא במספר איטרציות) על נקודות הקצה הציבוריות המרכזיות,
// כדי לוודא שהאפליקציה בכלל עובדת לפני שמריצים עומס אמיתי (load.js/spike.js/
// soak.js). לא בודק ביצועים - רק "זה עונה ולא זורק שגיאות".
//
// הרצה: k6 run loadtest/smoke.js
// עם BASE_URL מותאם: k6 run -e BASE_URL=http://localhost:5000 loadtest/smoke.js
//
// כל הנתיבים כאן מאומתים מול קוד ה-controller בפועל - ראו הפניות קובץ:שורה
// בתוך loadtest/config.js ובטבלה המלאה ב-README.md.

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
  HEALTH_LIVE_PATH,
  JEWISH_CALENDAR_PATH,
  randomFrom,
} from './config.js';

export const options = {
  vus: 5,
  duration: '1m',
  thresholds: {
    // כל שגיאה (סטטוס>=400 או timeout) בסבב עשן כזה קטן אומרת שמשהו בסיסי
    // שבור - נכשיל את כל הריצה אם קורה אפילו כשל אחד.
    http_req_failed: ['rate<=0'],
    checks: ['rate>=1'],
    http_req_duration: ['p(95)<2000'],
  },
};

export default function () {
  const chapter = randomFrom(CHAPTERS);
  const section = randomFrom(SECTIONS);
  const book = randomFrom(BOOK_TITLES);

  // GET /Tanakh/books/{book}/{chapter} - Backend/Tanakh.Api/Controllers/TanakhController.cs:45-56
  let res = http.get(`${BASE_URL}/Tanakh/books/${chapter.book}/${chapter.chapter}`, {
    tags: { endpoint: 'chapter_read' },
  });
  check(res, { 'chapter read: status 200': (r) => r.status === 200 });

  // GET /Tanakh/books/{section} - Backend/Tanakh.Api/Controllers/TanakhController.cs:26-30
  res = http.get(`${BASE_URL}/Tanakh/books/${section}`, { tags: { endpoint: 'section_list' } });
  check(res, { 'section list: status 200': (r) => r.status === 200 });

  // GET /Tanakh/books/main/{book} - Backend/Tanakh.Api/Controllers/TanakhController.cs:35-39
  res = http.get(`${BASE_URL}/Tanakh/books/main/${book}`, { tags: { endpoint: 'book_structure' } });
  check(res, { 'book structure: status 200': (r) => r.status === 200 });

  // GET /api/v1/system/maintenance - Backend/Tanakh.Api/Controllers/SystemController.cs:28-33
  res = http.get(`${BASE_URL}${MAINTENANCE_PATH}`, { tags: { endpoint: 'system_read' } });
  check(res, { 'maintenance: status 200': (r) => r.status === 200 });

  // GET /api/v1/system/banner - Backend/Tanakh.Api/Controllers/SystemController.cs:35-50
  res = http.get(`${BASE_URL}${BANNER_PATH}`, { tags: { endpoint: 'system_read' } });
  check(res, { 'banner: status 200': (r) => r.status === 200 });

  // GET /api/v1/system/flags - Backend/Tanakh.Api/Controllers/SystemController.cs:52-58
  res = http.get(`${BASE_URL}${FLAGS_PATH}`, { tags: { endpoint: 'system_read' } });
  check(res, { 'flags: status 200': (r) => r.status === 200 });

  // GET /health/live - Backend/Tanakh.Api/Program.cs:295-298
  res = http.get(`${BASE_URL}${HEALTH_LIVE_PATH}`, { tags: { endpoint: 'health' } });
  check(res, { 'health/live: status 200': (r) => r.status === 200 });

  // GET /JewishCalendar/getJewishCalendar - Backend/Tanakh.Api/Controllers/JewishCalendarController.cs:20-25
  // נכלל כאן פעם אחת בלבד (עומס נמוך, 5 VUs) כי המימוש פונה בפועל ל-hebcal.com
  // ללא מטמון - ראו האזהרה המלאה ב-loadtest/config.js ליד JEWISH_CALENDAR_PATH.
  // לא נכלל ב-load.js/spike.js/soak.js מאותה סיבה.
  res = http.get(`${BASE_URL}${JEWISH_CALENDAR_PATH}`, { tags: { endpoint: 'jewish_calendar' } });
  check(res, { 'jewish calendar: status 200': (r) => r.status === 200 });

  sleep(1);
}
