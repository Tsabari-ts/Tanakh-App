import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';

@Component({
    selector: 'app-booklist',
    templateUrl: './booklist.component.html',
    styleUrl: './booklist.component.css',
    changeDetection: ChangeDetectionStrategy.Eager // TODO(F-03): remove after signals migration
})

export class BooklistComponent implements OnInit {
  section: string | null = "";
  data: any;
  loadError = false;

    constructor(private activatedRoute: ActivatedRoute,
                private apiService: ApiCallService,
                private router: Router,
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
            this.loadError = true;
            return;
          }
          this.data = data;
        }, (error) => {
          console.log(error);
          this.loadError = true;
        })
      }
    })
  }

  goTo(path:any){
    let section = path.section;
    let book = path.title;
    this.router.navigate([`/books/${section}/${book}`]);
    }

}
