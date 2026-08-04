import { Component, ElementRef, OnInit, OnDestroy, ViewChild, ChangeDetectionStrategy, DestroyRef, inject, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';
import { ReadingHistoryService } from '../../services/reading-history.service';
import { ReaderPrefsService } from '../../services/reader-prefs.service';
import { TtsService } from '../../core/tts/tts.service';
import gematriya from 'gematriya';

const SCROLL_SPEEDS = [0, 0.5, 1.1, 2];

@Component({
    selector: 'app-chapter',
    templateUrl: './chapter.component.html',
    styleUrl: './chapter.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChapterComponent implements OnInit, OnDestroy {
  @ViewChild('readerScroll') private readerScrollRef?: ElementRef<HTMLElement>;

  readonly prefs = inject(ReaderPrefsService);
  readonly tts = inject(TtsService);

  section = '';
  book = '';
  chapterNumber = 1;
  heBook = '';
  private nextChapterRaw: string | null = null;
  private raf = 0;

  readonly data = signal<string[] | undefined>(undefined);
  readonly loadError = signal(false);
  readonly scrollSpeed = signal(0);

  /* Not a computed(): it would only read this.book/this.chapterNumber once
     and never again, since those are plain fields (set from route params,
     not signals) and computed() only tracks signal reads as dependencies -
     the mark-as-read button would then keep showing the previous chapter's
     state after navigating. Set explicitly wherever the chapter changes. */
  readonly isRead = signal(false);
  readonly speaking = computed(() => this.tts.state() === 'playing');
  readonly readerFontPx = computed(() => Math.round(20 * this.prefs.scale()));

  readonly verses = computed(() => {
    const data = this.data();
    if (!data) return [];
    return data.map((verse, i) => ({
      heb: gematriya(i + 1, { punctuate: false }),
      text: this.prefs.applyNikud(verse),
    }));
  });

  constructor(private activatedRoute: ActivatedRoute,
              private apiService: ApiCallService,
              private router: Router,
              private appComponent: AppComponent,
              private readingHistory: ReadingHistoryService,
              private destroyRef: DestroyRef) {
    this.appComponent.showButton.set(true);
    const snapshot = this.activatedRoute.snapshot.paramMap;
    this.appComponent.backTarget.set(['/books', snapshot.get('section') ?? 'torah', snapshot.get('book') ?? '']);
  }

  ngOnInit(): void {
    this.activatedRoute.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(p => {
        this.section = p['section'];
        this.book = p['book'];
        this.chapterNumber = parseInt(p['chapterNumber'], 10) || 1;
        this.appComponent.backTarget.set(['/books', this.section, this.book]);
        this.isRead.set(this.readingHistory.isRead(this.book, this.chapterNumber));
        this.loadChapter();
      });
  }

  ngOnDestroy(): void {
    cancelAnimationFrame(this.raf);
    this.tts.cancelForNavigation();
  }

  private loadChapter(): void {
    this.loadError.set(false);
    this.scrollSpeed.set(0);
    this.apiService.getVerses(this.book, String(this.chapterNumber))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: any) => {
          if (data.error) {
            this.loadError.set(true);
            return;
          }
          this.data.set(data.bookData.verses);
          this.heBook = data.bookData.heBook ?? data.bookData.hebrewSectionRef ?? this.book;
          this.nextChapterRaw = data.bookData.nextChapter ?? null;
          this.tts.loadChapter(data.bookData.verses, `${this.book}/${this.chapterNumber}`);
          if (this.readerScrollRef) {
            this.readerScrollRef.nativeElement.scrollTop = 0;
          }
        },
        error: () => this.loadError.set(true),
      });
  }

  chapterHeb(n: number): string {
    return gematriya(n, { punctuate: false });
  }

  toggleTTS(): void {
    if (this.speaking()) {
      this.tts.stop();
    } else {
      this.tts.playChapter();
    }
  }

  cycleScroll(): void {
    this.scrollSpeed.update(s => (s + 1) % SCROLL_SPEEDS.length);
    this.runScroll();
  }

  private runScroll(): void {
    cancelAnimationFrame(this.raf);
    const speed = this.scrollSpeed();
    const el = this.readerScrollRef?.nativeElement;
    if (!speed || !el) return;

    const px = SCROLL_SPEEDS[speed];
    const step = () => {
      const target = this.readerScrollRef?.nativeElement;
      if (!this.scrollSpeed() || !target) return;
      target.scrollTop += px;
      this.raf = requestAnimationFrame(step);
    };
    this.raf = requestAnimationFrame(step);
  }

  toggleRead(): void {
    this.readingHistory.toggleRead(this.book, this.chapterNumber);
    const nowRead = this.readingHistory.isRead(this.book, this.chapterNumber);
    this.isRead.set(nowRead);
    if (nowRead) {
      this.saveProgress();
    }
  }

  private saveProgress(): void {
    localStorage.setItem('HasStorage', 'true');
    localStorage.setItem('SectionRef', `${this.section} ${this.book} ${this.chapterNumber}`);

    const subscriberToken = localStorage.getItem('subscriberToken');
    if (subscriberToken) {
      this.apiService.updateReadingProgress({
        token: subscriberToken,
        book: this.book,
        chapter: this.chapterNumber,
      }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }
  }

  hasPrevChapter(): boolean {
    return this.chapterNumber > 1;
  }

  prevChapter(): void {
    if (!this.hasPrevChapter()) return;
    this.router.navigate([`/books/${this.section}/${this.book}/${this.chapterNumber - 1}/false`]);
  }

  hasNextChapter(): boolean {
    return !!this.nextChapterRaw;
  }

  nextChapter(): void {
    if (!this.nextChapterRaw) return;
    const { book, chapter } = this.parseNextChapter(this.nextChapterRaw);
    this.router.navigate([`/books/${this.section}/${book}/${chapter}/false`]);
  }

  private parseNextChapter(nextChapter: string): { book: string; chapter: string } {
    const parts = nextChapter.split(' ');
    if (/^[IVXLCDM]+$/.test(parts[0])) {
      return { book: `${parts[0]} ${parts[1]}`, chapter: parts.slice(2).join(' ') };
    }
    if (nextChapter.startsWith('Song of Songs')) {
      return { book: 'Song of Songs', chapter: nextChapter.replace('Song of Songs', '').trim() };
    }
    return { book: parts[0], chapter: parts.slice(1).join(' ') };
  }
}
