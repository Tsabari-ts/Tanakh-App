import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';

@Component({
    selector: 'app-booklist',
    templateUrl: './booklist.component.html',
    styleUrl: './booklist.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class BooklistComponent implements OnInit {
  section: string | null = "";
  readonly data = signal<any>(undefined);
  readonly loadError = signal(false);

    constructor(private activatedRoute: ActivatedRoute,
                private apiService: ApiCallService,
                private router: Router,
                private appComponent: AppComponent,
                private destroyRef: DestroyRef) {
                  this.appComponent.showButton.set(true);
                 }

  ngOnInit(): void {
    this.activatedRoute.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(p => {
      this.section = p['section'];

      if (this.section != null) {
        this.apiService.getBookList(this.section)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(data => {
          if (data.error) {
            console.log(data.error);
            this.loadError.set(true);
            return;
          }
          this.data.set(data);
        }, () => {
          this.loadError.set(true);
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
