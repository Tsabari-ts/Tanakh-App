import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-read-permission',
  templateUrl: './read-permission.component.html',
  styleUrl: './read-permission.component.css'
})

export class ReadPermissionComponent implements OnInit{
  userHasConfirmedReading = false;
  isButtonDisabled = false;
  isSavedInProgress = false;
  isSavedSuccessful = false;
  progressValue = 0;
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
    if (this.progressValue < 200) {
      this.progressValue += stepSize;
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
    this.isButtonDisabled = true;
    this.isSavedInProgress = true;
   this.startLoading();

    setTimeout(() => {
      this.stopLoading();
    
      setTimeout(() => {
        this.saveSectionToLocalStorage();
        this.isSavedInProgress = false;
        this.isSavedSuccessful = true;
    
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