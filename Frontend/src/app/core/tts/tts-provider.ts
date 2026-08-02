import { Signal } from '@angular/core';
import { TtsSpeakOptions, TtsVoiceInfo } from './tts.model';

/**
 * Provider-agnostic TTS contract. TtsService (and the player UI) only ever
 * talk to this - swapping WebSpeechProvider for a future CloudTtsProvider
 * (see docs/adr/00X-cloud-tts.md, V-12) is a one-line change in
 * app.config.ts's `{ provide: TtsProvider, useClass: ... }`, nothing else
 * needs to change.
 */
export abstract class TtsProvider {
  abstract readonly isSupported: boolean;
  abstract readonly voices: Signal<TtsVoiceInfo[]>;

  /** Resolves when this utterance finishes; rejects on error or cancel(). */
  abstract speak(text: string, options: TtsSpeakOptions): Promise<void>;
  abstract pause(): void;
  abstract resume(): void;
  abstract cancel(): void;
}
