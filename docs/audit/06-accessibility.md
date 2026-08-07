# מיפוי נגישות (Accessibility) — Frontend

מסמך זה הוא מיפוי עובדתי בלבד של יישום הנגישות בקוד ה-Frontend נכון למועד הכתיבה. אין בו המלצות, דירוגי תאימות ל-WCAG או קביעות "מה צריך לתקן" — רק תיאור של מה שקיים בפועל, עם ציטוט קובץ+שורה לכל טענה.

## 1. תקן הנגישות שהאתר מצהיר עליו, והיכן ההצהרה מופיעה

הצהרת הנגישות מוגדרת כמחרוזת HTML קבועה בקבוע `ACCESSIBILITY_HTML` בקובץ `Frontend/src/app/shared/legal/legal-content-html.ts:162-204`. הטקסט הרלוונטי:

> "האפליקציה הונגשה בהתאם לתקן הישראלי ת"י 5568, ברמת התאמה AA, ובהתבסס על הנחיות WCAG 2.2 ברמה AA."
> — `Frontend/src/app/shared/legal/legal-content-html.ts:166`

שדות נוספים בהצהרה עצמה:
- תאריך הנגשה: 02/08/2026 — `Frontend/src/app/shared/legal/legal-content-html.ts:180`
- "מועד הבדיקה האחרון": 02/08/2026 — `Frontend/src/app/shared/legal/legal-content-html.ts:182-183`
- "הבדיקה בוצעה על ידי": "בדיקה פנימית (בדיקות אוטומטיות axe-core ו-Lighthouse; בדיקת מקלדת ומבנה סמנטי ידנית)" — `Frontend/src/app/shared/legal/legal-content-html.ts:185-186`
- רשימת "חלקים שאינם נגישים במלואם" (חלונות "ברוכים הבאים"/אישור קריאת פרק שאינם נסגרים ב-Escape) — `Frontend/src/app/shared/legal/legal-content-html.ts:188-193`
- פרטי רכז נגישות (תומר צברי, Tanakhdev@gmail.com) — `Frontend/src/app/shared/legal/legal-content-html.ts:195-201`

מטא-דאטה של המסמך (כותרת "הצהרת נגישות", מזהה `accessibility`, תאריך עדכון אחרון 2026-08-02) מוגדרת ב-`Frontend/src/app/shared/legal/legal-content.ts:39-46`.

