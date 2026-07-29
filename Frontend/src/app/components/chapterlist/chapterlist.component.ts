import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import gematriya from 'gematriya';
import { AppComponent } from '../../app.component';

@Component({
  selector: 'app-chapterlist',
  templateUrl: './chapterlist.component.html',
  styleUrl: './chapterlist.component.css'
})
export class ChapterlistComponent implements OnInit {
  section: string | null = "";
  book: string | null = "";
  chapters: number[] = [];

  constructor(private activatedRoute: ActivatedRoute,
              private router: Router,
              private bookService: BookService,              
              private appComponent: AppComponent) {
                this.appComponent.showButton = true;
               }

  ngOnInit(): void {
    this.chapters = this.bookService.getBookChapter();

      this.activatedRoute.params.subscribe(p => {
      this.section = p['section'];
      this.book = p['book'];
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
