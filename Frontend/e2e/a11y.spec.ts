import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// These routes render without needing the .NET backend (or degrade to an
// accessible error state if it isn't running - see chapter/booklist/
// chapterlist's loadError branches), so the suite works standalone in CI.
const SCREENS = [
  { name: 'בית (עם חלון ברוכים הבאים)', path: '/home', seenWelcome: false },
  { name: 'בית', path: '/home', seenWelcome: true },
  { name: 'הגדרות', path: '/settings', seenWelcome: true },
  { name: 'רשימת ספרים', path: '/books/torah', seenWelcome: true },
  { name: 'רשימת פרקים', path: '/books/Torah/Genesis', seenWelcome: true },
  { name: 'פרק', path: '/books/Torah/Genesis/1/false', seenWelcome: true },
];

const MODES: { name: string; classes: string[]; scale?: string }[] = [
  { name: 'ברירת מחדל', classes: [] },
  { name: 'ניגודיות גבוהה', classes: ['a11y-high-contrast'] },
  { name: 'ניגודיות הפוכה', classes: ['a11y-inverted-contrast'] },
  { name: 'גופן ענק', classes: [], scale: '1.5' },
];

async function seedWelcomeSeen(page: Page, path: string, seen: boolean): Promise<void> {
  await page.addInitScript((value) => {
    if (value) {
      localStorage.setItem('userHasSeenWelcomeModal', 'true');
    }
  }, seen);
  await page.goto(path);
  // Material dialogs animate in (opacity/transform); running axe mid-fade
  // makes every color look low-contrast because it's blended with whatever
  // is behind it. Give the enter animation time to finish first.
  if (await page.locator('mat-dialog-container').count()) {
    await page.locator('mat-dialog-container').first().waitFor({ state: 'visible' });
    // Under parallel-worker CPU contention the enter animation can still be
    // mid-fade at 300ms, making every color look falsely low-contrast.
    await page.waitForTimeout(600);
  }
}

const MOCK_VERSES = [
  'בְּרֵאשִׁית בָּרָא אֱלֹהִים אֵת הַשָּׁמַיִם וְאֵת הָאָרֶץ',
  'וְהָאָרֶץ הָיְתָה תֹהוּ וָבֹהוּ וְחֹשֶׁךְ עַל פְּנֵי תְהוֹם',
  'וַיֹּאמֶר אֱלֹהִים יְהִי אוֹר וַיְהִי אוֹר',
];

async function mockChapterApi(page: Page): Promise<void> {
  await page.route('**/Tanakh/books/**', route => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify({
      bookData: {
        verses: MOCK_VERSES,
        hebrewSectionRef: 'בראשית א׳',
        nextChapter: 'Genesis 2',
      },
    }),
  }));
}

/** Fakes a Hebrew voice so the full player UI (not just the "no voice"
 *  fallback message) renders - CI runners typically have zero TTS voices. */
async function mockHebrewVoice(page: Page): Promise<void> {
  await page.addInitScript(() => {
    const fakeVoice = {
      voiceURI: 'fake-he-voice', name: 'Fake Hebrew Voice', lang: 'he-IL',
      default: true, localService: true,
    } as unknown as SpeechSynthesisVoice;
    Object.defineProperty(window.speechSynthesis, 'getVoices', {
      value: () => [fakeVoice],
    });
  });
}

async function applyMode(page: Page, mode: (typeof MODES)[number]): Promise<void> {
  await page.evaluate(([classes, scale]) => {
    document.documentElement.classList.add(...(classes as string[]));
    if (scale) {
      document.documentElement.style.setProperty('--a11y-font-scale', scale as string);
    }
  }, [mode.classes, mode.scale ?? '']);
}

for (const screen of SCREENS) {
  for (const mode of MODES) {
    test(`נגישות · ${screen.name} · ${mode.name}`, async ({ page }) => {
      await seedWelcomeSeen(page, screen.path, screen.seenWelcome);
      await applyMode(page, mode);

      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
        .analyze();

      expect(results.violations).toEqual([]);
    });
  }
}

test("נגישות · פאנל הווידג'ט הפתוח", async ({ page }) => {
  await seedWelcomeSeen(page, '/home', true);
  await page.getByRole('button', { name: 'פתיחת תפריט נגישות' }).click();
  await expect(page.locator('#a11y-panel')).toBeVisible();

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});

test('נגישות · דיאלוג הצהרת הנגישות (deep link)', async ({ page }) => {
  await seedWelcomeSeen(page, '/home?a11y=statement', true);
  await expect(page.getByRole('dialog')).toBeVisible();

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});

test('נגישות · פרק עם תוכן אמיתי', async ({ page }) => {
  await mockChapterApi(page);
  await seedWelcomeSeen(page, '/books/Torah/Genesis/1/false', true);
  await expect(page.locator('.verse').first()).toBeVisible();
  // Whether this machine's speechSynthesis.getVoices() happens to report a
  // Hebrew voice varies by OS/browser (Chrome lists some "network" voices
  // even offline) - either way the player must render *something* usable,
  // not a dead button (V-02), which the dedicated mocked-voice test below
  // checks more specifically.
  await expect(page.locator('.tts-player')).toBeVisible();

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});

test("נגישות · נגן ההקראה (עם קול עברי מדומה)", async ({ page }) => {
  await mockHebrewVoice(page);
  await mockChapterApi(page);
  await seedWelcomeSeen(page, '/books/Torah/Genesis/1/false', true);
  await expect(page.locator('.tts-player__controls')).toBeVisible();

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations).toEqual([]);

  const playButton = page.getByRole('button', { name: 'הפעלת הקראה' });
  await playButton.focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('button', { name: 'השהיית הקראה' })).toHaveAttribute('aria-pressed', 'true');
});

test('נגישות · דילוג לתוכן הראשי עובד במקלדת', async ({ page }) => {
  await seedWelcomeSeen(page, '/home', true);
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'דילוג לתוכן הראשי' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#main-content')).toBeFocused();
});
