import { DivineNameReading } from './tts.model';

// Hebrew cantillation marks (te'amim) - U+0591 to U+05AF. Near-universally
// unsupported by TTS engines and can produce garbled/garbage output.
const CANTILLATION = /[֑-֯]/g;

// Hebrew points (nikud) - U+05B0 to U+05C7 (includes dagesh/sin-shin dots).
const NIKUD = /[ְ-ׇ]/g;

// The Tetragrammaton, allowing any combining marks (nikud/cantillation)
// between the 4 base letters so it still matches vocalized text.
const MARK = '[\\u0591-\\u05C7]*';
const TETRAGRAMMATON = new RegExp(`י${MARK}ה${MARK}ו${MARK}ה${MARK}`, 'g');

const DIVINE_NAME_REPLACEMENTS: Record<DivineNameReading, string | null> = {
  adonai: 'אֲדֹנָי',
  hashem: 'הַשֵּׁם',
  asWritten: null,
};

// Standalone parashah/section break markers - פ)/ ס) or {פ}/{ס} conventions.
// Deliberately conservative (only matches the bracketed form) - a bare
// standalone פ/ס is indistinguishable from a real one-letter fragment
// without seeing actual Sefaria output, so it's left alone rather than
// risk chopping real text. Verify against real chapter data and extend
// if the source uses an unbracketed convention instead.
const SECTION_MARKER = /[({]\s*[פס]\s*['׳)}]/g;

// NOTE: verse-number markers (e.g. Sefaria's <sup> wrapping a Hebrew-letter
// verse number) are stripped as HTML tags by the backend (TanakhTextService)
// but their *text content* may survive if the marker isn't purely inside the
// tag. Not handled here: a same-length Hebrew letter/geresh combo at the
// start of a verse is indistinguishable from a real short first word (כי,
// אל, גם, ...), so a blanket strip would corrupt far more verses than it
// fixes. Needs checking against real API output before adding a rule -
// see docs/a11y-manual-test.md-style follow-up for this feature.

export interface TtsNormalizeOptions {
  keepNikud: boolean;
  divineNameReading: DivineNameReading;
}

export function normalizeVerseForSpeech(verse: string, options: TtsNormalizeOptions): string {
  let text = verse;

  const replacement = DIVINE_NAME_REPLACEMENTS[options.divineNameReading];
  if (replacement) {
    text = text.replace(TETRAGRAMMATON, replacement);
  }

  text = text.replace(SECTION_MARKER, '');
  text = text.replace(CANTILLATION, '');

  if (!options.keepNikud) {
    text = text.replace(NIKUD, '');
  }

  return text.trim();
}
