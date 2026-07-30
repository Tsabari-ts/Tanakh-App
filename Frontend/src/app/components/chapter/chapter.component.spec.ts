import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ChapterComponent } from './chapter.component';

describe('ChapterComponent', () => {
  let component: ChapterComponent;
  let fixture: ComponentFixture<ChapterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [ChapterComponent],
    providers: [provideZonelessChangeDetection()]
})
    .compileComponents();
    
    fixture = TestBed.createComponent(ChapterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