המסמך נפתח כדיאלוג (לא כעמוד/route ייעודי) על ידי `LegalDialogService.open('accessibility')` שמוגדר ב-`Frontend/src/app/shared/legal/legal-dialog.service.ts:21-38`, ומיוצג בקומפוננטה `LegalModalComponent` (`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts`). נקודות הפעלה:
- קישור "הצהרת הנגישות" בפוטר האתר — `Frontend/src/app/app.component.html:36` (קורא ל-`openLegal('accessibility')`, מוגדר ב-`Frontend/src/app/app.component.ts:86-88`).
- קישור "הצהרת הנגישות" בתוך תפריט הנגישות עצמו — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:72` (קורא ל-`openStatement()` המוגדר ב-`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts:62-65`).
- קישור עומק (`?a11y=statement`) שנבדק ב-`afterNextRender` בבנאי של `AppComponent` — `Frontend/src/app/app.component.ts:61-64` (וגם `?legal=terms`/`?legal=privacy` באותו בלוק, שורות 66-69).

בקובץ `docs/a11y-manual-test.md:45` מוזכר שם קובץ `AccessibilityStatementDialogComponent` ונתיב `Frontend/src/app/shared/a11y/accessibility-statement-dialog/accessibility-statement-dialog.component.html` — נבדק וקובץ/תיקייה כזו **אינה קיימת** בקוד הנוכחי. תפקידה מולא בפועל על ידי `LegalModalComponent` הגנרי (`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts`) עם תוכן ה-HTML של `ACCESSIBILITY_HTML`. פירוט בסעיף "לא ידוע / דורש אימות" למטה.

## 2. ווידג'ט/ספריית נגישות: צד שלישי או פיתוח פנימי

**זהו פיתוח פנימי (custom-built) — אין ספריית/ווידג'ט נגישות של צד שלישי.**

ראיות:
- `Frontend/package.json:13-24` (dependencies) ו-`Frontend/package.json:25-38` (devDependencies) — אין שום חבילה מסוג ווידג'ט נגישות מסחרי (למשל UserWay, AccessiBe, EqualWeb, Nagish-li, AudioEye, Monsido, Siteimprove). כל ה-dependencies הן חבילות Angular רשמיות, `gematriya`, `rxjs`, `tslib`; ה-devDependencies הן כלי בדיקה/בנייה (`@axe-core/playwright`, `@lhci/cli`, `@playwright/test`, `stylelint` וכו').
- חיפוש בקוד המקור (`Frontend`) אחר שמות ידועים של ספקי ווידג'טי נגישות (userway, accessibe, nagishli, equalweb, monsido, audioeye, siteimprove) — 0 תוצאות.
- `Frontend/src/index.html` אינו טוען שום סקריפט חיצוני של נגישות; הסקריפט המוטבע היחיד (`Frontend/src/index.html:34-52`) הוא קוד JS מקומי (inline) שמיישם את הגדרות הנגישות השמורות מ-localStorage לפני הרינדור הראשון (מניעת "הבהוב" חזותי) — הוא קורא ישירות למפתחות ולמחלקות CSS שמוגדרות ב-`Frontend/src/app/core/a11y/a11y.model.ts` וב-`Frontend/src/app/core/a11y/a11y.service.ts`, כפי שמצוין בהערה בשורות 30-33.
- מימוש הלוגיקה עצמה נמצא בתיקיות `Frontend/src/app/core/a11y/` (שירות + מודל) ו-`Frontend/src/app/shared/a11y/` (קומפוננטת ה-UI, קישור הדילוג), כלומר קוד TypeScript/HTML/SCSS רגיל של האפליקציה עצמה, לא הזרקה חיצונית.

## 3. תפריט/ווידג'ט הנגישות: קבצים, אפשרויות ואופן המימוש הטכני

### קבצים מרכזיים
- מודל נתונים: `Frontend/src/app/core/a11y/a11y.model.ts`
- שירות (state + החלת השינויים על ה-DOM): `Frontend/src/app/core/a11y/a11y.service.ts`
- קומפוננטת ה-UI (הכפתור הצף + הפאנל): `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts` / `.html` / `.scss`
- קובץ עיצוב הכללים שהמחלקות מפעילות: `Frontend/src/styles/_a11y-modes.scss`
- כללי CSS גלובליים נלווים (focus-visible, `prefers-reduced-motion`, `.visually-hidden`): `Frontend/src/styles/_a11y-utils.scss`

השירות (`A11yService`) מבוסס Angular `signal` (`Frontend/src/app/core/a11y/a11y.service.ts:15`) ופועל דרך `effect()` (`Frontend/src/app/core/a11y/a11y.service.ts:31-39`) שמריץ `apply()` (עדכון ה-DOM) ו-`save()` (שמירה ל-localStorage) בכל שינוי הגדרות.

### רשימת האפשרויות ואופן המימוש (כולן מוגדרות ב-`A11ySettings`, `Frontend/src/app/core/a11y/a11y.model.ts:4-14`)

| אפשרות | איך מוצגת ב-UI | איך ממומשת טכנית |
|---|---|---|
| גודל טקסט (`fontScale`, ערכים 1/1.15/1.3/1.5) | קבוצת רדיו בפאנל — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:32-43` | CSS custom property `--a11y-font-scale` מוגדר על `<html>` ב-`Frontend/src/app/core/a11y/a11y.service.ts:78` (`html.style.setProperty(...)`), ונצרך בחישובי `font-size` ב-`Frontend/src/styles/_tokens.scss:131-138` (למשל `--font-size-base: calc(clamp(...) * var(--a11y-font-scale))`) |
| ניגודיות (`contrast`: default/high/inverted) | קבוצת רדיו — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:45-58` | מחלקות CSS על `<html>`: `classList.toggle('a11y-high-contrast', ...)` ו-`classList.toggle('a11y-inverted-contrast', ...)` ב-`Frontend/src/app/core/a11y/a11y.service.ts:79-80`; הכללים עצמם ב-`Frontend/src/styles/_a11y-modes.scss:26-58` (דריסת משתני צבע CSS) |
| גווני אפור (`grayscale`) | כפתור toggle — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:62` | `classList.toggle('a11y-grayscale', ...)` — `Frontend/src/app/core/a11y/a11y.service.ts:81`; מיושם רק על `.app-shell` (לא על `<html>`/`<body>`) כדי לא לשבור `position: fixed` — `Frontend/src/styles/_a11y-modes.scss:60-68` |
| הדגשת קישורים (`highlightLinks`) | כפתור toggle — `...component.html:63` | `classList.toggle('a11y-highlight-links', ...)` — `a11y.service.ts:82`; כלל CSS ב-`Frontend/src/styles/_a11y-modes.scss:70-80` (underline + outline על `a`) |
| גופן קריא (`readableFont`) | כפתור toggle — `...component.html:64` | `classList.toggle('a11y-readable-font', ...)` — `a11y.service.ts:83`; משנה משתני CSS `--font-body`/`--letter-spacing`/`--word-spacing`/`--line-height-body` — `Frontend/src/styles/_a11y-modes.scss:82-88` |
| ריווח טקסט מוגבר (`textSpacing`) | כפתור toggle — `...component.html:65` | `classList.toggle('a11y-text-spacing', ...)` — `a11y.service.ts:84`; משנה `--line-height-body`/`--letter-spacing`/`--word-spacing`/`--paragraph-spacing` — `Frontend/src/styles/_a11y-modes.scss:90-96` |
| עצירת אנימציות (`noMotion`) | כפתור toggle — `...component.html:66` | `classList.toggle('a11y-no-motion', ...)` — `a11y.service.ts:85`; מאפס `animation-duration`/`transition-duration`/`scroll-behavior` — `Frontend/src/styles/_a11y-modes.scss:98-105`. בנוסף קיים מנגנון עצמאי מבוסס מדיה-קוורי של המערכת (`@media (prefers-reduced-motion: reduce)`) — `Frontend/src/styles/_a11y-utils.scss:31-39` |
| סמן עכבר מוגדל (`bigCursor`) | כפתור toggle — `...component.html:67` | `classList.toggle('a11y-big-cursor', ...)` — `a11y.service.ts:86`; `cursor` מוגדר כ-SVG data-URI מוטבע — `Frontend/src/styles/_a11y-modes.scss:107-116` |
| סרגל קריאה (`readingRuler`) | כפתור toggle — `...component.html:68` | **לא** מוגש כמחלקת CSS על `<html>` (כפי שמצוין בהערה ב-`Frontend/src/app/core/a11y/a11y.service.ts:43-44`), אלא כאלמנט `<div class="a11y-ruler">` שנוצר ומנוהל דינמית ע"י `enableReadingRuler()`/`disableReadingRuler()` — `Frontend/src/app/core/a11y/a11y.service.ts:95-111`. מיקומו האנכי מתעדכן דרך משתנה CSS `--ruler-y` בהאזנה ל-`pointermove` ו-`focusin` — `Frontend/src/app/core/a11y/a11y.service.ts:20-29`; עיצוב ה-gradient ב-`Frontend/src/styles/_a11y-modes.scss:118-132` |
| איפוס כל ההגדרות | כפתור "איפוס כל ההגדרות" — `...component.html:74` | `reset()` מחזיר את ה-signal לערכי ברירת המחדל `A11Y_DEFAULTS` — `Frontend/src/app/core/a11y/a11y.service.ts:67-70`, `Frontend/src/app/core/a11y/a11y.model.ts:16-26` |

