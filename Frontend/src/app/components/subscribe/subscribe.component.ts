import { Component, EventEmitter, Inject, Output, ChangeDetectionStrategy } from '@angular/core';
import { ApiCallService } from '../../services/api-call.service';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
    selector: 'app-subscribe',
    templateUrl: './subscribe.component.html',
    styleUrl: './subscribe.component.css',
    changeDetection: ChangeDetectionStrategy.Eager, // TODO(F-03): remove after signals migration
    standalone: false
})

export class SubscribeComponent {
  @Output() subscriptionStatusChange: EventEmitter<{
     newButtonName: string }> = new EventEmitter();
  private markSubscribeKey = 'userHasSubscribed';
  serverResponse: string = '';
  subscribeSuccessful = false;
  userHasSubscribed = false;
  isButtonDisabled: boolean = false;
  isRequestInProgress: boolean = false;
  isRequestSuccessful: boolean = false;
  progressValue = 0;
  loadingInterval: any;

  emailValue: string = '';
  displayNameValue: string = '';
  timeValue: string = '';
  skipShabbatHolidays: boolean = true;
  consentGiven: boolean = false;
  timeOptions: string[] = this.generateTimeOptions();

  constructor(@Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<SubscribeComponent>,
    private apiService: ApiCallService) {
      this.userHasSubscribed = localStorage.getItem(this.markSubscribeKey) === 'true';
    }

  generateTimeOptions(): string[] {
    const options: string[] = [];
    for (let hour = 8; hour <= 20; hour++) {
      options.push(`${hour}:00`);
    }
    return options;
  }

  closeDialog() {
    this.dialogRef.close();
  }

  submitForm(form: any) {
    if (form.valid && this.consentGiven) {
      this.closeAndSubscribe();
    } else {
      form.submitted = true;
    }
  }

  closeAndSubscribe() {
    this.isButtonDisabled = true;
    this.isRequestInProgress = true;
    this.startLoading();

    const subscriptionRequest = {
      email: this.emailValue,
      displayName: this.displayNameValue || null,
      preferredTime: this.timeValue,
      skipShabbatHolidays: this.skipShabbatHolidays,
      consent: this.consentGiven
    };

    this.apiService.subscribe(subscriptionRequest).subscribe(() => {
      this.subscribeSuccessful = true;
      this.markSubscribe();
    }, (error) => {
      console.log(error);
      this.subscribeSuccessful = false;
    },
    () => {
      setTimeout(() => {
        this.stopLoading();
        this.setSubscribeServerResponse();
      }, 3000);
    });
  }

  setSubscribeServerResponse() {
    setTimeout(() => {
      if (this.subscribeSuccessful) {
        this.serverResponse = 'שלחנו לך מייל אישור - יש ללחוץ על הקישור בו כדי להשלים את ההרשמה.';
        this.subscriptionStatusChange.emit({
          newButtonName: 'נרשמת לתזכורת' });
      } else {
        this.serverResponse = 'הרישום נכשל, אנא נסה שוב מאוחר יותר';
      }
      this.isRequestInProgress = false;
      this.isRequestSuccessful = true;

      setTimeout(() => {
        this.dialogRef.close();
      }, 3000);
    }, 3000);
  }

  startLoading(): void {
    this.progressValue = 0;
    const duration = 3000;
    const interval = 10;
    const steps = (duration / interval);
    const stepSize = 100 / steps;

    this.loadingInterval = setInterval(() => {
      if (this.progressValue < 100) {
        this.progressValue += stepSize;
      } else {
        this.stopLoading();
      }
    }, interval);

    setTimeout(() => {
      this.stopLoading();
    }, duration);
  }

  stopLoading(): void {
    clearInterval(this.loadingInterval);
  }

  markSubscribe(): void {
    localStorage.setItem(this.markSubscribeKey, 'true');
  }
}
