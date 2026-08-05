import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../admin-auth.service';

type LoginStep = 'credentials' | 'otp';

@Component({
  selector: 'app-admin-login',
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule]
})
export class AdminLoginComponent {
  private readonly authService = inject(AdminAuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly step = signal<LoginStep>('credentials');
  readonly busy = signal(false);
  readonly errorMessage = signal('');

  username = '';
  password = '';
  code = '';

  submitCredentials(): void {
    if (!this.username || !this.password || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.errorMessage.set('');

    this.authService.login(this.username, this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.step.set('otp');
        },
        error: () => {
          this.busy.set(false);
          this.errorMessage.set('שם משתמש או סיסמה שגויים');
        }
      });
  }

  submitOtp(): void {
    if (!this.code || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.errorMessage.set('');

    this.authService.verifyOtp(this.code)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.router.navigate(['/', environment.adminRoutePath]);
        },
        error: () => {
          this.busy.set(false);
          this.code = '';
          this.errorMessage.set('קוד האימות שגוי או שפג תוקפו');
        }
      });
  }
}