כל שינוי מלווה בהכרזה (announce) לקוראי מסך דרך `LiveAnnouncer` של Angular CDK (`aria-live` דינמי שה-CDK מנהל בעצמו): `setFontScale`, `setContrast`, `toggle` ו-`reset` — `Frontend/src/app/core/a11y/a11y.service.ts:41-70`.

### מבנה הפאנל עצמו
הפאנל (`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:17-77`) הוא `<div role="dialog" aria-modal="true">` (שורות 20-21) עם `cdkTrapFocus` ו-`[cdkTrapFocusAutoCapture]="true"` מ-`@angular/cdk/a11y` (שורות 23-24, וייבוא `A11yModule` ב-`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts:2,15`), וסגירה במקש Escape דרך `(keydown.escape)="close()"` (שורה 25). ניווט בין אפשרויות רדיו (גודל טקסט/ניגודיות) בעזרת חצים/Home/End ממומש ידנית בפונקציה `onRadioKeydown` — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts:67-83`.

הכפתור הצף (`.a11y-fab`) ממוקם ב-`inset-inline-start`/`inset-block-end` (בפריסת RTL = פינה שמאלית תחתונה) — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.scss:4-7`, תואם לתיאור בהצהרת הנגישות ("הכפתור הצף בפינה השמאלית התחתונה", `legal-content-html.ts:176`).

## 4. שמירת העדפות (persistence)

**כן — נשמר ב-`localStorage`, לא בעוגייה ולא במסד נתונים.**

