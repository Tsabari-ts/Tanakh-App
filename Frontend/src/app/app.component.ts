import { Component } from '@angular/core';
import { Location } from '@angular/common';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.css',
    standalone: false
})
export class AppComponent {
  title = 'Tanakh';
  showButton = false;
  returnIcon:string = 'return-icon';

  constructor(private location: Location) { }

  goBack(): void {
    this.location.back();
  }
}
