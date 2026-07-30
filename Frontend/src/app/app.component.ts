import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { Location, NgClass } from '@angular/common';
import { Dir } from '@angular/cdk/bidi';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './services/notification.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Dir, NgClass, RouterOutlet]
})
export class AppComponent {
  title = 'Tanakh';
  readonly showButton = signal(false);
  returnIcon:string = 'return-icon';
  readonly notifications = inject(NotificationService);

  constructor(private location: Location) { }

  goBack(): void {
    this.location.back();
  }
}