- מפתח האחסון: `'tanach.a11y.v1'` — מוגדר כקבוע `A11Y_STORAGE_KEY` ב-`Frontend/src/app/core/a11y/a11y.model.ts:28`.
- כתיבה: `A11yService.save()` — `Frontend/src/app/core/a11y/a11y.service.ts:122-127` (`localStorage.setItem(A11Y_STORAGE_KEY, JSON.stringify(s))`, בתוך try/catch שמתעלם משגיאה במצב גלישה פרטית).
- קריאה: `A11yService.load()` — `Frontend/src/app/core/a11y/a11y.service.ts:113-120` (`localStorage.getItem(A11Y_STORAGE_KEY)`, ממוזג עם `A11Y_DEFAULTS`).
- קריאה נוספת (מוקדמת, לפני האתחול של Angular) כדי למנוע הבהוב ויזואלי: הסקריפט המוטבע ב-`Frontend/src/index.html:29-53` קורא את אותו מפתח (`localStorage.getItem('tanach.a11y.v1')`, שורה 36) ומחיל מחלקות/משתני CSS על `<html>` לפני הרינדור הראשון.
- הערה: מפתח נפרד `'tanach.theme'` (מצב כהה/בהיר, נקרא ב-`Frontend/src/index.html:38`) קיים גם כן, אך הוא שייך ל-`ThemeService` הכללי של האתר ולא לתפריט הנגישות (אין לו קשר ל-`A11ySettings`).

## 5. שימוש ב-ARIA — ספירה לפי סוג תכונה

הספירות להלן הן ל-attributes בפועל שמתרנדרים ל-DOM (כולל bindings דינמיים מסוג `[attr.aria-*]`), בכל `Frontend/src/app` (קבצי `.html` וטמפלטים inline בקבצי `.ts`). תכונות `i18n-aria-label` (תכונת קומפיילר, לא מתרנדרת) **לא** נספרות כאן — הן מפורטות בנפרד בסעיף 10.

| תכונה | ספירה | קבצים עיקריים |
|---|---|---|
| `aria-label` (כולל `[attr.aria-label]` דינמי) | 27 (24 סטטי + 3 דינמי) | ראו פירוט מלא למטה |
| `aria-hidden` | 21 | `tts-player.component.html` (5), `entrance.component.html` (3), `settings.component.html` (3), `app.component.html` (2), ועוד |
| `role` | 41 | פירוט לפי ערך למטה |
| `aria-pressed` (כולל `[attr.aria-pressed]`) | 8 | `accessibility-widget.component.html` (7), `tts-player.component.html` (1) |
| `aria-checked` (כולל `[attr.aria-checked]`) | 7 | `accessibility-widget.component.html` (4), `settings.component.html` (2), `subscribe.component.html` (1) |
| `aria-invalid` (כולן `[attr.aria-invalid]`) | 5 | `subscribe.component.html` בלבד (שורות 36, 48, 66, 80, 104) |
| `aria-expanded` | 1 | `accessibility-widget.component.html:4` |
| `aria-controls` | 1 | `accessibility-widget.component.html:5` |
| `aria-labelledby` | 1 | `accessibility-widget.component.html:22` |
| `aria-modal` | 1 | `accessibility-widget.component.html:21` |
| `aria-live` (סטטי ב-template) | 1 | `app.component.html:6` (`aria-live="assertive"` על toast שגיאה) |
| `aria-valuemin` / `aria-valuemax` | 3 + 3 | `subscribe.component.html:116`, `read-permission.component.html:21,52` (progressbar) |
| `aria-valuenow` (כולן `[attr.aria-valuenow]`) | 3 | `subscribe.component.html:117`, `read-permission.component.html:22,53` |
| `aria-valuetext` | 1 | `tts-player.component.html:43` (input range למהירות ההקראה) |
| `aria-describedby`, `aria-current`, `aria-disabled`, `aria-required`, `aria-atomic`, `aria-busy`, `aria-haspopup`, `aria-selected` | 0 | לא נמצא אף שימוש |

פירוט 24 המופעים הסטטיים של `aria-label="..."`: `app.component.html:8,23,28`; `tts-player.component.html:1,24,29,34`; `accessibility-widget.component.html:7,29,34,47`; `welcome-modal.component.html:9`; `chapter.component.html:6,9,25`; `subscribe.component.html:7,118`; `settings.component.html:12,32,38,54`; `read-permission.component.html:13,39,54`. 3 המופעים הדינמיים (`[attr.aria-label]`): `tts-player.component.html:15`, `legal-modal.component.html:18,23`.

