import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ScrollToTopButtonComponent } from './scroll-to-top-button.component';

describe('ScrollToTopButtonComponent', () => {
  let component: ScrollToTopButtonComponent;
  let fixture: ComponentFixture<ScrollToTopButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [ScrollToTopButtonComponent],
    providers: [provideZonelessChangeDetection()]
})
    .compileComponents();
    
    fixture = TestBed.createComponent(ScrollToTopButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
