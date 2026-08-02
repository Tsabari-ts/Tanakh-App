import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { TtsService } from '../../../core/tts/tts.service';
import { DivineNameReading } from '../../../core/tts/tts.model';

@Component({
  selector: 'app-tts-player',
  standalone: true,
  templateUrl: './tts-player.component.html',
  styleUrl: './tts-player.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TtsPlayerComponent {
  readonly tts = inject(TtsService);

  readonly isPlaying = computed(() => this.tts.state() === 'playing');
  readonly rateLabel = computed(() => `${this.tts.settings().rate}x`);

  readonly playPauseLabelPlaying = $localize`:@@tts.player.pause:השהיית הקראה`;
  readonly playPauseLabelPaused = $localize`:@@tts.player.play:הפעלת הקראה`;

  togglePlayPause(): void {
    const state = this.tts.state();
    if (state === 'playing') {
      this.tts.pause();
    } else if (state === 'paused') {
      this.tts.resume();
    } else {
      this.tts.playChapter();
    }
  }

  stop(): void {
    this.tts.stop();
  }

  previous(): void {
    this.tts.previous();
  }

  next(): void {
    this.tts.next();
  }

  onRateChange(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.tts.setRate(value);
  }

  onVoiceChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.tts.setVoice(value || null);
  }

  onDivineNameChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as DivineNameReading;
    this.tts.setDivineNameReading(value);
  }

  /** Space toggles play/pause, but only while focus is inside the player -
   *  never globally, so it doesn't hijack Space on the rest of the page. */
  onPlayerKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const isFormControl = ['INPUT', 'SELECT', 'TEXTAREA'].includes(target.tagName);
    if (event.code === 'Space' && !isFormControl) {
      event.preventDefault();
      this.togglePlayPause();
    }
  }
}