פירוט 41 מופעי `role`: `role="alert"` — 12 (`app.component.html:6`, `error-screen.component.html:3,12`, `maintenance-screen.component.html:1`, `subscribe.component.html:40,53,56,59,73,90,96,108`); `role="status"` — 12 (`app.component.html:13`, `cookie-banner.component.ts:10`, `tts-player.component.html:3,6`, `announcement-banner.component.html:3`, `read-permission.component.html:59`, `subscribe.component.html:123,155,162`, `entrance.component.html:2,21,51`); `role="radio"` — 4 (`accessibility-widget.component.html:37,48,51,54`); `role="switch"` — 3 (`settings.component.html:12,54`, `subscribe.component.html:7`); `role="progressbar"` — 3 (`subscribe.component.html:116`, `read-permission.component.html:21,52`); `role="radiogroup"` — 2 (`accessibility-widget.component.html:34,47`); `role="banner"`, `role="contentinfo"`, `role="dialog"`, `role="region"`, `role="group"` — 1 כל אחד (`app.component.html:21`, `app.component.html:33`, `accessibility-widget.component.html:20`, `legal-modal.component.html:23`, `tts-player.component.html:1`, בהתאמה).

**ריכוז ARIA הגבוה ביותר**: `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html` (פאנל הנגישות עצמו) — 4 `aria-label`, `aria-expanded`, `aria-controls`, `aria-labelledby`, `aria-modal`, `role="dialog"`, 2× `role="radiogroup"`, 4× `role="radio"`, 4× `aria-checked`, 7× `aria-pressed`. מיד אחריו `Frontend/src/app/components/subscribe/subscribe.component.html` (טופס הרשמה לתזכורות) — 5× `role="alert"`, `role="progressbar"`, `aria-valuemin/max/now`, 5× `aria-invalid`, `aria-checked`, `aria-label`, ו-`Frontend/src/app/components/read-permission/read-permission.component.html` עם דפוסי progressbar זהים.

## 6. ניווט מקלדת

- **קישור "דילוג לתוכן"**: `SkipLinkComponent` — `Frontend/src/app/shared/a11y/skip-link/skip-link.component.ts:6-9` (קישור ל-`#main-content`, עם `focusMain()` שקורא ל-`main?.focus()` בשורה 34). מוצב ראשון בעץ הרינדור של `AppComponent` — `Frontend/src/app/app.component.html:3`. יעד הקישור: `<main id="main-content" tabindex="-1">` — `Frontend/src/app/app.component.html:50`.
- **`tabindex`**: נמצא ב-`Frontend/src/app/app.component.html:50` (`tabindex="-1"` על `<main>`), ב-4 מופעים ב-`accessibility-widget.component.html:39,49,52,55` (roving tabindex בקבוצות הרדיו — `0`/`-1` בהתאם לבחירה הנוכחית), ב-`legal-modal.component.html:23` (`tabindex="0"` על גוף הדיאלוג הגלילי), ובקוד דינמי ב-`Frontend/src/app/core/a11y/route-focus.service.ts:47-48` (`target.setAttribute('tabindex', '-1')` אם לכותרת `h1` של העמוד החדש אין `tabindex` עדיין).
- **`FocusTrap`/`cdkTrapFocus`**: המקום היחיד בקוד האפליקציה עם `cdkTrapFocus` מפורש הוא פאנל הנגישות — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:23-24` (`cdkTrapFocus`, `[cdkTrapFocusAutoCapture]="true"`). דיאלוגים אחרים (welcome-modal, read-permission, subscribe, legal-modal) בנויים על `MatDialog`/`MatDialogContainer` של Angular Material, שמיישם focus trap משלו באופן מובנה — הערה מפורשת בקוד: `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html:2` ("No role=\"dialog\"/aria-modal/cdkTrapFocus here: MatDialogContainer...").
- **החזרת פוקוס בסגירת הפאנל**: `close(returnFocus = true)` מחזיר פוקוס לכפתור ה-FAB — `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts:39-44` (`this.fab().nativeElement.focus()`).
- **ניהול פוקוס במעבר בין נתיבים (routes)**: `RouteFocusService` — `Frontend/src/app/core/a11y/route-focus.service.ts` — בכל `NavigationEnd` (מלבד הניווט הראשון בעליית האפליקציה) מאתר את `main h1` או, בהיעדרו, את `#main-content` (שורות 40-43), מוסיף `tabindex="-1"` במידת הצורך, קורא ל-`.focus({ preventScroll: true })` (שורה 50) ומכריז את הטקסט דרך `LiveAnnouncer` (שורות 53-56).
- **חלון "ברוכים הבאים" (`welcome-modal`)**: `Frontend/src/app/components/welcome-modal/welcome-modal.component.ts` — מבוסס `MatDialogRef` בלבד (שורה 3), ללא `cdkTrapFocus`/`tabindex`/קריאות `.focus()` ידניות בקובץ עצמו; לפי ההצהרה בסעיף 1 (`legal-content-html.ts:190-193`) חלון זה נסגר רק בכפתור הסגירה הייעודי ולא ב-Escape — החלטה מכוונת. כפתור הסגירה מתויג עם `aria-label="סגירה"` — `Frontend/src/app/components/welcome-modal/welcome-modal.component.html:9`.
- **מעטפת הניהול (`admin-shell`)**: `Frontend/src/app/admin/shell/admin-shell.component.html` — קישורי ניווט רגילים (`<a routerLink>`, שורות 25-29) ללא `tabindex`, `aria-*` או ניהול פוקוס מיוחד כלשהו (ראו גם סעיף 9 — לוח הניהול אינו מכיל תכונות `i18n`).
- בדיקת קצה-לקצה אוטומטית לדילוג-לתוכן קיימת ב-`Frontend/e2e/a11y.spec.ts:153-159` ("נגישות · דילוג לתוכן הראשי עובד במקלדת" — Tab ואז Enter).

