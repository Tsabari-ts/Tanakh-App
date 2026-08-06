import { Component, ChangeDetectionStrategy, DestroyRef, computed, inject, signal, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiCallService } from '../../services/api-call.service';
import { FormsModule, NgForm } from '@angular/forms';
import { LegalDialogService } from '../../shared/legal/legal-dialog.service';
import { LEGAL_DOCS_META } from '../../shared/legal/legal-content';
import { getStoredUsername } from '../../shared/user-prefs';
import { validateIsraeliMobilePhone, PhoneValidationResult } from '../../shared/israeli-mobile-phone-validator';
import { getStoredManageToken, setStoredManageToken, clearStoredManageToken } from '../../shared/reminder-subscription';

/**
 * Embedded directly on the settings page (not a MatDialog) - the reminder
 * signup/manage panel is always visible there rather than behind a popup.
 */
@Component({
    selector: 'app-subscribe',
    templateUrl: './subscribe.component.html',
    styleUrl: './subscribe.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule]
})

export class SubscribeComponent implements OnInit {
  private readonly legal = inject(LegalDialogService);
  readonly serverResponse = signal('');
  subscribeSuccessful = false;
  // A signal (not a plain property) because it changes after the initial
  // render (cancelSubscription, a failed loadPreferences) and the component
  // uses OnPush - a plain property mutation from inside an async callback
  // would not otherwise trigger a re-render.
  readonly userHasSubscribed = signal(getStoredManageToken() !== null);
  // Drives the "הזכר לי לקרוא" switch above the form/manage panel - turning
  // it on (while not yet subscribed) reveals the signup form; turning it
  // off while already subscribed goes through showCancelConfirm instead of
  // cancelling immediately, since that's a destructive action.
  readonly formExpanded = signal(false);
  readonly detailsVisible = computed(() => this.userHasSubscribed() || this.formExpanded());
  readonly showCancelConfirm = signal(false);
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

  // Exact wording shown next to the consent checkbox - sent verbatim as
  // ConsentText so ConsentRecord captures precisely what the subscriber
  // agreed to (see subscribe.component.html consent-field block).
  private readonly consentText =
    'אני מסכים/ה לקבל הודעות תזכורת ב-SMS ומאשר/ת שקראתי את תנאי השימוש ואת מדיניות הפרטיות.';

  // OTP step - shown after the form's own client-side validation passes,
  // in place of subscribing immediately, so a phone can't be registered
  // without its owner receiving and entering the code.
  readonly otpStep = signal(false);
  readonly otpSending = signal(false);
  readonly otpVerifying = signal(false);
  readonly otpError = signal('');
  otpValue: string = '';
  private pendingPhoneE164: string = '';

  // Manage-subscription panel - shown instead of the signup form once a
  // manage token is stored locally, i.e. this browser has already subscribed.
  readonly managePreferencesLoaded = signal(false);
  readonly manageLoadFailed = signal(false);
  readonly manageBusy = signal(false);
  readonly manageStatusMessage = signal('');
  managePreferredTime: string = '';
  managePausedUntil: string | null = null;

  constructor(private apiService: ApiCallService,
    private destroyRef: DestroyRef) {
    }

  ngOnInit(): void {
    if (this.userHasSubscribed()) {
      this.loadPreferences();
    }
  }

  // Zero-padded ("08:00", not "8:00") to match the backend's TimeOnly
  // serialization exactly - loadPreferences() compares managePreferredTime
  // against these values verbatim, and "8:00" never matched "08:00:00".
  generateTimeOptions(): string[] {
    const options: string[] = [];
    for (let hour = 8; hour <= 20; hour++) {
      options.push(`${hour.toString().padStart(2, '0')}:00`);
    }
    return options;
  }

  openTerms(): void {
    this.legal.open('terms');
  }

  openPrivacy(): void {
    this.legal.open('privacy');
  }

  // The switch's "on" state is fully derived (detailsVisible), not a native
  // form control, so turning it off while subscribed can just leave it be
  // and show the confirm prompt instead - no value to revert.
  onToggleClick(): void {
    if (!this.detailsVisible()) {
      this.formExpanded.set(true);
      return;
    }

    if (this.userHasSubscribed()) {
      this.showCancelConfirm.set(true);
    } else {
      this.formExpanded.set(false);
    }
  }

  requestCancel(): void {
    this.showCancelConfirm.set(true);
  }

  keepSubscription(): void {
    this.showCancelConfirm.set(false);
  }

  confirmCancel(): void {
    this.showCancelConfirm.set(false);
    this.cancelSubscription();
  }

  onPhoneInput(): void {
    this.phoneValidation.set(validateIsraeliMobilePhone(this.phoneValue).result);
  }

  submitForm(form: NgForm) {
    this.phoneTouched = true;
    const phoneCheck = validateIsraeliMobilePhone(this.phoneValue);
    this.phoneValidation.set(phoneCheck.result);

    // form.submitted is a read-only getter (already true here, since this
    // method only runs as the form's own (ngSubmit) handler) - assigning to
    // it used to be a harmless no-op on older Angular, but throws on this
    // version and was never needed for the inline `myForm.submitted && ...`
    // error messages to appear.
    if (form.valid && this.consentGiven && phoneCheck.result === 'valid') {
      this.requestOtp(phoneCheck.e164);
    }
  }

