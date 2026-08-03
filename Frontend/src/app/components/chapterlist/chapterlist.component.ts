import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { ApiCallService } from '../../services/api-call.service';
import gematriya from 'gematriya';
import { AppComponent } from '../../app.component';
import { ReadingHistoryService } from '../../services/reading-history.service';

@Component({
    selector: 'app-chapterlist',
    templateUrl: './chapterlist.component.html',
    styleUrl: './chapterlist.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChapterlistComponent implements OnInit {
  section: string | null = "";
  book: string | null = "";
  heBook: string | null = "";
  readonly chapters = signal<number[]>([]);
  readonly loadError = signal(false);

  readonly readCount = computed(() =>
    this.book ? this.chapters().filter((_, i) => this.readingHistory.isRead(this.book!, i + 1)).length : 0);

  readonly chapterCells = computed(() =>
    this.chapters().map((verseCount, i) => ({
      n: i + 1,
      heb: this.getChapterName(i + 1),
      read: this.book ? this.readingHistory.isRead(this.book, i + 1) : false,
    })));

  constructor(private activatedRoute: ActivatedRoute,
              private router: Router,
              private bookService: BookService,
              private apiService: ApiCallService,
              private appComponent: AppComponent,
              private readingHistory: ReadingHistoryService,
              private destroyRef: DestroyRef) {
                this.appComponent.showButton.set(true);
                this.appComponent.backTarget.set(['/books', this.activatedRoute.snapshot.paramMap.get('section') ?? 'torah']);
               }

  ngOnInit(): void {
    this.activatedRoute.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(p => {
      this.section = p['section'];
      this.book = p['book'];

      if (this.book) {
        this.apiService.getBookByTitle(this.book)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(data => {
          const bookData = Array.isArray(data) ? data[0] : data;
          if (bookData) {
            this.bookService.setBookData(bookData);
            this.heBook = bookData.heBook ?? bookData.heTitle ?? this.book;
            this.chapters.set(this.bookService.getBookChapter());
          }
        }, () => {
          this.loadError.set(true);
        });
      }
    })
  }

  goTo(path:any){
    let book = path.title;
    this.router.navigate([`/books/${book}`]);
  }

  getChapterName(chapterNumber: number): string {
    return gematriya(chapterNumber, { punctuate: false });
  }

  goToChapter(chapterNumber: number): void {
    if (this.book) {
      this.router.navigate([`/books/${this.section}/${this.book}/${chapterNumber}/false`]);
    }
  }
}
