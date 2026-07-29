import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';

@Component({
  selector: 'app-booklist',
  templateUrl: './booklist.component.html',
  styleUrl: './booklist.component.css'
})

export class BooklistComponent implements OnInit {
  section: string | null = "";
  data: any;

    constructor(private activatedRoute: ActivatedRoute,
                private apiService: ApiCallService,
                private router: Router,
                private bookService: BookService,
                private appComponent: AppComponent) {
                  this.appComponent.showButton = true;
                 }

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(p => {
      this.section = p['section'];
      
      if (this.section != null) {
        this.apiService.getBookList(this.section).subscribe(data => {
          if (data.error) {
            console.log(data.error);
            return;
          }
          this.data = data;

          console.log(this.data);  
        }, (error) => {
          console.log(error)
        })
      }
    })
  }

  goTo(path:any){
    let section = path.section;
    let book = path.title;
    this.bookService.setBookData(path);    
    this.router.navigate([`/books/${section}/${book}`]); 
    }

}
