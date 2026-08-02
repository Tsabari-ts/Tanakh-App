import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';

const SECTION_LABELS: Record<string, string> = {
  torah: $localize`:@@booklist.section.torah:תורה`,
  prophets: $localize`:@@booklist.section.prophets:נביאים`,
  writings: $localize`:@@booklist.section.writings:כתובים`,
};

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
  readonly heading = signal<string>($localize`:@@booklist.heading:ספרי התנ"ך`);

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
      const label = this.section != null ? SECTION_LABELS[this.section.toLowerCase()] : undefined;
      if (label) {
        this.heading.set(label);
      }

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
