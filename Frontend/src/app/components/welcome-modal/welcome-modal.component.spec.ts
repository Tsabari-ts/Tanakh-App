import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { WelcomeModalComponent } from './welcome-modal.component';

describe('WelcomeModalComponent', () => {
  let component: WelcomeModalComponent;
  let fixture: ComponentFixture<WelcomeModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [WelcomeModalComponent],
    providers: [provideZonelessChangeDetection()]
})
    .compileComponents();
    
    fixture = TestBed.createComponent(WelcomeModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
