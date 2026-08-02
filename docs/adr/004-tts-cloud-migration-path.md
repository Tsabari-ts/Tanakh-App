# ADR 004: TTS provider abstraction and future cloud migration path

## Status

Accepted. `TtsService` depends on the abstract `TtsProvider` token
(`Frontend/src/app/core/tts/tts-provider.ts`), wired to `WebSpeechProvider`
in `app.config.ts`. No UI code talks to `speechSynthesis` directly.

## Context

The chapter reader's "read aloud" feature (V-01 through V-11) needed a TTS
engine. Two options: the browser's built-in Web Speech API (free, no
server, works offline) or a cloud TTS provider (Azure Speech, Google Cloud
TTS, ElevenLabs - paid, consistent voice quality, requires a backend
endpoint and audio hosting).

## Decision

Ship with the Web Speech API now, behind a provider abstraction, so
switching to a cloud provider later is additive rather than a rewrite.

## Known limitation of the current choice

Hebrew voice availability and quality varies significantly by platform:

| Platform | Typical Hebrew voice quality |
|---|---|
| iOS/macOS (Safari) | Relatively good - ships a real he-IL voice |
| Chrome/Android | Usually acceptable |
| Windows | Depends entirely on installed OS voices/language packs; commonly **none** |

`TtsService.unavailableReason` surfaces this gracefully (a visible message,
not a dead button) rather than papering over it, but it's a real gap for
users on unsupported platforms - see V-02.

## What the swap to a cloud provider looks like

1. Add a backend endpoint, e.g. `POST /api/v1/tts` that takes
   `{ book, chapter, verseIndex }` and returns audio (MP3/Opus).
2. **Synthesize once, cache forever**: chapter text is static, so each
   verse only ever needs synthesizing a single time. Cache the audio by a
   stable key (e.g. `{book}/{chapter}/{verseIndex}/{voice}`) behind a CDN or
   blob storage with a long-lived cache header - this is the entire cost
   argument for going to a paid provider being viable for a free app.
3. Implement `CloudTtsProvider extends TtsProvider` in
   `core/tts/cloud-tts-provider.service.ts`: `speak()` fetches (or plays
   from cache) the audio file for the verse and plays it via
   `HTMLAudioElement` instead of `SpeechSynthesisUtterance`; `pause`/
   `resume`/`cancel` map directly to the audio element's own methods, which
   don't have any of Web Speech's quirks (no ~15s cutoff, no pause-derived
   "falling asleep" bug, no getVoices()-empty-on-first-call race).
4. Swap the one line in `app.config.ts`:
   `{ provide: TtsProvider, useClass: CloudTtsProvider }`.
   `TtsService`, `TtsPlayerComponent`, and chapter.component need **no**
   changes - they only ever depended on the `TtsProvider` contract.

## Rough cost shape (order-of-magnitude, verify current pricing before committing)

- Azure Speech / Google Cloud TTS: priced per character synthesized,
  typically in the low dollars per million characters for standard voices,
  more for "neural"/premium voices. Since every verse is synthesized
  exactly once ever (cached permanently after), total lifetime cost is
  bounded by the size of the Tanakh itself, not by traffic - full Tanakh is
  roughly 1.2M words / ~5-6M characters, so a one-time synthesis pass
  across every verse/voice combination offered is a small, fixed cost.
- ElevenLabs: higher quality, subscription-tier pricing rather than
  pay-per-character; likely overkill for this use case but worth a listen
  test given Hebrew is a specialty case where cheaper engines vary widely
  in quality.

## When to revisit

Move to a cloud provider if:

- Windows users (a real segment, given the Hebrew-voice-availability gap
  above) are getting an unusably broken experience often enough to matter, or
- listening-quality complaints outweigh the zero server cost of Web Speech, or
- the "read aloud" feature turns out to be popular enough that investing in
  a one-time synthesis-and-cache pass is clearly worth it.

None of these are confirmed yet - ship Web Speech, watch usage/feedback,
revisit with real data rather than speculatively building the cloud path now.
