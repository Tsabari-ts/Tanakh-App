import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiCallService } from '../../services/api-call.service';
import { DialogService } from '../../services/dialog.service';
import { AppComponent } from '../../app.component';

@Component({
  selector: 'app-chapter',
  templateUrl: './chapter.component.html',
  styleUrl: './chapter.component.css'
})

export class ChapterComponent implements OnInit {
  section: string = "";
  chapter: string = "";
  book: string = "";
  title: string | null = "";
  keepReading: string | null = "";
  data: any;
  nextChapter: any;

  isScrolling = false;
  isScrollingDown = false;
  isScrollingUp = false;
  myInterval: any;
  clicks = 0;
  maxClicks = 3;
  speed = 1.5;
  upSpeed = 1;
  maxSpeed = 1000;
  @ViewChild('contentContainer') contentContainer!: ElementRef;


  downIcon: string = 'down-icon';
  stopIcon: string = 'stop-icon';
  upIcon: string = 'up-icon';
  nextIcon: string = 'next-icon';

  scrollDown() {
    if (this.isScrollingUp) {
      this.stopScrolling();
      this.isScrollingUp = false;
    }

    if (this.isScrolling) {
      this.increaseScrollSpeed();
    } else {
      this.myInterval = setInterval(() => this.scrollToEnd(), 100);
      this.isScrolling = true;
      this.isScrollingDown = true;
    }
  }

  scrollToEnd() {
    if (this.contentContainer.nativeElement.scrollTop + this.contentContainer.nativeElement.clientHeight >=
      this.contentContainer.nativeElement.scrollHeight) {
      this.stopScrolling();
    } else {
      this.contentContainer.nativeElement.scrollTop += this.speed;
    }
  }

  increaseScrollSpeed() {
    this.clicks++;

    if (this.speed + this.upSpeed <= this.maxSpeed) {
      this.speed += this.upSpeed;
    }

    if (this.clicks === this.maxClicks) {
      this.speed = 2;
      this.clicks = 0;
    }
  }

  scrollUp() {
    if (this.isScrollingDown) {
      this.stopScrolling();
      this.isScrollingDown = false;
    }

    if (this.isScrolling) {
      this.increaseScrollUpSpeed();
    } else {
      this.myInterval = setInterval(() => this.scrollToUp(), 100);
      this.isScrolling = true;
      this.isScrollingUp = true;
    }
  }

  scrollToUp() {
    if (this.contentContainer.nativeElement.scrollTop === 0) {
      this.stopScrolling();
    } else {
      this.contentContainer.nativeElement.scrollTop -= this.speed;
    }
  }

  increaseScrollUpSpeed() {
    this.clicks++;

    if (this.clicks >= 1) {
      this.speed += 1;
    }
    if (this.clicks === this.maxClicks) {
      this.speed = 2;
      this.clicks = 0;
    }
  }

  stopScrolling() {
    clearInterval(this.myInterval);
    this.isScrolling = false;
    this.speed = 2;
    this.clicks = 0;
  }

  ngOnDestroy() {
    this.stopScrolling();
  }

  constructor(private activatedRoute: ActivatedRoute,
    private apiService: ApiCallService,
    private router: Router,
    private appComponent: AppComponent,
    private dialogService: DialogService) {
    this.appComponent.showButton = true;
  }



  returnToHomePage() {
    this.router.navigate(['/homepage']);
  }

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(p => {
      this.section = p['section'];
      this.book = p['book'];
      this.chapter = p['chapterNumber'];
      this.keepReading = p['keepReading'];

      if (this.chapter != null && this.book != null) {
        this.apiService.getVerses(this.book, this.chapter).subscribe(data => {
          if (data.error) {
            console.log(data.error);
            return;
          }
          this.data = data.bookData.verses;
          this.title = data.bookData.hebrewSectionRef;
          this.nextChapter = data.bookData.nextChapter;
          console.log(this.data);
        }, (error) => {
          console.log(error)
        })
      }
    })

    this.createTitle(this.title);
  }

  createTitle(title: any) {
    this.title = title;
  }

  LoadLocalStorage(data: any) {
    localStorage['HasStorage'] = "true";
    localStorage['SectionRef'] = this.section + " " + data.bookData.sectionRef;
  }

  GetNextChapter() {
    let nextSection: string = this.nextChapter;
    let nextSectionParts: string[] = nextSection.split(' ');

    let book: string = '';
    let chapter: string = '';

    if (/^[IVXLCDM]+$/.test(nextSectionParts[0])) {
      book = nextSectionParts[0] + ' ' + nextSectionParts[1];
      chapter = nextSectionParts.slice(2).join(' ');
    } else if (nextSection.startsWith("Song of Songs")) {
      book = "Song of Songs";
      chapter = nextSection.replace("Song of Songs", '').trim();
    } else {
      book = nextSectionParts[0];
      chapter = nextSectionParts.slice(1).join(' ');
    }

    console.log('Book:', book);
    console.log('Chapter:', chapter);
    this.chapter = chapter;


    this.apiService.getVerses(book, chapter).subscribe(data => {
      if (data.error) {
        console.log(data.error);
        return;
      }
      this.data = data.bookData.verses;
      this.title = data.bookData.hebrewSectionRef;
      this.nextChapter = data.bookData.nextChapter;
      console.log(this.data);
      this.contentContainer.nativeElement.scrollTop = 0;

    }, (error) => {
      console.log(error)
    })
  }

  finishedReading() {
    let userHasConfirmedReading = this.keepReading == 'true';
    let sectionRef = {
      section: this.section,
      nextChapter: this.nextChapter
    };

    const data = {
      additionalData: {
        sectionRef: sectionRef,
        userHasConfirmedReading: userHasConfirmedReading,
      },
    };

    this.dialogService.openReadPermissionDialog(data);
  }
}