  requestOtp(phoneE164: string): void {
    this.otpError.set('');
    this.otpSending.set(true);
    this.pendingPhoneE164 = phoneE164;

    this.apiService.requestOtp(phoneE164)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.otpSending.set(false);
          this.otpStep.set(true);
        },
        error: () => {
          this.otpSending.set(false);
          this.otpError.set($localize`:@@subscribe.otp.sendFailed:שליחת קוד האימות נכשלה, נסו שוב.`);
        },
      });
  }

  resendOtp(): void {
    if (this.pendingPhoneE164) {
      this.requestOtp(this.pendingPhoneE164);
    }
  }

  verifyOtpAndSubscribe(): void {
    if (!this.otpValue) {
      this.otpError.set($localize`:@@subscribe.otp.required:יש להזין את קוד האימות שנשלח אליך.`);
      return;
    }
    this.closeAndSubscribe(this.pendingPhoneE164, this.otpValue);
  }

  closeAndSubscribe(phoneE164: string, otpCode: string) {
    this.otpVerifying.set(true);
    this.isButtonDisabled.set(true);
    this.isRequestInProgress.set(true);
    this.startLoading();

    const subscriptionRequest = {
      phoneNumber: phoneE164,
      displayName: this.displayNameValue || null,
      preferredTime: this.timeValue,
      skipShabbatHolidays: this.skipShabbatHolidays,
      consent: this.consentGiven,
      otpCode,
      termsVersion: LEGAL_DOCS_META.terms.lastUpdated,
      privacyVersion: LEGAL_DOCS_META.privacy.lastUpdated,
      consentText: this.consentText,
    };

    this.apiService.subscribe(subscriptionRequest)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.subscribeSuccessful = true;
          setStoredManageToken(response.manageToken);
        },
        // error and complete are mutually exclusive in RxJS - a failed
        // request used to only set this flag and stop there, since the
        // finishing steps were in `complete`, which never runs after
        // `error`. That left isRequestInProgress stuck true forever with
        // no inline feedback at all, so the only thing the user ever saw
        // was the generic global error toast (error.interceptor.ts).
        error: (err: HttpErrorResponse) => {
          this.otpVerifying.set(false);
          const title = err.error?.title;
          if (title === 'otp_invalid' || title === 'otp_locked') {
            // Wrong/expired code - let the user retry entering it instead
            // of dropping back through the generic "signup failed" flow,
            // which would otherwise discard the otp step and force
            // restarting from the phone/time/consent form.
            this.isButtonDisabled.set(false);
            this.isRequestInProgress.set(false);
            this.stopLoading();
            this.otpError.set(title === 'otp_locked'
              ? $localize`:@@subscribe.otp.locked:יותר מדי ניסיונות שגויים. יש לבקש קוד חדש.`
              : $localize`:@@subscribe.otp.invalid:קוד האימות שגוי. נסו שוב.`);
            return;
          }
          this.subscribeSuccessful = false;
          this.finishSubscribeAttempt();
        },
        complete: () => {
          this.otpVerifying.set(false);
          this.finishSubscribeAttempt();
        },
      });
  }

  private finishSubscribeAttempt(): void {
    setTimeout(() => {
      this.stopLoading();
      this.setSubscribeServerResponse();
    }, 3000);
  }

  setSubscribeServerResponse() {
    setTimeout(() => {
      if (this.subscribeSuccessful) {
        this.serverResponse.set($localize`:@@subscribe.confirmationSent:נרשמת בהצלחה! תקבל/י תזכורת ב-SMS בשעה שבחרת.`);
      } else {
        this.serverResponse.set($localize`:@@subscribe.failed:הרישום נכשל, אנא נסה שוב מאוחר יותר`);
      }
      this.isRequestInProgress.set(false);
      this.isRequestSuccessful.set(true);

      if (this.subscribeSuccessful) {
        // Swap from the signup form to the manage-subscription panel once
        // the success message has had a moment to be read - mirrors the
        // 3s delay this used to wait before auto-closing as a dialog.
        setTimeout(() => {
          this.userHasSubscribed.set(true);
          this.loadPreferences();
        }, 3000);
      }
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
          this.resetToFreshState();
        },
        error: () => {
          this.manageBusy.set(false);
          this.manageStatusMessage.set($localize`:@@subscribe.manage.saveFailed:העדכון נכשל, נסו שוב.`);
        }
      });
  }

  // Cancelling used to leave every leftover signal/field from the previous
  // signup in place: formExpanded stayed true (set when the switch was
  // first turned on, never touched again), so the form reappeared instead
  // of collapsing back to the switch-off state; the stale name/phone/time/
  // consent values were still filled in; and the old "נרשמת בהצלחה" success
  // banner was still showing since isRequestSuccessful/serverResponse are
  // never cleared - together making it look like the cancellation hadn't
  // actually happened. Puts everything back to how it looked before the
  // user ever touched the switch.
  private resetToFreshState(): void {
    this.formExpanded.set(false);
    this.manageBusy.set(false);

    this.displayNameValue = getStoredUsername();
    this.phoneValue = '';
    this.phoneTouched = false;
    this.phoneValidation.set('empty');
    this.timeValue = '';
    this.consentGiven = false;

    this.otpStep.set(false);
    this.otpSending.set(false);
    this.otpVerifying.set(false);
    this.otpError.set('');
    this.otpValue = '';
    this.pendingPhoneE164 = '';

    this.isButtonDisabled.set(false);
    this.isRequestInProgress.set(false);
    this.isRequestSuccessful.set(false);
    this.serverResponse.set('');
    this.subscribeSuccessful = false;

    this.managePreferencesLoaded.set(false);
    this.manageLoadFailed.set(false);
    this.manageStatusMessage.set('');
    this.managePreferredTime = '';
    this.managePausedUntil = null;
  }
}
