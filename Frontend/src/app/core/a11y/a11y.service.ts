import { Injectable, signal, effect, computed, inject, DestroyRef } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import {
  A11ySettings, A11Y_DEFAULTS, A11Y_STORAGE_KEY, A11Y_TOGGLE_LABELS,
  FontScale, ContrastMode,
} from './a11y.model';

@Injectable({ providedIn: 'root' })
export class A11yService {
  private doc = inject(DOCUMENT);
  private live = inject(LiveAnnouncer);
  private destroyRef = inject(DestroyRef);

  readonly settings = signal<A11ySettings>(this.load());
  readonly isDirty = computed(() =>
    JSON.stringify(this.settings()) !== JSON.stringify(A11Y_DEFAULTS)
  );

  private rulerEl: HTMLElement | null = null;
  private onPointerMove = (e: PointerEvent): void => {
    this.rulerEl?.style.setProperty('--ruler-y', `${e.clientY}px`);
  };
  private onFocusIn = (e: FocusEvent): void => {
    const target = e.target as HTMLElement;
    if (!(target instanceof HTMLElement)) { return; }
    const r = target.getBoundingClientRect();
    this.rulerEl?.style.setProperty('--ruler-y', `${r.top + r.height / 2}px`);
  };

  constructor() {
    effect(() => {
      const s = this.settings();
      this.apply(s);
      this.save(s);
    });

    this.destroyRef.onDestroy(() => this.disableReadingRuler());
  }

  setFontScale(v: FontScale): void {
    this.patch({ fontScale: v });
    const percent = Math.round(v * 100);
    this.live.announce($localize`:@@a11y.announce.fontScale:גודל הטקסט ${percent}:percent: אחוז`, 'polite');
  }

  setContrast(v: ContrastMode): void {
    this.patch({ contrast: v });
    const label = {
      default: $localize`:@@a11y.contrast.default:רגילה`,
      high: $localize`:@@a11y.contrast.high:גבוהה`,
      inverted: $localize`:@@a11y.contrast.inverted:הפוכה`,
    }[v];
    this.live.announce($localize`:@@a11y.announce.contrast:ניגודיות ${label}:label:`, 'polite');
  }

  toggle(key: keyof Omit<A11ySettings, 'fontScale' | 'contrast'>): void {
    const next = !this.settings()[key];
    this.patch({ [key]: next } as Partial<A11ySettings>);
    const label = A11Y_TOGGLE_LABELS[key] ?? key;
    const state = next
      ? $localize`:@@a11y.announce.enabled:הופעל`
      : $localize`:@@a11y.announce.disabled:בוטל`;
    this.live.announce(`${label} ${state}`, 'polite');
  }

  reset(): void {
    this.settings.set({ ...A11Y_DEFAULTS });
    this.live.announce($localize`:@@a11y.announce.reset:כל הגדרות הנגישות אופסו`, 'polite');
  }

  private patch(p: Partial<A11ySettings>): void {
    this.settings.update(s => ({ ...s, ...p }));
  }

  private apply(s: A11ySettings): void {
    const html = this.doc.documentElement;
    html.style.setProperty('--a11y-font-scale', String(s.fontScale));
    html.classList.toggle('a11y-high-contrast', s.contrast === 'high');
    html.classList.toggle('a11y-inverted-contrast', s.contrast === 'inverted');
    html.classList.toggle('a11y-grayscale', s.grayscale);
    html.classList.toggle('a11y-highlight-links', s.highlightLinks);
    html.classList.toggle('a11y-readable-font', s.readableFont);
    html.classList.toggle('a11y-text-spacing', s.textSpacing);
    html.classList.toggle('a11y-no-motion', s.noMotion);
    html.classList.toggle('a11y-big-cursor', s.bigCursor);

    if (s.readingRuler) {
      this.enableReadingRuler();
    } else {
      this.disableReadingRuler();
    }
  }

  private enableReadingRuler(): void {
    if (this.rulerEl) { return; }
    this.rulerEl = this.doc.createElement('div');
    this.rulerEl.className = 'a11y-ruler';
    this.rulerEl.setAttribute('aria-hidden', 'true');
    this.doc.body.appendChild(this.rulerEl);
    this.doc.addEventListener('pointermove', this.onPointerMove, { passive: true });
    this.doc.addEventListener('focusin', this.onFocusIn);
  }

  private disableReadingRuler(): void {
    if (!this.rulerEl) { return; }
    this.doc.removeEventListener('pointermove', this.onPointerMove);
    this.doc.removeEventListener('focusin', this.onFocusIn);
    this.rulerEl.remove();
    this.rulerEl = null;
  }

  private load(): A11ySettings {
    try {
      const raw = localStorage.getItem(A11Y_STORAGE_KEY);
      return raw ? { ...A11Y_DEFAULTS, ...JSON.parse(raw) } : { ...A11Y_DEFAULTS };
    } catch {
      return { ...A11Y_DEFAULTS };
    }
  }

  private save(s: A11ySettings): void {
    try {
      localStorage.setItem(A11Y_STORAGE_KEY, JSON.stringify(s));
    } catch {
      /* localStorage unavailable (private browsing) - setting stays session-only */
    }
  }
}
