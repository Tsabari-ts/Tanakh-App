import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { ApiCallService } from '../../services/api-call.service';
import gematriya from 'gematriya';
import { AppComponent } from '../../app.component';

@Component({
    selector: 'app-chapterlist',
    templateUrl: './chapterlist.component.html',
    styleUrl: './chapterlist.component.css',
    changeDetection: ChangeDetectionStrategy.Eager // TODO(F-03): remove after signals migration
})
export class ChapterlistComponent implements OnInit {
  section: string | null = "";
  book: string | null = "";
  chapters: number[] = [];
  loadError = false;

  constructor(private activatedRoute: ActivatedRoute,
              private router: Router,
              private bookService: BookService,
              private apiService: ApiCallService,
              private appComponent: AppComponent) {
                this.appComponent.showButton = true;
               }

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(p => {
      this.section = p['section'];
      this.book = p['book'];

      if (this.book) {
        this.apiService.getBookByTitle(this.book).subscribe(data => {
          const bookData = Array.isArray(data) ? data[0] : data;
          if (bookData) {
            this.bookService.setBookData(bookData);
            this.chapters = this.bookService.getBookChapter();
          }
        }, (error) => {
          console.log(error);
          this.loadError = true;
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
