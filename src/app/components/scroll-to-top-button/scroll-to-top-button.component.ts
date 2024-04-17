import { Component, Input, Renderer2 } from '@angular/core';

@Component({
  selector: 'app-scroll-to-top-button',
  template: `
  <button class="scroll-to-top-button" (click)="scrollToTop()">
  <mat-icon>keyboard_arrow_up</mat-icon>
  <div>קפוץ</div>
  <div class="up-text">למעלה</div>
  </button>
`,
  styles: [`
  .scroll-to-top-button {
    position: fixed;
    top: 300px;
    right: 7%;
    width: 45px;
    contain: content;
    display: none; 
    height: 100px;
    background-color: #333333e6; 
    color: #aaa;
    border: none;
    border-radius: 20px;
    cursor: pointer;
  }

  .scroll-to-top-button:hover {
  color: #fdfdfd; 
}


  .up-text{
    margin-right: -5px;
  }

  @media screen and (max-width: 1024px) {
      .scroll-to-top-button{
        top: 300px;
        right: 1%;
        width: 40px;
        color: #fdfdfd; 
      }
    }
`]
})
export class ScrollToTopButtonComponent {
  constructor(private renderer: Renderer2) {}
  @Input() contentContainer!: HTMLElement;
  
 
  ngAfterViewInit() {
    this.contentContainer.addEventListener('scroll', () => {
      this.toggleButtonVisibility();
    });
  }

  scrollToTop() {
    this.contentContainer.scrollTo({ top: 0, behavior: 'smooth' });
  }

  toggleButtonVisibility() {
    const button = document.querySelector('.scroll-to-top-button');
    if (button) {
      if (this.contentContainer.scrollTop > 300) {
        this.renderer.setStyle(button, 'display', 'block');
      } else {
        this.renderer.setStyle(button, 'display', 'none');
      }
    }
  }
}
