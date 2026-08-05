import { Component, ChangeDetectionStrategy, Input } from '@angular/core';

@Component({
  selector: 'app-maintenance-screen',
  templateUrl: './maintenance-screen.component.html',
  styleUrl: './maintenance-screen.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaintenanceScreenComponent {
  @Input() message: string | null = null;

  readonly defaultMessage = $localize`:@@maintenanceScreen.defaultMessage:האתר בתחזוקה זמנית. נא לנסות שוב בעוד כמה דקות.`;

  reload(): void {
    document.location.reload();
  }
}
