import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { ApiCallService } from '../../services/api-call.service';
import gematriya from 'gematriya';
import { AppComponent } from '../../app.component';

@Component({
    selector: 'app-chapterlist',
    templateUrl: './chapterlist.component.html',
    styleUrl: './chapterlist.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChapterlistComponent implements OnInit {
  section: string | null = "";
  book: string | null = "";
  readonly chapters = signal<number[]>([]);
  readonly loadError = signal(false);

  constructor(private activatedRoute: ActivatedRoute,
              private router: Router,
              private bookService: BookService,
              private apiService: ApiCallService,
              private appComponent: AppComponent,
              private destroyRef: DestroyRef) {
                this.appComponent.showButton.set(true);
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
