import { Component, ElementRef, OnInit, ChangeDetectionStrategy, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiCallService } from '../../services/api-call.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-entrance',
    templateUrl: './entrance.component.html',
    styleUrl: './entrance.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class EntranceComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly isHolidayOrShabat = signal(false);

  words: string[] = [" ''וזאת", "התורה", "אשר", "שם", " ","משה", "לפני", "בני", "ישראל''"];

  currentIndex = 0;
  readonly shownWords = signal<string[]>([]);


  constructor(private apiService:ApiCallService, private router: Router,
              private elementRef: ElementRef, private destroyRef: DestroyRef) { }

  ngOnInit(): void {
    this.showNextWord();
    this.apiService.getHolidays()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(
      (response) => {
        this.isHolidayOrShabat.set(response);

        if(response){
          this.isLoading.set(false);
        }
        else{
          this.isLoading.set(false);
           this.showNextWord();
         }
      },
      () => {
        this.isLoading.set(true);
      }
    );
  }

  showNextWord() {
    if (this.currentIndex < this.words.length) {
      this.shownWords.update(words => [...words, this.words[this.currentIndex]]);
      this.currentIndex++;

      setTimeout(() => {
        void this.elementRef.nativeElement.offsetWidth;
        this.showNextWord();
      }, 500);
  }

  else{
     setTimeout(()=> {
        this.router.navigate(['/home']);
     },2000);
  }

}
}
