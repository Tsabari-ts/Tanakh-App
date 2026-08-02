import { Injectable, inject, signal, computed } from '@angular/core';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { TtsProvider } from './tts-provider';
import { normalizeVerseForSpeech } from './tts-text-normalizer';
import {
  TtsPlaybackState, TtsSettings, TTS_SETTINGS_DEFAULTS, TTS_SETTINGS_STORAGE_KEY,
  DivineNameReading,
} from './tts.model';

const RESUME_STORAGE_KEY = 'tanach.tts.resume.v1';

interface StoredResumePosition {
  chapterKey: string;
  verseIndex: number;
}

@Injectable({ providedIn: 'root' })
export class TtsService {
  private provider = inject(TtsProvider);
  private live = inject(LiveAnnouncer);

  readonly isSupported = this.provider.isSupported;
  readonly voices = this.provider.voices;
  readonly hasHebrewVoice = computed(() => this.voices().length > 0);
  /** Shown persistently by the player, not just announced once (V-02) -
   *  a screen-reader user landing after the fact still needs to see why
   *  the play button doesn't do anything. */
  readonly unavailableReason = computed<string | null>(() => {
    if (!this.isSupported) {
      return $localize`:@@tts.unsupported:הדפדפן שלך אינו תומך בהקראת טקסט.`;
    }
    if (!this.hasHebrewVoice()) {
      return $localize`:@@tts.noHebrewVoice:הדפדפן שלך לא כולל קול עברי - נסה Chrome או Safari, או התקן חבילת שפה עברית במערכת ההפעלה.`;
    }
    return null;
  });

  readonly settings = signal<TtsSettings>(this.loadSettings());
  readonly state = signal<TtsPlaybackState>('idle');
  readonly activeVerseIndex = signal<number | null>(null);
  /** Verse to offer resuming from for the *currently loaded* chapter, or
   *  null if there's nothing to resume (V-09). */
  readonly resumeVerseIndex = signal<number | null>(null);

  private verses: string[] = [];
  private chapterKey: string | null = null;
  /** Bumped on every stop()/loadChapter() so a stale speak() promise from a
   *  superseded run can't advance playback after the fact. */
  private runToken = 0;

  constructor() {
    // V-11: stop the "ghost" narration a user isn't looking at anymore when
    // they switch tabs/apps, if they've opted into that (default on).
    document.addEventListener('visibilitychange', () => {
      if (document.hidden && this.settings().stopOnTabHidden && this.state() === 'playing') {
        this.pause();
      }
    });
  }

  loadChapter(verses: string[], chapterKey: string): void {
    this.cancelInternal();
    this.verses = verses;
    this.chapterKey = chapterKey;
    this.activeVerseIndex.set(null);
    this.state.set('idle');
    this.resumeVerseIndex.set(this.loadResumePosition(chapterKey));
  }

  /** V-09: continue from where the user left off in this chapter. */
  playFromResumePoint(): void {
    const index = this.resumeVerseIndex();
    if (index !== null) {
      this.playFromVerse(index);
    }
  }

  playChapter(): void {
    this.playFromVerse(0);
  }

  playFromVerse(index: number): void {
    if (index < 0 || index >= this.verses.length) {
      return;
    }
    const reason = this.unavailableReason();
    if (reason) {
      this.state.set('error');
      this.live.announce(reason, 'assertive');
      return;
    }
    this.runToken++;
    this.state.set('playing');
    this.speakVerse(index, this.runToken);
  }

  private speakVerse(index: number, token: number): void {
    if (index >= this.verses.length) {
      this.state.set('idle');
      this.activeVerseIndex.set(null);
      // Finished the chapter naturally - nothing left to resume.
      this.clearResumePosition();
      return;
    }

    this.activeVerseIndex.set(index);
    const text = normalizeVerseForSpeech(this.verses[index], {
      keepNikud: true,
      divineNameReading: this.settings().divineNameReading,
    });

    this.provider.speak(text, {
      voiceURI: this.settings().voiceURI ?? undefined,
      rate: this.settings().rate,
    }).then(() => {
      // A newer run (stop/replay/skip) has already taken over - don't
      // resurrect this finished-but-superseded chain.
      if (token !== this.runToken || this.state() !== 'playing') {
        return;
      }
      this.speakVerse(index + 1, token);
    }).catch(() => {
      if (token === this.runToken) {
        this.state.set('error');
      }
    });
  }

