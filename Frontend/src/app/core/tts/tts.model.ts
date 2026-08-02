export type TtsPlaybackState = 'idle' | 'playing' | 'paused' | 'error';

export interface TtsVoiceInfo {
  voiceURI: string;
  name: string;
  lang: string;
}

export interface TtsSpeakOptions {
  voiceURI?: string;
  rate?: number;
}

/** How the TTS engine should read the Tetragrammaton (V-08). */
export type DivineNameReading = 'adonai' | 'hashem' | 'asWritten';

export interface TtsSettings {
  rate: number;
  voiceURI: string | null;
  divineNameReading: DivineNameReading;
  stopOnTabHidden: boolean;
}

export const TTS_SETTINGS_DEFAULTS: TtsSettings = {
  rate: 1,
  voiceURI: null,
  divineNameReading: 'adonai',
  stopOnTabHidden: true,
};

export const TTS_SETTINGS_STORAGE_KEY = 'tanach.tts.v1';
