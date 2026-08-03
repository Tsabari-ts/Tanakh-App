import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { AppComponent } from '../../app.component';
import { ReadingHistoryService } from '../../services/reading-history.service';

const SECTIONS: { id: string; label: string; sub: string }[] = [
  { id: 'torah', label: $localize`:@@booklist.section.torah:תורה`, sub: $localize`:@@booklist.section.torah.sub:חמישה חומשים` },
  { id: 'prophets', label: $localize`:@@booklist.section.prophets:נביאים`, sub: $localize`:@@booklist.section.prophets.sub:נביאים ראשונים ואחרונים` },
  { id: 'writings', label: $localize`:@@booklist.section.writings:כתובים`, sub: $localize`:@@booklist.section.writings.sub:תהילים, משלי ועוד` },
];

type BooksVariation = 'cards' | 'list' | 'grid';
const VARIATION_KEY = 'tanach.bookVariation';

@Component({
    selector: 'app-booklist',
    templateUrl: './booklist.component.html',
    styleUrl: './booklist.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BooklistComponent implements OnInit {
  readonly heading = $localize`:@@booklist.heading:ספרי התנ"ך`;
  readonly loadError = signal(false);
  readonly variation = signal<BooksVariation>(this.loadVariation());

  readonly variationOptions: { id: BooksVariation; label: string }[] = [
    { id: 'cards', label: $localize`:@@booklist.variation.cards:כרטיסים` },
    { id: 'list', label: $localize`:@@booklist.variation.list:רשימה` },
    { id: 'grid', label: $localize`:@@booklist.variation.grid:תגיות` },
  ];

  private readonly sectionData = signal<Record<string, any[]>>({});

  readonly sections = computed(() => {
    const data = this.sectionData();
    return SECTIONS.map(s => ({
      id: s.id,
      label: s.label,
      sub: s.sub,
      books: (data[s.id] ?? []).map((item: any) => {
        const total: number = item.chapters?.length ?? 0;
        const read = this.readingHistory.countForBook(item.title);
        const pct = total ? Math.round((read / total) * 100) : 0;
        return { ...item, section: item.section ?? s.id, total, read, pct };
      }),
    }));
  });

  constructor(private apiService: ApiCallService,
              private router: Router,
              private appComponent: AppComponent,
              private readingHistory: ReadingHistoryService,
              private destroyRef: DestroyRef) {
    this.appComponent.showButton.set(true);
    this.appComponent.backTarget.set(['/home']);
  }

  ngOnInit(): void {
    for (const s of SECTIONS) {
      this.apiService.getBookList(s.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: data => {
            if (data.error) {
              this.loadError.set(true);
              return;
            }
            this.sectionData.update(d => ({ ...d, [s.id]: data }));
          },
          error: () => this.loadError.set(true),
        });
    }
  }

  setVariation(variation: BooksVariation): void {
    this.variation.set(variation);
    localStorage.setItem(VARIATION_KEY, variation);
  }

  goTo(item: any): void {
    this.router.navigate([`/books/${item.section}/${item.title}`]);
  }

  private loadVariation(): BooksVariation {
    const stored = localStorage.getItem(VARIATION_KEY);
    return stored === 'cards' || stored === 'list' || stored === 'grid' ? stored : 'cards';
  }
}
