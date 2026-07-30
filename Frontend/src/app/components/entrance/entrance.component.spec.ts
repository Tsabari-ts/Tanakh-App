import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { EntranceComponent } from './entrance.component';

describe('EntranceComponent', () => {
  let component: EntranceComponent;
  let fixture: ComponentFixture<EntranceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [EntranceComponent],
    providers: [provideZonelessChangeDetection()]
})
    .compileComponents();
    
    fixture = TestBed.createComponent(EntranceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
