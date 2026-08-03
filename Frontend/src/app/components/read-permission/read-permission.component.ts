import { Component, Inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { CdkScrollable } from '@angular/cdk/scrolling';

@Component({
    selector: 'app-read-permission',
    templateUrl: './read-permission.component.html',
    styleUrl: './read-permission.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogTitle, MatIcon, CdkScrollable, MatDialogContent, MatDialogActions]
})

export class ReadPermissionComponent implements OnInit{
  userHasConfirmedReading = false;
  readonly isButtonDisabled = signal(false);
  readonly isSavedInProgress = signal(false);
  readonly isSavedSuccessful = signal(false);
  readonly progressValue = signal(0);
  loadingInterval: any;
  book: string = '';
  private hasStorage = 'HasStorage';
  private sectionRef = 'SectionRef';

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<ReadPermissionComponent>) {
      this.userHasConfirmedReading = this.data.additionalData.userHasConfirmedReading;
    }

    ngOnInit(): void {
      if(this.userHasConfirmedReading){
        this.startLoading();
      }
    }

    startLoading(): void {
  const duration = 3000;
  const interval = 30;
  const steps = (duration / interval);
  const stepSize = 100 / steps;

  this.loadingInterval = setInterval(() => {
    if (this.progressValue() < 200) {
      this.progressValue.update(v => v + stepSize);
    } else {
      this.stopLoading();
    }
  }, 10);

  setTimeout(() => {
    this.stopLoading();
  }, duration);
}

stopLoading(): void {
  clearInterval(this.loadingInterval);
  if(this.userHasConfirmedReading){
    this.saveSectionToLocalStorage();
    this.dialogRef.close();
  }
}

    closeDialog(): void {
    this.dialogRef.close();
  }

  saveAndClose(): void {
    this.isButtonDisabled.set(true);
    this.isSavedInProgress.set(true);
   this.startLoading();

    setTimeout(() => {
      this.stopLoading();

      setTimeout(() => {
        this.saveSectionToLocalStorage();
        this.isSavedInProgress.set(false);
        this.isSavedSuccessful.set(true);

        setTimeout(() => {
          this.dialogRef.close();
        }, 2000);
      }, 1000);
    }, 1000);
  }

  saveSectionToLocalStorage(): void {
    let section = this.data.additionalData.sectionRef.section;
    let nextChapter = this.data.additionalData.sectionRef.nextChapter;
    let sectionData = section + " " + nextChapter;

    localStorage.setItem(this.hasStorage, 'true');
    localStorage.setItem(this.sectionRef, sectionData);
  }
}
