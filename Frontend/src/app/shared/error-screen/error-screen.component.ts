import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { ErrorStateService } from '../../services/error-state.service';

@Component({
  selector: 'app-error-screen',
  templateUrl: './error-screen.component.html',
  styleUrl: './error-screen.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorScreenComponent {
  readonly errorState = inject(ErrorStateService);

  reload(): void {
    document.location.reload();
  }

  backToHome(): void {
    this.errorState.clear();
    document.location.href = '/home';
  }
}
