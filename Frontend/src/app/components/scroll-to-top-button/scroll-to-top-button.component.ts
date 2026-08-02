import { Component, Input, Renderer2, ChangeDetectionStrategy } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
    selector: 'app-scroll-to-top-button',
    template: `
  <button class="scroll-to-top-button" (click)="scrollToTop()">
  <mat-icon>keyboard_arrow_up</mat-icon>
  <div i18n="@@scrollToTop.jump">קפוץ</div>
  <div class="up-text" i18n="@@scrollToTop.up">למעלה</div>
  </button>
`,
    styles: [`
  .scroll-to-top-button {
    position: fixed;
    top: 300px;
    inset-inline-start: 7%;
    width: var(--tap-target-min);
    contain: content;
    display: none;
    height: 100px;
    background-color: var(--color-panel-bg);
    color: var(--color-panel-text);
    border: none;
    border-radius: 20px;
    cursor: pointer;
    z-index: var(--z-scroll-top);
    font-size: var(--font-size-base);
  }

  .scroll-to-top-button:hover {
  color: var(--color-panel-text-hover);
}


  .up-text{
    margin-inline-start: -5px;
  }

  @media screen and (max-width: 1024px) {
      .scroll-to-top-button{
        top: 300px;
        inset-inline-start: 1%;
        width: 40px;
        color: var(--color-panel-text-hover);
      }
    }
`],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatIcon]
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
