import { Component, OnInit, ChangeDetectionStrategy, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DialogService } from '../../services/dialog.service';
import { AppComponent } from '../../app.component';
import { ReadingHistoryService } from '../../services/reading-history.service';
import { ApiCallService } from '../../services/api-call.service';
import { getStoredUsername } from '../../shared/user-prefs';
import gematriya from 'gematriya';

interface LastPosition {
  section: string;
  book: string;
  chapter: number;
}

const DEFAULT_POSITION: LastPosition = { section: 'torah', book: 'Genesis', chapter: 1 };
const DEFAULT_HE_BOOK = 'בראשית';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent implements OnInit {
  userHasSeenWelcomeModal = localStorage.getItem('userHasSeenWelcomeModal');
  hasStorage = localStorage.getItem('HasStorage') === 'true';
  username = getStoredUsername();
  greeting = this.computeGreeting();
  lastPosition = this.parseSectionRef();

  readonly readTotal = computed(() => this.readingHistory.totalRead());
  readonly streak = computed(() => this.readingHistory.streak());
  readonly heBook = signal(DEFAULT_HE_BOOK);

  readonly lastLabel = computed(() => {
    const { chapter } = this.lastPosition ?? DEFAULT_POSITION;
    return `${this.heBook()} · פרק ${this.chapterHeb(chapter)}`;
  });

  readonly lastSnippet = this.lastPosition
    ? $localize`:@@home.continueSnippet:המשיכו לקרוא מהמקום שבו הפסקתם`
    : $localize`:@@home.startSnippet:התחילו את מסע הקריאה שלכם מבראשית פרק א׳`;

  constructor(private router: Router,
              private dialogService: DialogService,
              private appComponent: AppComponent,
              private readingHistory: ReadingHistoryService,
              private apiService: ApiCallService){
                this.appComponent.showButton.set(false);
               }

  ngOnInit(){
    if (!this.userHasSeenWelcomeModal) {
      this.dialogService.openWelcomeDialog();
    }

    if (this.lastPosition) {
      this.apiService.getBookByTitle(this.lastPosition.book).subscribe({
        next: (data: any) => {
          const bookData = Array.isArray(data) ? data[0] : data;
          const heBook = bookData?.heBook ?? bookData?.heTitle;
          if (heBook) this.heBook.set(heBook);
        },
        error: () => { /* keep default Hebrew name on failure */ },
      });
    }
  }

  private computeGreeting(): string {
    const hour = new Date().getHours();
    const greeting = hour < 12 ? $localize`:@@home.greetingMorning:בוקר טוב`
      : hour < 18 ? $localize`:@@home.greetingAfternoon:צהריים טובים`
      : $localize`:@@home.greetingEvening:ערב טוב`;
    return this.username ? `${greeting}, ${this.username}` : greeting;
  }

  private parseSectionRef(): LastPosition | null {
    if (!this.hasStorage) return null;
    const sectionRef = localStorage.getItem('SectionRef');
    if (!sectionRef) return null;

    const parts = sectionRef.split(' ');
    const section = parts[0];
    const book = parts.slice(1, -1).join(' ');
    const chapter = parseInt(parts[parts.length - 1], 10);
    if (!book || isNaN(chapter)) return null;

    return { section, book, chapter };
  }

  chapterHeb(n: number): string {
    return gematriya(n, { punctuate: false });
  }

  continueReading(): void {
    const { section, book, chapter } = this.lastPosition ?? DEFAULT_POSITION;
    this.router.navigate([`/books/${section}/${book}/${chapter}/true`]);
  }

  openBooks(): void {
    this.router.navigate(['/books/torah']);
  }
}