## 7. RTL וכיווניות

- כיוון הדף כולו נקבע סטטית ב-`Frontend/src/index.html:2` — `<html dir="rtl" lang="he">`. אין קוד שמשנה `dir` דינמית בזמן ריצה (לא נמצאו קריאות `setAttribute('dir', ...)` בכל `Frontend/src/app`).
- מסכי שגיאה/תחזוקה מגדירים שוב `dir="rtl" lang="he"` ברמת האלמנט: `Frontend/src/app/shared/error-screen/error-screen.component.html:3,12`, `Frontend/src/app/shared/maintenance-screen/maintenance-screen.component.html:1`.
- אלמנט `<div dir="auto"></div>` ריק בתחילת התבנית הראשית — `Frontend/src/app/app.component.html:1` (ללא הערה מסבירה בקוד; מטרתו המדויקת לא אומתה — ראו סעיף "לא ידוע").
- **לא נמצאו** סלקטורים מסוג `[dir]`/`:dir()` בתיקיית `Frontend/src/styles` — כלומר אין החלפת עיצוב מבוססת-כיוון ברמת ה-CSS. במקום זאת הקוד משתמש בעקביות ב**תכונות CSS לוגיות** (`inset-inline-start/end`, `inset-block-start/end`, `margin-inline`, `padding-inline`, `border-inline` וכו') שמסתגלות אוטומטית לכיוון שנקבע ב-`dir` — נמצאו 60 מופעים כאלה ב-15 קבצים, למשל `Frontend/src/app/shared/a11y/skip-link/skip-link.component.ts:14-15` (`inset-block-start`, `inset-inline-start`), `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.scss:6-7` (`inset-block-end`, `inset-inline-start`), ו-`Frontend/src/app/app.component.scss:32,59` (הערה מפורשת: "App is dir=\"rtl\" only... so physical env(safe-area-inset-*)").
- חריג מכוון ל-LTR: `Frontend/src/app/components/subscribe/subscribe.component.scss:145` — `direction: ltr` (ככל הנראה עבור הצגת מספר טלפון/קוד OTP; לא אומת הקשר המדויק מעבר למיקום ה-CSS).

## 8. תמונות וטקסט חלופי (alt)

**נמצאו 0 תגיות `<img>`** בכל עץ `Frontend/src` (קבצי `.html` וגם קבצי `.ts` עם תבניות inline נבדקו). לפיכך אין מה לספור מבחינת נוכחות/היעדר `alt` — האתר אינו משתמש בתגית `<img>` כלל. אייקונים גרפיים מיושמים לחלוטין באמצעות `<svg>` מוטבע בתוך הטמפלטים (למשל `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:9-14`, `Frontend/src/app/app.component.html:24,29`), עם `aria-hidden="true"` ו/או `focusable="false"` על ה-SVG כאשר הוא דקורטיבי בלבד (למשל `accessibility-widget.component.html:9`).

## 9. HTML סמנטי והיררכיית כותרות

**תגיות `<header>`**: `Frontend/src/app/app.component.html:21` (`role="banner"`), `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html:7`, `Frontend/src/app/components/welcome-modal/welcome-modal.component.html:2`, `Frontend/src/app/components/read-permission/read-permission.component.html:5,31`, `Frontend/src/app/admin/shell/admin-shell.component.html:2`.

**`<nav>`**: מופע יחיד — `Frontend/src/app/admin/shell/admin-shell.component.html:24` (ניווט בין מסכי לוח הניהול).

**`<main>`**: `Frontend/src/app/app.component.html:50` (`id="main-content"`, האתר הציבורי), `Frontend/src/app/admin/shell/admin-shell.component.html:32` (לוח הניהול).

**`<footer>`**: `Frontend/src/app/app.component.html:33` (`role="contentinfo"`), `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html:25`.

**`<section>`**: 4 מופעים, כולם ב-`Frontend/src/app/admin/system/admin-system.component.html:1,36,51,64`.

**`<article>`**: לא נמצא אף מופע בכל `Frontend/src/app`.

**היררכיית כותרות (h1-h5)** — עיקרי המסכים: `home.component.html:4` (h1 "הפרק היומי שלך"), `settings.component.html:2` (h1 "הגדרות"), `entrance.component.html:4,22,43,52` (h1, חלקם עם מחלקת `visually-hidden`), `booklist.component.html:2,16` (h1 מוסתר-חזותית + h2 לכל קטגוריה), `chapterlist.component.html:2` (h1 מוסתר-חזותית "רשימת פרקים"), `admin-shell.component.html:3` (h1 "לוח ניהול"). `app.component.html:34` משתמש ב-h4 לכותרת הפוטר ("בשם ה' נעשה ונצליח") — קפיצה ישירה מ-h1/h2 של תוכן העמוד ל-h4 באזור הפוטר. **ממצא ספציפי**: בעמוד קריאת הפרק (`Frontend/src/app/components/chapter/chapter.component.html`) יש h2 לשם הספר (שורה 35, `heBook`), אך שורת ה-h1 המיועדת לכותרת הפרק **מסומנת כהערת HTML (מוערת/disabled)** — `Frontend/src/app/components/chapter/chapter.component.html:36` (`<!-- <h1 class="reader-heading__chapter" ...>...</h1> -->`). כלומר בפועל אין h1 פעיל בעמוד זה. הצהרת הנגישות עצמה (`legal-content-html.ts`) ומסמכים משפטיים אחרים (`terms`, `privacy`) משתמשים ב-`<h3>` בלבד ליחידות המשנה שלהם (`Frontend/src/app/shared/legal/legal-content-html.ts:13` ואילך) — הכותרת הראשית של הדיאלוג עצמו היא `<h2>` ב-`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html:16`.

## 10. הקשר בין נגישות ל-i18n

**כן, ברמה גבוהה ועקבית — כמעט כל `aria-label` סטטי עטוף גם ב-`i18n-aria-label`.**

- מתוך 24 מופעי `aria-label="..."` הסטטיים שנספרו בסעיף 5, **כל ה-24** מלווים בתכונת `i18n-aria-label="@@..."` צמודה על אותו אלמנט. דוגמאות: `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:6-7` (`i18n-aria-label="@@a11y.widget.open"` + `aria-label="פתיחת תפריט נגישות"`), `Frontend/src/app/app.component.html:23` (`i18n-aria-label="@@app.back"` + `aria-label="חזרה"`), `Frontend/src/app/components/settings/settings.component.html:12` (`i18n-aria-label="@@settings.darkMode"`).
- **תכונות `aria-hidden`, `aria-checked`, `aria-pressed`, `aria-expanded`, `aria-modal`, `role`, `aria-valuemin/max/now`, `aria-invalid`**: אינן עטופות ב-`i18n-*` — וזה עקבי עם כך שהערכים שלהן הם בוליאנים/מספרים/מזהי-role טכניים ולא טקסט קריא לאדם, כך שאין מה לתרגם.
- **3 מופעי `[attr.aria-label]` הדינמיים** (בינדינג, לא מחרוזת סטטית) **אינם** ואינם יכולים להיות עטופים ב-`i18n-aria-label` (מגבלת ה-compiler של Angular — `i18n-*` עובד רק על תכונות סטטיות): 
  - `Frontend/src/app/shared/tts/tts-player/tts-player.component.html:15` — הטקסט עצמו כן מתורגם, אך דרך `$localize` בקוד ה-TypeScript: `Frontend/src/app/shared/tts/tts-player/tts-player.component.ts:21-22` (`playPauseLabelPlaying`/`playPauseLabelPaused`, שניהם `$localize` עם מזהי `@@tts.player.pause`/`@@tts.player.play`).
  - `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html:18,23` — הטקסט הדינמי מורכב מ-`data.title` (כותרת המסמך המשפטי הנוכחי, למשל "הצהרת נגישות"). כותרות אלו מוגדרות כמחרוזות עברית קבועות ב-`Frontend/src/app/shared/legal/legal-content.ts:24,33,41` **ללא** עטיפת `$localize` — כלומר במקרה הזה הטקסט שמוזן ל-`aria-label` הדינמי אינו עובר דרך שום מנגנון i18n (לא `i18n-aria-label` ולא `$localize`).
- תוויות טקסט רגילות (לא ARIA) לרוב עטופות ב-`i18n="@@..."` על אלמנט התוכן עצמו, למשל כל תוויות הכפתורים בפאנל הנגישות (`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:28,33,46,60,62-68`) ומחרוזות ה-`A11Y_TOGGLE_LABELS`/הכרזות ה-`LiveAnnouncer` בשירות עצמו — כולן `$localize` עם מזהי `@@a11y....` — `Frontend/src/app/core/a11y/a11y.model.ts:31-37`, `Frontend/src/app/core/a11y/a11y.service.ts:44,50-54,62-63,69`.
- לוח הניהול (`Frontend/src/app/admin/**`) — נבדק במפורש: **אין** אף שימוש ב-`i18n`/`i18n-aria-label` בכל התיקייה (למשל `Frontend/src/app/admin/shell/admin-shell.component.html` כולו טקסט עברי קשיח ללא תכונות i18n), וגם אין בו כמעט שום תכונת ARIA (ראו סעיף 6) — עקבי עם היותו ממשק ניהול פנימי ולא חלק מהאתר הציבורי המונגש/מתורגם.

## לא ידוע / דורש אימות

- **`AccessibilityStatementDialogComponent`**: קובץ `docs/a11y-manual-test.md:45` מפנה ל-`Frontend/src/app/shared/a11y/accessibility-statement-dialog/accessibility-statement-dialog.component.html`. חיפשתי בכל `Frontend/src/app` (כולל `find`/`glob` על התיקייה `Frontend/src/app/shared/a11y`) ולא מצאתי תיקייה או קובץ בשם `accessibility-statement-dialog`. ההצהרה נפתחת בפועל דרך `LegalModalComponent` הגנרי (`Frontend/src/app/shared/legal/legal-modal/legal-modal.component.ts`). לא ברור אם מדובר בשם קובץ שהוחלף ברפקטור מאוחר יותר (המעבר ל-`LegalDialogService` המתועד ב-`Frontend/src/app/shared/legal/legal-dialog.service.ts:6-11` כ"Replaces the former TermsService/AccessibilityStatementService" תומך בכך), או בטעות בתיעוד.
- **הפונקציה/מטרה המדויקת של `<div dir="auto"></div>`** ב-`Frontend/src/app/app.component.html:1`: האלמנט ריק, ללא הערה מסבירה בקוד, וללא שימוש נראה-לעין בהמשך הקובץ. חיפשתי הפניות אליו (class, id, selector) בשאר הקוד ולא מצאתי. לא ידוע אם זהו שריד קוד לא-פעיל, טריק דפדפן ספציפי, או משהו אחר.
- **בדיקות ידניות עם קוראי מסך אמיתיים** (NVDA, VoiceOver): לפי `docs/a11y-manual-test.md:16-31`, טרם בוצעו נכון למועד כתיבת אותו מסמך — מצוין גם בתוך טקסט הצהרת הנגישות עצמה (`Frontend/src/app/shared/legal/legal-content-html.ts:192`: "טרם בוצעה בדיקה ידנית מלאה עם משתמשי תוכנת הקראת מסך אמיתיים"). לא נבדק על ידי, ולא ניתן לאמת מתוך הקוד בלבד, אם בוצעו בדיקות כאלה מאז.
- **ההקשר המדויק של `direction: ltr`** ב-`Frontend/src/app/components/subscribe/subscribe.component.scss:145`: אותר המיקום בקובץ, אך לא אומת באיזה אלמנט HTML ספציפי (תבנית) הכלל הזה חל, ולכן לא ניתן לאשר בוודאות שמדובר בהצגת מספר טלפון/קוד אימות ולא במשהו אחר.
