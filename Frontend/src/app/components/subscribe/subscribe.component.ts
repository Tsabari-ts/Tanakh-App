import { Component, EventEmitter, Inject, Output, ChangeDetectionStrategy, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiCallService } from '../../services/api-call.service';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';

@Component({
    selector: 'app-subscribe',
    templateUrl: './subscribe.component.html',
    styleUrl: './subscribe.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogTitle, MatIcon, CdkScrollable, MatDialogContent, FormsModule, NgClass]
})

export class SubscribeComponent {
  @Output() subscriptionStatusChange: EventEmitter<{
     newButtonName: string }> = new EventEmitter();
  private markSubscribeKey = 'userHasSubscribed';
  readonly serverResponse = signal('');
  subscribeSuccessful = false;
  userHasSubscribed = false;
  readonly isButtonDisabled = signal(false);
  readonly isRequestInProgress = signal(false);
  readonly isRequestSuccessful = signal(false);
  readonly progressValue = signal(0);
  loadingInterval: any;

  emailValue: string = '';
  displayNameValue: string = '';
  timeValue: string = '';
  skipShabbatHolidays: boolean = true;
  consentGiven: boolean = false;
  timeOptions: string[] = this.generateTimeOptions();

  constructor(@Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<SubscribeComponent>,
    private apiService: ApiCallService,
    private destroyRef: DestroyRef) {
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
    this.isButtonDisabled.set(true);
    this.isRequestInProgress.set(true);
    this.startLoading();

    const subscriptionRequest = {
      email: this.emailValue,
      displayName: this.displayNameValue || null,
      preferredTime: this.timeValue,
      skipShabbatHolidays: this.skipShabbatHolidays,
      consent: this.consentGiven
    };

    this.apiService.subscribe(subscriptionRequest)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
      this.subscribeSuccessful = true;
      this.markSubscribe();
    }, () => {
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
        this.serverResponse.set($localize`:@@subscribe.confirmationSent:שלחנו לך מייל אישור - יש ללחוץ על הקישור בו כדי להשלים את ההרשמה.`);
        this.subscriptionStatusChange.emit({
          newButtonName: $localize`:@@subscribe.subscribedButton:נרשמת לתזכורת` });
      } else {
        this.serverResponse.set($localize`:@@subscribe.failed:הרישום נכשל, אנא נסה שוב מאוחר יותר`);
      }
      this.isRequestInProgress.set(false);
      this.isRequestSuccessful.set(true);

      setTimeout(() => {
        this.dialogRef.close();
      }, 3000);
    }, 3000);
  }

  startLoading(): void {
    this.progressValue.set(0);
    const duration = 3000;
    const interval = 10;
    const steps = (duration / interval);
    const stepSize = 100 / steps;

    this.loadingInterval = setInterval(() => {
      if (this.progressValue() < 100) {
        this.progressValue.update(v => v + stepSize);
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
