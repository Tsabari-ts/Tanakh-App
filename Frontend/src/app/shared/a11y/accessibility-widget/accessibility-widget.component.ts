import { Component, ChangeDetectionStrategy, inject, signal, viewChild, ElementRef } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { A11yService } from '../../../core/a11y/a11y.service';
import { FontScale, ContrastMode } from '../../../core/a11y/a11y.model';
import { AccessibilityStatementService } from '../accessibility-statement-dialog/accessibility-statement.service';

interface ScaleOption {
  value: FontScale;
  label: string;
}

@Component({
  selector: 'app-accessibility-widget',
  standalone: true,
  imports: [A11yModule],
  templateUrl: './accessibility-widget.component.html',
  styleUrl: './accessibility-widget.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessibilityWidgetComponent {
  private a11y = inject(A11yService);
  private statement = inject(AccessibilityStatementService);

  readonly s = this.a11y.settings;
  readonly open = signal(false);
  readonly fab = viewChild.required<ElementRef<HTMLButtonElement>>('fab');

  readonly scales: ScaleOption[] = [
    { value: 1, label: $localize`:@@a11y.scale.normal:רגיל` },
    { value: 1.15, label: $localize`:@@a11y.scale.large:גדול` },
    { value: 1.3, label: $localize`:@@a11y.scale.larger:גדול מאוד` },
    { value: 1.5, label: $localize`:@@a11y.scale.huge:ענק` },
  ];

  toggle(): void {
    this.open.update(v => !v);
  }

  close(returnFocus = true): void {
    this.open.set(false);
    if (returnFocus) {
      this.fab().nativeElement.focus();
    }
  }

  setScale(v: FontScale): void {
    this.a11y.setFontScale(v);
  }

  setContrast(v: ContrastMode): void {
    this.a11y.setContrast(v);
  }

  flip(key: 'grayscale' | 'highlightLinks' | 'readableFont' | 'textSpacing' | 'noMotion' | 'bigCursor' | 'readingRuler'): void {
    this.a11y.toggle(key);
  }

  reset(): void {
    this.a11y.reset();
  }

  openStatement(): void {
    this.close(false);
    this.statement.open();
  }

  onRadioKeydown(e: KeyboardEvent, list: HTMLElement): void {
    const keys = ['ArrowRight', 'ArrowLeft', 'ArrowUp', 'ArrowDown', 'Home', 'End'];
    if (!keys.includes(e.key)) {
      return;
    }
    e.preventDefault();
    const items = Array.from(list.querySelectorAll<HTMLElement>('[role="radio"]'));
    const i = items.indexOf(document.activeElement as HTMLElement);
    // RTL layout: ArrowLeft moves to the next option, ArrowRight to the previous.
    const delta = (e.key === 'ArrowLeft' || e.key === 'ArrowDown') ? 1
      : (e.key === 'ArrowRight' || e.key === 'ArrowUp') ? -1 : 0;
    const next = e.key === 'Home' ? 0
      : e.key === 'End' ? items.length - 1
      : (i + delta + items.length) % items.length;
    items[next]?.focus();
    items[next]?.click();
  }
}
