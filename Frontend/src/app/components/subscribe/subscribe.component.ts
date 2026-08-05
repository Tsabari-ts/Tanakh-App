import { Component, EventEmitter, Inject, Output, ChangeDetectionStrategy, DestroyRef, inject, signal, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiCallService } from '../../services/api-call.service';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { FormsModule } from '@angular/forms';
import { TermsService } from '../../shared/legal/terms-dialog/terms.service';
import { getStoredUsername } from '../../shared/user-prefs';
import { validateIsraeliMobilePhone, PhoneValidationResult } from '../../shared/israeli-mobile-phone-validator';
import { getStoredManageToken, setStoredManageToken, clearStoredManageToken } from '../../shared/reminder-subscription';

@Component({
    selector: 'app-subscribe',
    templateUrl: './subscribe.component.html',
    styleUrl: './subscribe.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogTitle, MatIcon, CdkScrollable, MatDialogContent, FormsModule]
})

export class SubscribeComponent implements OnInit {
  @Output() subscriptionStatusChange: EventEmitter<{
     newButtonName: string }> = new EventEmitter();
  private readonly terms = inject(TermsService);
  readonly serverResponse = signal('');
  subscribeSuccessful = false;
  // A signal (not a plain property) because it changes after the initial
  // render (cancelSubscription, a failed loadPreferences) and the component
  // uses OnPush - a plain property mutation from inside an async callback
  // would not otherwise trigger a re-render.
  readonly userHasSubscribed = signal(getStoredManageToken() !== null);
  readonly isButtonDisabled = signal(false);
  readonly isRequestInProgress = signal(false);
  readonly isRequestSuccessful = signal(false);
  readonly progressValue = signal(0);
  loadingInterval: any;

  phoneValue: string = '';
  phoneTouched = false;
  readonly phoneValidation = signal<PhoneValidationResult>('empty');
  displayNameValue: string = getStoredUsername();
  timeValue: string = '';
  /** Not shown in the form - the design simplifies the visible fields to
      name/phone/time/consent, but the backend contract still expects this. */
  private readonly skipShabbatHolidays: boolean = true;
  consentGiven: boolean = false;
  timeOptions: string[] = this.generateTimeOptions();

  // Manage-subscription panel - shown instead of the signup form once a
  // manage token is stored locally, i.e. this browser has already subscribed.
  readonly managePreferencesLoaded = signal(false);
  readonly manageLoadFailed = signal(false);
  readonly manageBusy = signal(false);
  readonly manageStatusMessage = signal('');
  managePreferredTime: string = '';
  managePausedUntil: string | null = null;

  constructor(@Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<SubscribeComponent>,
    private apiService: ApiCallService,
    private destroyRef: DestroyRef) {
    }

  ngOnInit(): void {
    if (this.userHasSubscribed()) {
      this.loadPreferences();
    }
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

  openTerms(): void {
    this.terms.open();
  }

  onPhoneInput(): void {
    this.phoneValidation.set(validateIsraeliMobilePhone(this.phoneValue).result);
  }

  submitForm(form: any) {
    this.phoneTouched = true;
    const phoneCheck = validateIsraeliMobilePhone(this.phoneValue);
    this.phoneValidation.set(phoneCheck.result);

    if (form.valid && this.consentGiven && phoneCheck.result === 'valid') {
      this.closeAndSubscribe(phoneCheck.e164);
    } else {
      form.submitted = true;
    }
  }

  closeAndSubscribe(phoneE164: string) {
    this.isButtonDisabled.set(true);
    this.isRequestInProgress.set(true);
    this.startLoading();

    const subscriptionRequest = {
      phoneNumber: phoneE164,
      displayName: this.displayNameValue || null,
      preferredTime: this.timeValue,
      skipShabbatHolidays: this.skipShabbatHolidays,
      consent: this.consentGiven
    };

    this.apiService.subscribe(subscriptionRequest)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.subscribeSuccessful = true;
          setStoredManageToken(response.manageToken);
        },
        error: () => {
          this.subscribeSuccessful = false;
        },
        complete: () => {
          setTimeout(() => {
            this.stopLoading();
            this.setSubscribeServerResponse();
          }, 3000);
        }
      });
  }

  setSubscribeServerResponse() {
    setTimeout(() => {
      if (this.subscribeSuccessful) {
        this.serverResponse.set($localize`:@@subscribe.confirmationSent:נרשמת בהצלחה! תקבל/י תזכורת ב-SMS בשעה שבחרת.`);
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

  isPaused(): boolean {
    return !!this.managePausedUntil && new Date(this.managePausedUntil).getTime() > Date.now();
  }

  loadPreferences(): void {
    const token = getStoredManageToken();
    if (!token) {
      return;
    }

    this.apiService.getReminderPreferences(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preferences) => {
          this.managePreferredTime = (preferences.preferredTime ?? '').slice(0, 5);
          this.managePausedUntil = preferences.pausedUntil ?? null;
          this.managePreferencesLoaded.set(true);
        },
        error: () => {
          // Manage token no longer resolves to an active subscriber (e.g.
          // already unsubscribed elsewhere) - fall back to the signup form
          // rather than showing a stuck panel.
          clearStoredManageToken();
          this.userHasSubscribed.set(false);
          this.manageLoadFailed.set(true);
        }
      });
  }

  saveManagePreferences(): void {
    const token = getStoredManageToken();
    if (!token) {
      return;
    }

    this.manageBusy.set(true);
    this.apiService.updateReminderPreferences(token, this.managePreferredTime)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.manageBusy.set(false);
          this.manageStatusMessage.set($localize`:@@subscribe.manage.saved:העדכון בוצע בהצלחה.`);
        },
        error: () => {
          this.manageBusy.set(false);
          this.manageStatusMessage.set($localize`:@@subscribe.manage.saveFailed:העדכון נכשל, נסו שוב.`);
        }
      });
  }

  togglePause(): void {
    const token = getStoredManageToken();
    if (!token) {
      return;
    }

    this.manageBusy.set(true);
    this.apiService.updateReminderPreferences(token, undefined, this.isPaused() ? 'resume' : 'pause')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.manageBusy.set(false);
          this.loadPreferences();
        },
        error: () => {
          this.manageBusy.set(false);
          this.manageStatusMessage.set($localize`:@@subscribe.manage.saveFailed:העדכון נכשל, נסו שוב.`);
        }
      });
  }

  cancelSubscription(): void {
    const token = getStoredManageToken();
    if (!token) {
      return;
    }

    this.manageBusy.set(true);
    this.apiService.unsubscribeReminder(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          clearStoredManageToken();
          this.userHasSubscribed.set(false);
          this.subscriptionStatusChange.emit({
            newButtonName: $localize`:@@settings.subscribeButton:הירשם לתזכורת יומית` });
          this.dialogRef.close();
        },
        error: () => {
          this.manageBusy.set(false);
          this.manageStatusMessage.set($localize`:@@subscribe.manage.saveFailed:העדכון נכשל, נסו שוב.`);
        }
      });
  }
}
