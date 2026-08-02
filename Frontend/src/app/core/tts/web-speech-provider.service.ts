import { Injectable, signal } from '@angular/core';
import { TtsProvider } from './tts-provider';
import { TtsSpeakOptions, TtsVoiceInfo } from './tts.model';

/**
 * Web Speech API implementation. Known browser quirks handled here (V-10):
 *  - Chrome returns [] from getVoices() on the first call - must wait for
 *    the 'voiceschanged' event.
 *  - Chrome silently stops speaking ~15s into a long utterance - mitigated
 *    at the TtsService level by speaking one verse at a time, not here.
 *  - Chrome's synthesis engine can "fall asleep" if left paused for more
 *    than ~15s - a pause()/resume() keepalive heartbeat works around it.
 *  - iOS Safari requires speak() to happen synchronously inside a user
 *    gesture handler the first time - this class never awaits anything
 *    before calling speechSynthesis.speak(), so callers just need to call
 *    TtsService's play methods directly from a (click) handler.
 */
@Injectable()
export class WebSpeechProvider extends TtsProvider {
  readonly isSupported = typeof window !== 'undefined' && 'speechSynthesis' in window;

  private readonly _voices = signal<TtsVoiceInfo[]>([]);
  readonly voices = this._voices.asReadonly();

  private currentUtterance: SpeechSynthesisUtterance | null = null;
  private keepAliveHandle: ReturnType<typeof setInterval> | null = null;

  constructor() {
    super();
    if (!this.isSupported) {
      return;
    }
    this.refreshVoices();
    speechSynthesis.addEventListener('voiceschanged', () => this.refreshVoices());
  }

  private refreshVoices(): void {
    const voices = speechSynthesis.getVoices()
      .filter(v => v.lang.toLowerCase().startsWith('he'))
      .map(v => ({ voiceURI: v.voiceURI, name: v.name, lang: v.lang }));
    this._voices.set(voices);
  }

  speak(text: string, options: TtsSpeakOptions): Promise<void> {
    if (!this.isSupported) {
      return Promise.reject(new Error('speechSynthesis not supported'));
    }

    // A pending utterance's callbacks would otherwise fire after this new
    // one starts; cancel() before speak() avoids overlapping/ghost events.
    this.stopKeepAlive();
    speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = options.rate ?? 1;
    utterance.lang = 'he-IL';

    if (options.voiceURI) {
      const match = speechSynthesis.getVoices().find(v => v.voiceURI === options.voiceURI);
      if (match) {
        utterance.voice = match;
        utterance.lang = match.lang;
      }
    }

    this.currentUtterance = utterance;

    return new Promise<void>((resolve, reject) => {
      utterance.onend = () => {
        this.currentUtterance = null;
        resolve();
      };
      utterance.onerror = (e) => {
        this.currentUtterance = null;
        // 'canceled'/'interrupted' happen on every intentional stop()/next() -
        // not a real error, just resolve so callers don't need to special-case it.
        if (e.error === 'canceled' || e.error === 'interrupted') {
          resolve();
        } else {
          reject(new Error(e.error));
        }
      };
      speechSynthesis.speak(utterance);
    });
  }

  pause(): void {
    if (!this.isSupported) { return; }
    speechSynthesis.pause();
    this.startKeepAlive();
  }

  resume(): void {
    if (!this.isSupported) { return; }
    this.stopKeepAlive();
    speechSynthesis.resume();
  }

  cancel(): void {
    if (!this.isSupported) { return; }
    this.stopKeepAlive();
    speechSynthesis.cancel();
    this.currentUtterance = null;
  }

  private startKeepAlive(): void {
    this.stopKeepAlive();
    this.keepAliveHandle = setInterval(() => {
      if (speechSynthesis.paused) {
        speechSynthesis.resume();
        speechSynthesis.pause();
      }
    }, 10_000);
  }

  private stopKeepAlive(): void {
    if (this.keepAliveHandle !== null) {
      clearInterval(this.keepAliveHandle);
      this.keepAliveHandle = null;
    }
  }
}
