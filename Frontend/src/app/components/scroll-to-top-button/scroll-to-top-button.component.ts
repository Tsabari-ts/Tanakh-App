import { Component, Input, Renderer2, ChangeDetectionStrategy } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
    selector: 'app-scroll-to-top-button',
    template: `
  <button class="scroll-to-top-button" type="button" (click)="scrollToTop()">
  <mat-icon aria-hidden="true">keyboard_arrow_up</mat-icon>
  <div i18n="@@scrollToTop.jump">קפוץ</div>
  <div class="up-text" i18n="@@scrollToTop.up">למעלה</div>
  </button>
`,
    styleUrl: './scroll-to-top-button.component.scss',
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
