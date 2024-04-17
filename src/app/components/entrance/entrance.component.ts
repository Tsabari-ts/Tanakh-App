import { Component, ElementRef, OnInit } from '@angular/core';
import { ApiCallService } from '../../services/api-call.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-entrance',
  templateUrl: './entrance.component.html',
  styleUrl: './entrance.component.css'
})
export class EntranceComponent implements OnInit {
  isLoading: boolean = true;
  isHolidayOrShabat = false;
  
  words: string[] = [" ''וזאת", "התורה", "אשר", "שם", " ","משה", "לפני", "בני", "ישראל''"];

  currentIndex = 0;
  shownWords: string[] = [];


  constructor(private apiService:ApiCallService, private router: Router,
              private elementRef: ElementRef) { }
  
  ngOnInit(): void {
    this.apiService.getHolidays().subscribe(
      (response) => {
        this.isHolidayOrShabat = response;

        if(this.isHolidayOrShabat){
          this.isLoading = false;
          console.log('is shabes');
        }
        else{
          this.isLoading = false;
           this.showNextWord();
         }        
      },
      (error) => {
        console.error('Error loading data', error);
        this.isLoading = true;
      }
    );
  }

  showNextWord() {
    if (this.currentIndex < this.words.length) {
      this.shownWords.push(this.words[this.currentIndex]);
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
