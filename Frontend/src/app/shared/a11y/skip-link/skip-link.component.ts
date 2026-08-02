import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-skip-link',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a class="skip-link" href="#main-content" (click)="focusMain($event)" i18n="@@skipLink.label">
      דילוג לתוכן הראשי
    </a>
  `,
  styles: [`
    .skip-link {
      position: absolute;
      inset-block-start: -100%;
      inset-inline-start: 1rem;
      z-index: 10000;
      padding: 0.75rem 1.25rem;
      background: var(--color-bg);
      color: var(--color-text);
      border: 2px solid var(--color-focus);
      border-radius: 0 0 6px 6px;
      font-size: var(--font-size-base);
      text-decoration: underline;
    }
    .skip-link:focus-visible {
      inset-block-start: 0;
    }
  `],
})
export class SkipLinkComponent {
  focusMain(e: Event): void {
    e.preventDefault();
    const main = document.getElementById('main-content');
    main?.focus();
    main?.scrollIntoView({ block: 'start' });
  }
}
