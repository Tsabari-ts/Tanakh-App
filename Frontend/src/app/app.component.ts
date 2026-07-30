import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { Location, NgClass } from '@angular/common';
import { Dir } from '@angular/cdk/bidi';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './services/notification.service';
import { ErrorStateService } from './services/error-state.service';
import { ErrorScreenComponent } from './shared/error-screen/error-screen.component';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Dir, NgClass, RouterOutlet, ErrorScreenComponent]
})
export class AppComponent {
  title = 'Tanakh';
  readonly showButton = signal(false);
  returnIcon:string = 'return-icon';
  readonly notifications = inject(NotificationService);
  readonly errorState = inject(ErrorStateService);

  constructor(private location: Location) { }

  goBack(): void {
    this.location.back();
  }
}
