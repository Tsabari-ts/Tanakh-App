export type ContrastMode = 'default' | 'high' | 'inverted';
export type FontScale = 1 | 1.15 | 1.3 | 1.5;

export interface A11ySettings {
  fontScale: FontScale;
  contrast: ContrastMode;
  grayscale: boolean;
  highlightLinks: boolean;
  readableFont: boolean;
  textSpacing: boolean;
  noMotion: boolean;
  bigCursor: boolean;
  readingRuler: boolean;
}

export const A11Y_DEFAULTS: A11ySettings = {
  fontScale: 1,
  contrast: 'default',
  grayscale: false,
  highlightLinks: false,
  readableFont: false,
  textSpacing: false,
  noMotion: false,
  bigCursor: false,
  readingRuler: false,
};

export const A11Y_STORAGE_KEY = 'tanach.a11y.v1';

export const A11Y_TOGGLE_LABELS: Record<string, string> = {
  grayscale: $localize`:@@a11y.toggle.grayscale:גווני אפור`,
  highlightLinks: $localize`:@@a11y.toggle.highlightLinks:הדגשת קישורים`,
  readableFont: $localize`:@@a11y.toggle.readableFont:גופן קריא`,
  textSpacing: $localize`:@@a11y.toggle.textSpacing:ריווח טקסט מוגבר`,
  noMotion: $localize`:@@a11y.toggle.noMotion:עצירת אנימציות`,
  bigCursor: $localize`:@@a11y.toggle.bigCursor:סמן עכבר מוגדל`,
  readingRuler: $localize`:@@a11y.toggle.readingRuler:סרגל קריאה`,
};
