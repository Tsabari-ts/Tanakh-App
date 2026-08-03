import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'tanach.readerPrefs.v1';
const MIN_SCALE = 0.7;
const MAX_SCALE = 2;
const SCALE_STEP = 0.12;

export type ReaderFont = 'serif' | 'heading' | 'body';

interface ReaderPrefs {
  font: ReaderFont;
  scale: number;
  nikud: boolean;
}

const FONT_STACKS: Record<ReaderFont, string> = {
  serif: 'var(--font-scripture)',
  heading: 'var(--font-heading)',
  body: 'var(--font-body)',
};

const DEFAULTS: ReaderPrefs = { font: 'serif', scale: 1, nikud: true };

function load(): ReaderPrefs {
  try {
    return { ...DEFAULTS, ...JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}') };
  } catch {
    return DEFAULTS;
  }
}

const NIKUD_RANGE = /[֑-ׇ]/g;

/**
 * Reader-only text preferences (font family, size, niqqud) for the chapter
 * reading view - scoped to scripture text via --reader-font-scale, separate
 * from the whole-app --a11y-font-scale the accessibility widget controls.
 */
@Injectable({ providedIn: 'root' })
export class ReaderPrefsService {
  private readonly prefs = signal(load());

  readonly font = computed(() => this.prefs().font);
  readonly scale = computed(() => this.prefs().scale);
  readonly nikud = computed(() => this.prefs().nikud);
  readonly fontFamily = computed(() => FONT_STACKS[this.prefs().font]);
  readonly scalePercent = computed(() => Math.round(((this.scale() - MIN_SCALE) / (MAX_SCALE - MIN_SCALE)) * 100));

  setFont(font: ReaderFont): void {
    this.update({ font });
  }

  incFont(): void {
    this.update({ scale: Math.min(MAX_SCALE, +(this.prefs().scale + SCALE_STEP).toFixed(2)) });
  }

  decFont(): void {
    this.update({ scale: Math.max(MIN_SCALE, +(this.prefs().scale - SCALE_STEP).toFixed(2)) });
  }

  toggleNikud(): void {
    this.update({ nikud: !this.prefs().nikud });
  }

  applyNikud(text: string): string {
    return this.prefs().nikud ? text : text.replace(NIKUD_RANGE, '');
  }

  private update(partial: Partial<ReaderPrefs>): void {
    this.prefs.update(p => ({ ...p, ...partial }));
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.prefs()));
  }
}