  pause(): void {
    if (this.state() !== 'playing') { return; }
    this.provider.pause();
    this.state.set('paused');
  }

  resume(): void {
    if (this.state() !== 'paused') { return; }
    this.provider.resume();
    this.state.set('playing');
  }

  stop(): void {
    this.saveCurrentAsResumePosition();
    this.cancelInternal();
    this.state.set('idle');
    this.activeVerseIndex.set(null);
  }

  next(): void {
    const current = this.activeVerseIndex();
    if (current === null) { return; }
    this.playFromVerse(current + 1);
  }

  previous(): void {
    const current = this.activeVerseIndex();
    if (current === null) { return; }
    this.playFromVerse(Math.max(0, current - 1));
  }

  setRate(rate: number): void {
    this.patchSettings({ rate });
    this.restartCurrentVerseIfPlaying();
  }

  setVoice(voiceURI: string | null): void {
    this.patchSettings({ voiceURI });
    this.restartCurrentVerseIfPlaying();
  }

  setDivineNameReading(reading: DivineNameReading): void {
    this.patchSettings({ divineNameReading: reading });
  }

  setStopOnTabHidden(stopOnTabHidden: boolean): void {
    this.patchSettings({ stopOnTabHidden });
  }

  /** Called on route leave / component destroy (V-10) - speechSynthesis
   *  keeps talking in the background otherwise. */
  cancelForNavigation(): void {
    this.saveCurrentAsResumePosition();
    this.cancelInternal();
    this.state.set('idle');
    this.activeVerseIndex.set(null);
  }

  private restartCurrentVerseIfPlaying(): void {
    const current = this.activeVerseIndex();
    if (current !== null && this.state() === 'playing') {
      this.playFromVerse(current);
    }
  }

  private cancelInternal(): void {
    this.runToken++;
    this.provider.cancel();
  }

  private patchSettings(patch: Partial<TtsSettings>): void {
    this.settings.update(s => {
      const next = { ...s, ...patch };
      this.saveSettings(next);
      return next;
    });
  }

  private loadSettings(): TtsSettings {
    try {
      const raw = localStorage.getItem(TTS_SETTINGS_STORAGE_KEY);
      return raw ? { ...TTS_SETTINGS_DEFAULTS, ...JSON.parse(raw) } : { ...TTS_SETTINGS_DEFAULTS };
    } catch {
      return { ...TTS_SETTINGS_DEFAULTS };
    }
  }

  private saveSettings(settings: TtsSettings): void {
    try {
      localStorage.setItem(TTS_SETTINGS_STORAGE_KEY, JSON.stringify(settings));
    } catch {
      /* localStorage unavailable (private browsing) - setting stays session-only */
    }
  }

  private saveCurrentAsResumePosition(): void {
    if (!this.chapterKey) { return; }
    const index = this.activeVerseIndex();
    if (index === null) { return; }
    try {
      const position: StoredResumePosition = { chapterKey: this.chapterKey, verseIndex: index };
      localStorage.setItem(RESUME_STORAGE_KEY, JSON.stringify(position));
      this.resumeVerseIndex.set(index);
    } catch {
      /* localStorage unavailable - resume just won't be offered next time */
    }
  }

  private loadResumePosition(chapterKey: string): number | null {
    try {
      const raw = localStorage.getItem(RESUME_STORAGE_KEY);
      if (!raw) { return null; }
      const stored: StoredResumePosition = JSON.parse(raw);
      return stored.chapterKey === chapterKey ? stored.verseIndex : null;
    } catch {
      return null;
    }
  }

  private clearResumePosition(): void {
    this.resumeVerseIndex.set(null);
    try {
      localStorage.removeItem(RESUME_STORAGE_KEY);
    } catch {
      /* localStorage unavailable - nothing to clear */
    }
  }
}
