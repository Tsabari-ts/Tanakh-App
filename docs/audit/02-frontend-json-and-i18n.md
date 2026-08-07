# מיפוי קבצי JSON ו-i18n (בינאום) — Frontend

מסמך זה ממפה בצורה עובדתית את כל קבצי ה-JSON תחת `Frontend/` (למעט `node_modules`, `.angular`, `dist`, `.lighthouseci`, `test-results`) ואת מנגנון הבינאום (i18n) של האפליקציה. כל טענה מגובה בציון קובץ ושורה מדויקים. הבדיקה נעשתה ידנית מול קוד המקור בפועל (grep/קריאת קבצים), ללא הרצת שרתים או גישה למסדי נתונים.

---

## 1. כל קבצי ה-JSON

נמצאו **13** קבצי `.json` תחת `Frontend/` (לא כולל התיקיות שהוחרגו). לא נמצאו קבצי `.json` תחת `Frontend/src/assets` ולא נמצאה תיקיית `assets/i18n` או דומה לה (ראו סעיף 2).

### 1.1 קבצי תצורה/כלים (build/tooling) — לא תוכן/תרגום

| קובץ | מה יש בו | מי קורא אותו | זמן טעינה |
|---|---|---|---|
| `Frontend/package.json` | הגדרת הפרויקט (`name`, `version`), סקריפטים (`ng`, `start`, `build`, `watch`, `test`, `e2e:a11y`, `lighthouse:a11y`, `lint:css`, `verify` — `Frontend/package.json:5-14`), רשימת `dependencies`/`devDependencies` (`Frontend/package.json:16-53`) | נקרא ע"י `npm`/`ng` CLI בזמן פיתוח/בנייה. לא נטען ע"י קוד האפליקציה ב-runtime | Build/tooling בלבד |
| `Frontend/package-lock.json` | נעילת גרסאות מדויקות של כל התלויות (npm lockfile) | נקרא ע"י `npm install`/`npm ci` בלבד | Build/tooling בלבד |
| `Frontend/angular.json` | תצורת פרויקט Angular CLI: נתיבי מקור, הגדרות `build`/`serve`/`test`/`extract-i18n`, בלוק i18n (`Frontend/angular.json:22-33`), `assets`, `styles`, `polyfills` (כולל `@angular/localize/init`, `Frontend/angular.json:55,135`), תקציבי bundle, `fileReplacements`, `serviceWorker: "ngsw-config.json"` (`Frontend/angular.json:84`) | נקרא ע"י Angular CLI (`ng build`/`ng serve`/`ng test`/`ng extract-i18n`) | Build/tooling בלבד |
| `Frontend/tsconfig.json` | תצורת TypeScript בסיסית (`strict`, `target: ES2022` וכו', `Frontend/tsconfig.json:1-29`) | נקרא ע"י מהדר ה-TypeScript/Angular compiler | Build/tooling בלבד |
| `Frontend/tsconfig.app.json` | מרחיב את `tsconfig.json`, מגדיר `types: ["@angular/localize"]` (`Frontend/tsconfig.app.json:6-9`), קובץ כניסה `src/main.ts` | נקרא ע"י Angular CLI בעת בניית האפליקציה | Build/tooling בלבד |
| `Frontend/tsconfig.spec.json` | תצורת TypeScript לבדיקות (Karma/Jasmine), כולל `types: ["jasmine", "@angular/localize"]` (`Frontend/tsconfig.spec.json:6-9`) | נקרא ע"י Karma runner בעת `ng test` | Build/tooling בלבד |
| `Frontend/.stylelintrc.json` | כללי linting ל-CSS/SCSS (איסור `outline: none`, איסור יחידות `px` ב-`font-size`, איסור `!important` עם חריגים לקבצי a11y, `Frontend/.stylelintrc.json:1-25`) | נקרא ע"י stylelint דרך הסקריפט `lint:css` (`Frontend/package.json:12`) | Build/tooling בלבד |
| `Frontend/ngsw-config.json` | תצורת Service Worker: קבוצות נכסים (`app`, `assets`) וקבוצות דאטה עם אסטרטגיות caching ל-`/Tanakh/books/**` ול-`/JewishCalendar/**` (`Frontend/ngsw-config.json:1-56`) | **קובץ קלט לזמן build**: הבילדר `@angular/service-worker` (מוגדר ב-`Frontend/angular.json:84` תחת קונפיגורציית ה-production) קורא אותו בעת `ng build` ומייצר ממנו את `ngsw.json` בפועל בתיקיית ה-dist. את `ngsw.json` המיוצר (לא קיים במאגר המקור) קורא ה-Service Worker בדפדפן ב-runtime | Build-time; הפלט הנגזר ממנו נצרך ב-runtime |
| `Frontend/lighthouserc.json` | תצורת Lighthouse CI: כתובות לבדיקה (`http://localhost:4200/home`, `.../settings`), סף ציון נגישות מינימלי 0.95 (`Frontend/lighthouserc.json:1-19`) | נקרא ע"י `@lhci/cli` דרך הסקריפט `lighthouse:a11y` (`Frontend/package.json:11`) | Build/tooling בלבד (CI) |
| `Frontend/.vscode/extensions.json` | המלצת תוסף VS Code: `angular.ng-template` | נקרא ע"י עורך VS Code בלבד | לא רלוונטי ל-runtime/build |
| `Frontend/.vscode/launch.json` | תצורות debug של VS Code (`ng serve`, `ng test`) | נקרא ע"י עורך VS Code בלבד | לא רלוונטי ל-runtime/build |
| `Frontend/.vscode/tasks.json` | הגדרת משימות VS Code המריצות `npm start`/`npm test` ברקע | נקרא ע"י עורך VS Code בלבד | לא רלוונטי ל-runtime/build |
| `Frontend/.claude/settings.local.json` | רשימת הרשאות (`permissions.allow`) לכלי הסוכן Claude Code בתוך תיקיית ה-Frontend (פקודות Bash מותרות) | נקרא ע"י Claude Code CLI בלבד; אינו קובץ פרויקט/אפליקציה | לא רלוונטי ל-runtime/build של האתר |

**הערה על `manifest.webmanifest`**: הקובץ `Frontend/src/manifest.webmanifest` הוא בפורמט JSON (מכיל `name`, `short_name`, `theme_color`, `icons` וכו', `Frontend/src/manifest.webmanifest:1-64`) אך סיומתו `.webmanifest` ולא `.json`, ולכן אינו נכלל ברשימת "כל קבצי ה-JSON" שלעיל לפי ההגדרה המדויקת שהתבקשה. מצוין כאן לשלמות: הוא מקושר מ-`Frontend/src/index.html:12` (`<link rel="manifest" href="manifest.webmanifest">`) ונטען ע"י הדפדפן ב-runtime לצורך PWA (התקנה/אייקונים). תוכנו כתוב בעברית בלבד (`name`, `short_name`, `description` בעברית, `Frontend/src/manifest.webmanifest:2-3,10`) וללא גרסה מתורגמת לאנגלית.

### 1.2 קבצי תוכן/דאטה/תרגום בפועל

**לא נמצא אף קובץ JSON בקטגוריה זו.** נבדקה תיקיית `Frontend/src/assets` במלואה (`Frontend/src/assets/*`) ונמצאו בה רק תמונות, אייקונים, גופנים וקובץ `_headers` — ללא שום קובץ `.json`. לא נמצא שימוש ב-`HttpClient` או `fetch` הטוען קובץ `.json` מקומי מתיקיית `assets` בקוד תחת `Frontend/src/app`. כלומר, **אין באפליקציה קובצי JSON המשמשים כמקור תוכן/תרגום הנטענים ב-runtime** — כל התוכן הטקסטואלי הקבוע מגיע ישירות מתוך תבניות ה-HTML/TypeScript עצמן (ראו סעיפים 2–3).

---

## 2. קבצי תרגום/לוקאל

תיקיית `Frontend/src/locale/` מכילה **שני קבצים בפורמט XLIFF (`.xlf`), לא JSON**:

- `Frontend/src/locale/messages.xlf` — קובץ המקור (Hebrew), 725 שורות.
- `Frontend/src/locale/messages.en.xlf` — קובץ התרגום לאנגלית, 814 שורות.

**חשוב:** אלו קבצי XML (XLIFF גרסה 2.0 — `<xliff version="2.0" ... srcLang="he">`, `Frontend/src/locale/messages.xlf:2`), **לא קבצי JSON**. הם מוזכרים כאן לשלמות התמונה כפי שהתבקש, ולא נכללו ברשימת ה-JSON שבסעיף 1.

לא נמצאה תיקיית `assets/i18n` או כל תיקיה דומה (כגון `i18n/`, `locales/`) בשום מקום תחת `Frontend/src` — נבדק באמצעות חיפוש רקורסיבי אחר "i18n" בנתיבי קבצים ותיקיות תחת `Frontend` (למעט `node_modules`/`.angular`/`dist`), ולא נמצאה תיקייה כזו מלבד `Frontend/src/locale` עצמה.

### 2.1 יחידות תרגום (`<unit>`)

בפורמט XLIFF 2.0 (שבו משתמש הפרויקט) יחידת התרגום נקראת `<unit>` ולא `<trans-unit>` (התג `<trans-unit>` שייך ל-XLIFF 1.2). נספרו בפועל:

- `messages.xlf`: **89** תגי `<unit id="...">`.
- `messages.en.xlf`: **89** תגי `<unit id="...">`.

**השוואת מזהים בין הקבצים**: הושוותה רשימת ה-`id` המלאה של כל היחידות בין שני הקבצים (מיון + `comm`). **אין אף הבדל** — אותם 89 מזהים בדיוק מופיעים בשני הקבצים, ללא מזהה החסר באחד מהם וללא כפילות פנימית באף אחד מהקבצים.

**בדיקת תוכן התרגום לאנגלית**: נבדק תוכן כל 89 היחידות ב-`messages.en.xlf` — האם השדה `<target>` (התרגום לאנגלית) שונה מהשדה `<source>` (המקור העברי). התוצאה: **בכל 89 היחידות, `<target>` זהה מילה-במילה ל-`<source>`** (לדוגמה `Frontend/src/locale/messages.en.xlf:8-11`: גם `<source>` וגם `<target>` מכילים "סגירת הודעה" בעברית). כלומר, קובץ `messages.en.xlf` **לא תורגם בפועל לאנגלית** — הוא מכיל את הטקסט העברי המקורי גם בשדה ה-`<target>`, כפי שנוצר כברירת מחדל ע"י Angular CLI כאשר עוד לא בוצע תרגום ידני.

**עדכניות קובץ המקור אל מול הקוד**: נבדק תאריך השינוי האחרון של `Frontend/src/locale/messages.xlf` בהיסטוריית git — commit יחיד בתאריך `2026-07-31 02:39:43 +0300`, בעוד שקובץ `Frontend/src/app/components/subscribe/subscribe.component.html` (המכיל תגי `i18n="@@subscribe...` רבים) עודכן לאחרונה ב-`2026-08-06 22:47:21 +0300` — כלומר **אחרי** יצירת קובץ ה-XLIFF. ראו פירוט מלא בסעיף 3.6 להלן: נמצאו 145 מזהי `@@` הקיימים בקוד אך חסרים לגמרי מ-`messages.xlf`.

---

## 3. מנגנון `i18n="@@..."`

### 3.1 מהו המנגנון ולאיזו מערכת הוא שייך

זהו מנגנון הבינאום (internationalization) **המובנה של Angular** — חבילת **`@angular/localize`**. החבילה מופיעה כ-`devDependency` בגרסה `^22.1.0` (`Frontend/package.json:37`), ומופעלת בזמן ריצה/בנייה באמצעות ה-polyfill `"@angular/localize/init"` שמוזכר הן בתצורת ה-`build` והן בתצורת ה-`test` ב-`angular.json` (`Frontend/angular.json:55` וגם `Frontend/angular.json:135`). זהו מנגנון **שונה מהותית** מספריות כמו ngx-translate או Transloco (ראו סעיף 3.8): הוא פועל ע"י **קומפילציה מחדש של האפליקציה לכל שפה בזמן build** (Ahead-of-Time), ולא ע"י טעינת מילון תרגומים דינמי ב-runtime.

התחביר `i18n="..."` הוא attribute מיוחד על אלמנט HTML בתבנית Angular, שמסמן לקומפיילר של Angular (`@angular/compiler-cli`) שהתוכן הטקסטואלי של האלמנט ניתן לחילוץ ותרגום. לצידו קיים גם `$localize` — תג template literal ב-TypeScript המשמש לאותה מטרה בקוד (למשל לכותרות מסלול, הודעות דינמיות וכו').

### 3.2 המשמעות של `@@` ומה שאחריו

בתוך ה-attribute `i18n`, אפשר לציין מטא-דאטה בתחביר `i18n="description|meaning@@customId"`. הסמל `@@` מסמן תחילת **מזהה הודעה מותאם-אישית (custom message ID)** — מחרוזת קבועה שהמפתח בוחר (למשל `app.title`), שמייחדת את ההודעה הזו באופן חד-משמעי בקובצי התרגום.

**החלופה** (כשלא נעשה שימוש ב-`@@`) היא **מזהה אוטומטי** — hash שנוצר אוטומטית ע"י Angular מתוכן ההודעה, המיקום שלה בקוד והקשר שלה. מזהה אוטומטי כזה משתנה בכל פעם שהטקסט המקורי משתנה (מה שגורם ל"תרגום יתום"), בעוד מזהה מותאם-אישית (`@@id`) נשאר יציב גם אם מנסחים מחדש את הטקסט העברי המקורי — כך שהתרגום הקיים לא הולך לאיבוד. **בקוד הזה, המפתחים בחרו להשתמש בעקביות במזהי `@@` מותאמים-אישית בכל מקום (ראו סעיפים 3.3–3.5)** — לא אותר אף שימוש ב-`i18n="..."` ללא `@@` בקבצי `Frontend/src` שנבדקו (כל 147 המופעים של `i18n="` בתבניות HTML/TS כוללים `@@`).

### 3.3 ספירה מדויקת של `i18n="@@`

הפקודה `grep -rc 'i18n="@@' Frontend/src --include=*.html` הניבה **142** מופעים, פרוסים על פני **16** קבצי HTML:

| קובץ | מס' מופעים |
|---|---|
| `Frontend/src/app/app.component.html` | 8 |
| `Frontend/src/app/components/booklist/booklist.component.html` | 6 |
| `Frontend/src/app/components/chapter/chapter.component.html` | 4 |
| `Frontend/src/app/components/chapterlist/chapterlist.component.html` | 4 |
| `Frontend/src/app/components/entrance/entrance.component.html` | 9 |
| `Frontend/src/app/components/home/home.component.html` | 9 |
| `Frontend/src/app/components/read-permission/read-permission.component.html` | 10 |
| `Frontend/src/app/components/settings/settings.component.html` | 11 |
| `Frontend/src/app/components/subscribe/subscribe.component.html` | 37 |
| `Frontend/src/app/components/welcome-modal/welcome-modal.component.html` | 8 |
| `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html` | 17 |
| `Frontend/src/app/shared/announcement-banner/announcement-banner.component.html` | 1 |
| `Frontend/src/app/shared/error-screen/error-screen.component.html` | 6 |
| `Frontend/src/app/shared/legal/legal-modal/legal-modal.component.html` | 2 |
| `Frontend/src/app/shared/maintenance-screen/maintenance-screen.component.html` | 2 |
| `Frontend/src/app/shared/tts/tts-player/tts-player.component.html` | 8 |
| **סה"כ HTML** | **142** |

בנוסף, נבדקו קבצי `.ts` בעלי תבנית inline (`template:` בתוך `@Component`), ונמצאו **5** מופעים נוספים של `i18n="@@` בשני קבצים:

| קובץ | שורה | מס' מופעים |
|---|---|---|
| `Frontend/src/app/components/scroll-to-top-button/scroll-to-top-button.component.ts` | 9, 10 | 2 |
| `Frontend/src/app/shared/a11y/skip-link/skip-link.component.ts` | 7 | 1 |
| `Frontend/src/app/shared/cookie-banner/cookie-banner.component.ts` | 11, 14 | 2 |

**סה"כ כולל (HTML + TS): 142 + 5 = 147 מופעים של `i18n="@@...`.**

### 3.4 דוגמאות אמיתיות מהקוד

1. **`Frontend/src/app/app.component.html:27`**:
   `<p class="site-name" i18n="@@app.title">פרק תנ"ך ביום</p>`
   מסמן את הטקסט "פרק תנ\"ך ביום" (שם האתר בכותרת) כהודעה הניתנת לתרגום עם המזהה הקבוע `app.title`. בעת extract-i18n, ייווצר `<unit id="app.title">` עם `<source>` זה.

2. **`Frontend/src/app/app.component.html:8`**:
   `<button ... i18n-aria-label="@@app.dismissNotification" aria-label="סגירת הודעה">×</button>`
   כאן לא מתורגם תוכן טקסטואלי גלוי אלא ה-attribute `aria-label` עצמו (תג נגישות למקריאי מסך) — ראו הסבר מורחב על `i18n-` בסעיף 3.5.

3. **`Frontend/src/app/shared/cookie-banner/cookie-banner.component.ts:11`** (בתוך template inline ב-TS):
   `<p i18n="@@cookieBanner.text"> אתר זה משתמש בעוגיות (cookies) הנחוצות לתפעולו התקין... </p>`
   מדגים ש-`i18n="@@"` פועל גם בתוך template string מוטבע בקומפוננטת TypeScript (standalone component עם `template:` inline), לא רק בקובצי `.html` נפרדים.

4. **`Frontend/src/app/components/subscribe/subscribe.component.html:64`**:
   `<label for="timeInput" i18n="@@subscribe.timeLabel">בחירת שעה</label>`
   דוגמה למזהה שחוזר על עצמו: אותו `@@subscribe.timeLabel` מופיע שוב ב-`Frontend/src/app/components/subscribe/subscribe.component.html:135` עבור `<label for="manageTimeInput">` זהה טקסטואלית — שני מיקומים באותו קובץ החולקים אותו טקסט מקור וכתוצאה מכך יימוזגו ליחידת XLIFF אחת עם שתי הערות מיקום (ראו סעיף 3.6).

5. **`Frontend/src/app/app.routes.ts:16`** (שימוש ב-`$localize`, לא ב-`i18n=` attribute):
   `title: $localize`:@@route.entrance.title:תנ"ך`
   כותרת דף (`document.title`) עבור מסלול ה-`entrance`, המוגדרת בקובץ TypeScript (לא בתבנית HTML) בעזרת תג ה-template literal `$localize` עם אותו תחביר `@@id` בתוך `:...:` שלפני הטקסט. ראו הרחבה בסעיף 3.7.

### 3.5 שימוש ב-`i18n-<attribute>`

נספרו **25** מופעים של תחילית `i18n-` (המשמשת לתרגום attribute ולא תוכן אלמנט), כולם בקבצי `.html` (לא נמצא אף מופע בקבצי `.ts`):

- `i18n-aria-label=` — **24** מופעים.
- `i18n-placeholder=` — **1** מופע.

רשימת כל 25 המופעים לפי קובץ:

| קובץ | שורות |
|---|---|
| `Frontend/src/app/app.component.html` | 8, 23, 28 (i18n-aria-label) |
| `Frontend/src/app/components/chapter/chapter.component.html` | 6, 9, 25 (i18n-aria-label) |
| `Frontend/src/app/components/entrance/entrance.component.html` | 10 (i18n-placeholder) |
| `Frontend/src/app/components/read-permission/read-permission.component.html` | 13, 39, 54 (i18n-aria-label) |
| `Frontend/src/app/components/settings/settings.component.html` | 12, 32, 38, 54 (i18n-aria-label) |
| `Frontend/src/app/components/subscribe/subscribe.component.html` | 7, 118 (i18n-aria-label) |
| `Frontend/src/app/components/welcome-modal/welcome-modal.component.html` | 9 (i18n-aria-label) |
| `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html` | 6, 29, 34, 47 (i18n-aria-label) |
| `Frontend/src/app/shared/tts/tts-player/tts-player.component.html` | 1, 24, 29, 34 (i18n-aria-label) |

דוגמה: `Frontend/src/app/components/entrance/entrance.component.html:10` —
`autocomplete="given-name" [(ngModel)]="usernameValue" i18n-placeholder="@@entrance.nameGatePlaceholder"` — מסמן את ה-`placeholder` של שדה קלט השם לתרגום, בנפרד מהתוכן הטקסטואלי של האלמנט עצמו.

### 3.6 זרימת התרגום מקצה-לקצה בפרויקט הזה

- **שפת המקור**: `he` (עברית), מוגדרת ב-`Frontend/angular.json:23-26` (`i18n.sourceLocale.code: "he"`, `baseHref: "/"`).
- **שפת יעד מוגדרת**: `en` (אנגלית) בלבד, עם קובץ תרגום `src/locale/messages.en.xlf` ו-`baseHref: "/en/"` (`Frontend/angular.json:27-32`). לא הוגדרה אף שפה נוספת בבלוק ה-`i18n`.
- **חילוץ (extraction)**: קיים target ייעודי `extract-i18n` בתצורת ה-Architect של Angular CLI (`Frontend/angular.json:112-117`, `builder: "@angular-devkit/build-angular:extract-i18n"`), אך **אין** סקריפט npm בשם `extract-i18n` (או דומה) ב-`Frontend/package.json:4-14` — רשימת הסקריפטים שם כוללת רק `ng`, `start`, `build`, `watch`, `test`, `e2e:a11y`, `lighthouse:a11y`, `lint:css`, `verify`. כלומר, כדי לחלץ הודעות יש להריץ ידנית `ng extract-i18n` (או `npx ng extract-i18n`) — אין לכך קיצור מוגדר ב-package.json.
- **קובץ הפלט של החילוץ**: כברירת מחדל, `ng extract-i18n` כותב אל `src/locale/messages.xlf` (השם/הנתיב נגזר מברירות המחדל של Angular CLI ומהימצאות התיקייה `Frontend/src/locale/` בפרויקט; לא נמצאה הגדרת `outputPath`/`i18nFile` מפורשת נוספת בתצורת ה-`extract-i18n` ב-`angular.json:112-117` מעבר ל-`buildTarget`).
- **מצב התרגום בפועל**: כפי שנבדק בסעיף 2.1, קובץ `messages.en.xlf` **קיים ומכיל 89 יחידות, אך אף אחת מהן לא תורגמה בפועל** — כל השדות `<target>` זהים לשדות `<source>` העבריים. כלומר, שלב "התרגום האנושי/ידני" בפועל **לא בוצע** (או בוצע רק כהעתקה אוטומטית ראשונית של Angular CLI ולא הוחלף בתרגום אמיתי).
- **פערי עדכניות בין הקוד לקובץ החילוץ**: הושוו כל מזהי ה-`@@` המופיעים בפועל בקוד (147 מופעי `i18n="@@`, 25 מופעי `i18n-*="@@`, ו-73 מופעי `$localize` עם `@@` — ראו סעיף 3.7; בסה"כ 214 מזהים ייחודיים) מול 89 המזהים הקיימים ב-`messages.xlf`:
  - **145 מזהים קיימים בקוד אך חסרים לגמרי מ-`messages.xlf`** (כלומר, לא חולצו מעולם, או חולצו ואז הקובץ לא עודכן מחדש אחרי שהתווספו). דוגמאות: כל מזהי ה-`subscribe.otp.*`, `subscribe.manage.*`, כל מזהי `a11y.widget.*`/`a11y.toggle.*`/`a11y.contrast.*`, `home.title`, `settings.darkMode`, `tts.player.*` ועוד (הרשימה המלאה מכילה 145 מזהים).
  - **20 מזהים קיימים ב-`messages.xlf` אך לא נמצא עבורם אף שימוש תואם בקוד הנוכחי** (`chapter.finishedReading`, `chapter.nextChapter`, `chapter.scrollDown`, `chapter.scrollUp`, `chapter.stop`, `chapterlist.chapterLabel`, `home.prophets`, `home.settings`, `home.torah`, `home.writings`, `settings.contactUs`, `settings.subscribeButton`, `subscribe.alreadySubscribed1`, `subscribe.alreadySubscribed2`, `subscribe.alreadySubscribedTitle`, `subscribe.emailInvalid`, `subscribe.emailLabel`, `subscribe.skipShabbat`, `subscribe.subscribedButton`, `subscribe.title`).
  - עובדה תומכת: היסטוריית git מראה ש-`Frontend/src/locale/messages.xlf` עודכן לאחרונה ב-31/07/2026, בעוד קבצים כמו `Frontend/src/app/components/subscribe/subscribe.component.html` עודכנו לאחרונה ב-06/08/2026 — כלומר אחרי יצירת קובץ ה-XLIFF הנוכחי. מסקנה עובדתית: **קובץ החילוץ `messages.xlf` אינו עדכני ביחס לקוד המקור הנוכחי**, ולכן גם `messages.en.xlf` (שמבנה המזהים שלו זהה לחלוטין ל-`messages.xlf`, כפי שנבדק בסעיף 2.1) אינו עדכני.
- **בניית גרסה פר-לוקאל**: בלוק ה-`i18n` ב-`Frontend/angular.json:22-33` מגדיר את הלוקאל `en` עם `translation: "src/locale/messages.en.xlf"` ו-`baseHref: "/en/"`. זו התשתית התקנית של Angular ליצירת build נפרד לכל לוקאל (בדרך כלל דרך `ng build --localize` או קונפיגורציית build ספציפית ללוקאל). **אולם**, בבדיקת תצורת ה-`build` בפועל (`Frontend/angular.json:35-99`) **לא נמצא דגל/אפשרות `"localize"`** תחת אף אחת מקונפיגורציות ה-build (`production`/`development`). כמו כן, הסקריפט `build` בפועל הוא `"ng build"` בלבד (`Frontend/package.json:7`), ללא `--localize` וללא ציון לוקאל. גם workflow ה-CI היחיד שמריץ בנייה, `.github/workflows/frontend-a11y-ci.yml:39`, מריץ `npm run build` (כלומר `ng build` רגיל) — **ללא** הפעלת בנייה מקומית (localized build). **מסקנה עובדתית: קיימת הגדרת i18n locales ב-`angular.json`, אך לא אותרה שום נקודת הרצה (script/CI) בריפו זה שבפועל מפעילה בנייה פר-לוקאל (`--localize`) ומייצרת פלט אנגלי נפרד. אם קיים תהליך כזה, הוא מתבצע מחוץ לריפו זה (למשל ידנית או בפייפליין נפרד שלא אותר).**

### 3.7 שימוש ב-`$localize` (מנגנון i18n נוסף ב-TS, אותה מערכת)

מעבר לתחביר `i18n="@@"` (בתבניות) ו-`i18n-*="@@"` (attributes), המערכת `@angular/localize` כוללת גם את תג ה-template literal `$localize` לשימוש בקוד TypeScript טהור (מחוץ לתבנית). נמצאו **73** מופעים של `` $localize` `` בקבצי `.ts` תחת `Frontend/src`, **וכולם** (73 מתוך 73) משתמשים בתחביר `:@@id:טקסט` עם מזהה מותאם-אישית — כלומר עקביים לחלוטין עם הבחירה לא להשתמש במזהים אוטומטיים (ראו סעיף 3.2). פילוח לפי קובץ:

| קובץ | מס' מופעים |
|---|---|
| `Frontend/src/app/app.routes.ts` | 6 |
| `Frontend/src/app/components/booklist/booklist.component.ts` | 7 |
| `Frontend/src/app/components/home/home.component.ts` | 5 |
| `Frontend/src/app/components/settings/settings.component.ts` | 8 |
| `Frontend/src/app/components/subscribe/subscribe.component.ts` | 10 |
| `Frontend/src/app/core/a11y/a11y.model.ts` | 7 |
| `Frontend/src/app/core/a11y/a11y.service.ts` | 8 |
| `Frontend/src/app/core/global-error-handler.ts` | 1 |
| `Frontend/src/app/core/interceptors/error.interceptor.ts` | 9 |
| `Frontend/src/app/core/tts/tts.service.ts` | 2 |
| `Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.ts` | 4 |
| `Frontend/src/app/shared/maintenance-screen/maintenance-screen.component.ts` | 1 |
| `Frontend/src/app/shared/tts/tts-player/tts-player.component.ts` | 2 |

(מספר זה, 73, אינו נכלל בספירת "142/147" שהתבקשה בסעיף 3.3 — אשר מתייחסת אך ורק לתחביר `i18n="@@` — אך הוא חלק מאותה מערכת `@angular/localize` ומאותם קבצי XLIFF, ולכן מתועד כאן לשלמות התמונה כפי שנדרש בהקדמת המשימה.)

**סך כל ה"נקודות תרגום" הייחודיות בקוד המקור (i18n= + i18n-*= + `$localize`): 245 מופעים, המתמצים ל-214 מזהי `@@` ייחודיים** (ריבוי המופעים ביחס למזהים הייחודיים נובע משימוש חוזר באותו מזהה — ראו סעיף הבא).

### 3.8 בדיקת כפילויות במזהי `@@`

הושוו כלל 245 מופעי ה-`@@id` בקוד (i18n=, i18n-*=, ו-`$localize`) ונמצאו **28 מזהים** המופיעים ביותר ממקום אחד:

`a11y.contrast.default`, `a11y.contrast.high`, `a11y.contrast.inverted`, `a11y.toggle.bigCursor`, `a11y.toggle.grayscale`, `a11y.toggle.highlightLinks`, `a11y.toggle.noMotion`, `a11y.toggle.readableFont`, `a11y.toggle.readingRuler`, `a11y.toggle.textSpacing`, `a11y.widget.contrast`, `a11y.widget.fontSize`, `app.refreshNow`, `booklist.empty`, `chapter.decreaseFont`, `chapter.increaseFont`, `cookieBanner.dismiss`, `dialog.close`, `entrance.loading`, `errorScreen.tryAgain`, `readPermission.kicker`, `readPermission.saving`, `readPermission.title`, `settings.darkMode`, `settings.nikudDefault`, `subscribe.manage.saveFailed`, `subscribe.timeLabel`, `subscribe.toggleLabel`.

**לכל 28 המזהים הללו נבדק ידנית שהטקסט המקורי (עברית) בכל אחד מהמופעים זהה** (למשל `app.refreshNow` מופיע פעמיים — ב-`Frontend/src/app/app.component.html:15` וב-`Frontend/src/app/shared/error-screen/error-screen.component.html:16` — ובשני המקומות הטקסט הוא "רענן עכשיו" באופן זהה; `a11y.toggle.bigCursor` מוגדר פעם אחת כטקסט קבוע ב-`Frontend/src/app/core/a11y/a11y.model.ts:36` בעזרת `$localize` ופעם נוספת כתוכן אלמנט ב-`Frontend/src/app/shared/a11y/accessibility-widget/accessibility-widget.component.html:67` עם `i18n="@@a11y.toggle.bigCursor"` — שני המקומות עם הטקסט "סמן עכבר מוגדל"). **לא נמצא אף מקרה של אותו מזהה `@@` המשמש לשני טקסטים מקור שונים** (התנגשות אמיתית). כלומר, כל הכפילויות שנמצאו הן שימוש חוזר מכוון ועקבי באותו מזהה עבור אותה מחרוזת, בהתאם למנגנון הרגיל של Angular שממזג מיקומים מרובים תחת אותו `<unit>` (כפי שאכן נראה ב-XLIFF עצמו — למשל `app.refreshNow` מופיע כ-`<unit>` יחיד עם שתי הערות `<note category="location">` ב-`Frontend/src/locale/messages.xlf`, שורות שכנות ל-id זה).

### 3.9 מזהים שמוגדרים ב-XLIFF אך נראים לא בשימוש בקוד הנוכחי

ראו רשימה מלאה בסעיף 3.6 לעיל (20 מזהים). זהו בדיקה best-effort בלבד (התאמת מחרוזת מדויקת של ה-id מול קוד המקור); לא ניתן לשלול לחלוטין תרחישים חריגים (למשל שימוש דינמי שאינו נתפס ע"י grep). פירוט אי-הוודאות בסעיף "לא ידוע" בתחתית המסמך.

### 3.10 ספריית i18n נוספת (ngx-translate / Transloco וכו')

נבדקו `Frontend/package.json` (רשימת `dependencies`/`devDependencies` מלאה, `Frontend/package.json:16-53`) וגם חיפוש טקסט מלא תחת `Frontend/src` אחר `ngx-translate`, `transloco`, `i18next`. **לא נמצאה אף התאמה אחת** — לא כתלות ב-`package.json` ולא כ-import בקוד. **המסקנה העובדתית: אין באפליקציה שום ספריית i18n חיצונית/נוספת מעבר למנגנון המובנה של Angular (`@angular/localize`)**. כל הבינאום בפרויקט מתבצע אך ורק דרך `i18n="@@"`, `i18n-*="@@"` ו-`$localize` כמתואר לעיל.

---

## לא ידוע / דורש אימות

- **תהליך תרגום אנושי בפועל**: לא נמצא בריפו זה שום ראיה לתהליך עבודה (script, מסמך, כלי חיצוני) שבו מתבצע תרגום אמיתי של `messages.en.xlf` (מעבר להעתקה האוטומטית של Angular CLI). לא ידוע אם קיים תהליך תרגום חיצוני (למשל כלי SaaS לתרגום, מתרגם אנושי) שמופעל מחוץ לריפו ואינו מתועד כאן.
- **מיקום מדויק של קובץ הפלט של `ng extract-i18n`**: לא נמצאה הגדרת `outputPath`/שם קובץ מפורשת בתצורת ה-`extract-i18n` ב-`Frontend/angular.json:112-117` (מוגדר שם רק `buildTarget: "Tanakh:build"`). ההנחה שהפלט הולך ל-`src/locale/messages.xlf` מבוססת על ברירת המחדל הסטנדרטית של Angular CLI ועל כך שזהו הקובץ היחיד הקיים בפועל בתיקיית `src/locale/` בעברית — אך לא אומת ע"י הרצה בפועל של הפקודה (לא הורשה להריץ פקודות build/extract כחלק ממשימה זו, וגם לא התבקש).
- **האם קיימת בנייה פר-לוקאל (`--localize`) המופעלת מחוץ לריפו**: כפי שצוין בסעיף 3.6, לא נמצא script/CI בריפו זה שמפעיל `ng build --localize` או קונפיגורציית build עם `localize: true`. לא ניתן לשלול קיום תהליך כזה מחוץ לריפו (למשל בפלטפורמת אחסון/CI חיצונית שלא נסרקה), ולכן לא נקבע חד-משמעית "אין דבר כזה בכלל בפרויקט" — רק "לא אותר בריפו זה".
- **מזהי `@@` שנראים "לא בשימוש" (20 המזהים בסעיף 3.9)**: הבדיקה מבוססת על התאמת מחרוזת מדויקת בין ה-id שבקובץ ה-XLIFF לבין מופעי `@@id` בקוד המקור הנוכחי. ייתכן תיאורטית (אם כי לא נמצאה כל ראיה לכך) שמזהה כלשהו נבנה דינמית ב-runtime (לדוגמה concatenation של מחרוזות) ולכן לא נתפס ע"י חיפוש טקסטואלי; לא נמצא בפועל שום קוד כזה, אך מצוין כאן כסייג מתודולוגי.
- **שימוש בפועל של `baseHref: "/en/"` בשרת/בפריסה**: לא נבדק (מחוץ לתחום המשימה, שהיא Frontend/JSON/i18n בלבד) האם שרת ה-Backend או תצורת האחסון הסטטי (reverse proxy וכו') בפועל מגישים תוכן מתחת ל-`/en/`. הבדיקה כאן הוגבלה לקבצי תצורה תחת `Frontend/`.
