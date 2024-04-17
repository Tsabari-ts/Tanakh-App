import { Component } from '@angular/core';
import { Location } from '@angular/common';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Tanakh';
  showButton = false;
  returnIcon:string = 'return-icon';

  constructor(private location: Location) { }

  goBack(): void {
    console.log("return");
    this.location.back();
  }
}
