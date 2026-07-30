import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ChapterlistComponent } from './chapterlist.component';

describe('ChapterlistComponent', () => {
  let component: ChapterlistComponent;
  let fixture: ComponentFixture<ChapterlistComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [ChapterlistComponent],
    providers: [provideZonelessChangeDetection()]
})
    .compileComponents();
    
    fixture = TestBed.createComponent(ChapterlistComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
