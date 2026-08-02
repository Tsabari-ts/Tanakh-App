import { Component, ElementRef, OnInit, ViewChild, ChangeDetectionStrategy, DestroyRef, inject, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { DialogService } from '../../services/dialog.service';
import { AppComponent } from '../../app.component';
import { NgClass } from '@angular/common';
import { ScrollToTopButtonComponent } from '../scroll-to-top-button/scroll-to-top-button.component';
import { TtsService } from '../../core/tts/tts.service';
import { TtsPlayerComponent } from '../../shared/tts/tts-player/tts-player.component';

@Component({
    selector: 'app-chapter',
    templateUrl: './chapter.component.html',
    styleUrl: './chapter.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgClass, ScrollToTopButtonComponent, TtsPlayerComponent]
})

export class ChapterComponent implements OnInit {
  section: string = "";
  chapter: string = "";
  book: string = "";
  readonly title = signal<string | null>("");
  keepReading: string | null = "";
  readonly data = signal<string[] | undefined>(undefined);
  nextChapter: any;
  readonly loadError = signal(false);

  isScrolling = false;
  isScrollingDown = false;
  isScrollingUp = false;
  myInterval: any;
  clicks = 0;
  maxClicks = 3;
  speed = 1.5;
  upSpeed = 1;
  maxSpeed = 1000;
  @ViewChild('contentContainer') contentContainer!: ElementRef;


  downIcon: string = 'down-icon';
  stopIcon: string = 'stop-icon';
  upIcon: string = 'up-icon';
  nextIcon: string = 'next-icon';

  readonly tts = inject(TtsService);

  private scrollToActiveVerse = effect(() => {
    const index = this.tts.activeVerseIndex();
    if (index === null) {
      return;
    }
    const el = document.getElementById(`verse-${index}`);
    // scrollIntoView never moves keyboard focus (only .focus() does), so
    // this can't steal focus away from wherever the user was tabbing.
    const reducedMotion = document.documentElement.classList.contains('a11y-no-motion')
      || window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    el?.scrollIntoView({ behavior: reducedMotion ? 'auto' : 'smooth', block: 'center' });
  });

  readFromVerse(index: number): void {
    this.tts.playFromVerse(index);
  }

  scrollDown() {
    if (this.isScrollingUp) {
      this.stopScrolling();
      this.isScrollingUp = false;
    }

    if (this.isScrolling) {
      this.increaseScrollSpeed();
    } else {
      this.myInterval = setInterval(() => this.scrollToEnd(), 100);
      this.isScrolling = true;
      this.isScrollingDown = true;
    }
  }

  scrollToEnd() {
    if (this.contentContainer.nativeElement.scrollTop + this.contentContainer.nativeElement.clientHeight >=
      this.contentContainer.nativeElement.scrollHeight) {
      this.stopScrolling();
    } else {
      this.contentContainer.nativeElement.scrollTop += this.speed;
    }
  }

  increaseScrollSpeed() {
    this.clicks++;

    if (this.speed + this.upSpeed <= this.maxSpeed) {
      this.speed += this.upSpeed;
    }

    if (this.clicks === this.maxClicks) {
      this.speed = 2;
      this.clicks = 0;
    }
  }

  scrollUp() {
    if (this.isScrollingDown) {
      this.stopScrolling();
      this.isScrollingDown = false;
    }

    if (this.isScrolling) {
      this.increaseScrollUpSpeed();
    } else {
      this.myInterval = setInterval(() => this.scrollToUp(), 100);
      this.isScrolling = true;
      this.isScrollingUp = true;
    }
  }

  scrollToUp() {
    if (this.contentContainer.nativeElement.scrollTop === 0) {
      this.stopScrolling();
    } else {
      this.contentContainer.nativeElement.scrollTop -= this.speed;
    }
  }

  increaseScrollUpSpeed() {
    this.clicks++;

    if (this.clicks >= 1) {
      this.speed += 1;
    }
    if (this.clicks === this.maxClicks) {
      this.speed = 2;
      this.clicks = 0;
    }
  }

  stopScrolling() {
    clearInterval(this.myInterval);
    this.isScrolling = false;
    this.speed = 2;
    this.clicks = 0;
  }

  ngOnDestroy() {
    this.stopScrolling();
    // speechSynthesis keeps talking in the background after navigating away
    // otherwise (V-10).
    this.tts.cancelForNavigation();
  }

  constructor(private activatedRoute: ActivatedRoute,
    private apiService: ApiCallService,
    private router: Router,
    private appComponent: AppComponent,
    private dialogService: DialogService,
    private destroyRef: DestroyRef) {
    this.appComponent.showButton.set(true);
  }



  returnToHomePage() {
    this.router.navigate(['/home']);
  }

  ngOnInit(): void {
    this.activatedRoute.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(q => {
      const subscriberToken = q['sid'];
      if (subscriberToken) {
        localStorage.setItem('subscriberToken', subscriberToken);
      }
    });

    this.activatedRoute.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(p => {
      this.section = p['section'];
      this.book = p['book'];
      this.chapter = p['chapterNumber'];
      this.keepReading = p['keepReading'];

      if (this.chapter != null && this.book != null) {
        this.apiService.getVerses(this.book, this.chapter)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(data => {
          if (data.error) {
            console.log(data.error);
            this.loadError.set(true);
            return;
          }
          this.data.set(data.bookData.verses);
          this.tts.loadChapter(data.bookData.verses, `${this.book}/${this.chapter}`);
          this.title.set(data.bookData.hebrewSectionRef);
          this.nextChapter = data.bookData.nextChapter;
          this.reportReadingProgress();
        }, () => {
          this.loadError.set(true);
        })
      }
    })

    this.createTitle(this.title());
  }

  reportReadingProgress(): void {
    const subscriberToken = localStorage.getItem('subscriberToken');
    const chapterNumber = parseInt(this.chapter, 10);

    if (!subscriberToken || !this.book || isNaN(chapterNumber)) {
      return;
    }

    this.apiService.updateReadingProgress({
      token: subscriberToken,
      book: this.book,
      chapter: chapterNumber
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  createTitle(title: any) {
    this.title.set(title);
  }

  GetNextChapter() {
    let nextSection: string = this.nextChapter;
    let nextSectionParts: string[] = nextSection.split(' ');

    let book: string = '';
    let chapter: string = '';

    if (/^[IVXLCDM]+$/.test(nextSectionParts[0])) {
      book = nextSectionParts[0] + ' ' + nextSectionParts[1];
      chapter = nextSectionParts.slice(2).join(' ');
    } else if (nextSection.startsWith("Song of Songs")) {
      book = "Song of Songs";
      chapter = nextSection.replace("Song of Songs", '').trim();
    } else {
      book = nextSectionParts[0];
      chapter = nextSectionParts.slice(1).join(' ');
    }

    this.book = book;
    this.chapter = chapter;


    this.apiService.getVerses(book, chapter)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(data => {
      if (data.error) {
        console.log(data.error);
        this.loadError.set(true);
        return;
      }
      this.data.set(data.bookData.verses);
      this.tts.loadChapter(data.bookData.verses, `${book}/${chapter}`);
      this.title.set(data.bookData.hebrewSectionRef);
      this.nextChapter = data.bookData.nextChapter;
      this.contentContainer.nativeElement.scrollTop = 0;
      this.reportReadingProgress();

    }, () => {
      this.loadError.set(true);
    })
  }

  finishedReading() {
    let userHasConfirmedReading = this.keepReading == 'true';
    let sectionRef = {
      section: this.section,
      nextChapter: this.nextChapter
    };

    const data = {
      additionalData: {
        sectionRef: sectionRef,
        userHasConfirmedReading: userHasConfirmedReading,
      },
    };

    this.dialogService.openReadPermissionDialog(data);
  }
}
