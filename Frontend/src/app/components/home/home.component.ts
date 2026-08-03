import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { DialogService } from '../../services/dialog.service';
import { AppComponent } from '../../app.component';
import { NgClass } from '@angular/common';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgClass]
})
export class HomeComponent implements OnInit {
  showButtons: boolean = true;
  userHasSeenWelcomeModal = localStorage.getItem('userHasSeenWelcomeModal');
  hasStorage = localStorage.getItem('HasStorage');

  constructor(private router: Router,
              private dialogService: DialogService,
              private appComponent: AppComponent,){
                this.appComponent.showButton.set(false);
               }  
  
  ngOnInit(){
    if (!this.userHasSeenWelcomeModal) {
      this.dialogService.openWelcomeDialog();
    }
}
  
  tanakhButtons:any[]=[
    {text: $localize`:@@home.torah:תורה`, value: 'torah', iconClass: 'torah-icon'},
    {text: $localize`:@@home.prophets:נביאים`, value: 'prophets', iconClass: 'torah-icon'},
    {text: $localize`:@@home.writings:כתובים`, value: 'writings', iconClass: 'torah-icon'},
  ]

  buttons:any[]=[
    {text: this.hasStorage ? $localize`:@@home.continueReading:המשך מהפרק האחרון` : $localize`:@@home.startReading:התחל קריאה`, value: 'show-sentence', iconClass: 'BookWithBookmark-icon'},
    {text: $localize`:@@home.settings:הגדרות`, value: 'settings', iconClass: 'stteing-icon'}
  ]
 
  goTo(path:any){
let section = path.value;

if(section === 'show-sentence'){
      if(this.hasStorage === "true"){
        this.goToChapter();
      }
      else{
        this.router.navigate([`/books/Torah/Genesis/1/true`]); 
      }
    }
    else if(section === 'settings'){
      this.router.navigate([`/${section}`]); 
    }
    else{
      this.router.navigate([`/books/${section}`]);
    }
  }

  goToChapter() {
    let sectionRef = localStorage.getItem('SectionRef');
    if(sectionRef != null){
      let nextSection = sectionRef.split(' ');
      let section = nextSection[0];
      let book = nextSection.slice(1, -1).join(' ');
      let chapterNumber = (parseInt(nextSection[nextSection.length - 1], 10)).toString();      
      
      this.router.navigate([`/books/${section}/${book}/${chapterNumber}/true`]); 
    }
  }

}
