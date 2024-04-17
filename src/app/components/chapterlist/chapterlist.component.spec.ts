import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChapterlistComponent } from './chapterlist.component';

describe('ChapterlistComponent', () => {
  let component: ChapterlistComponent;
  let fixture: ComponentFixture<ChapterlistComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ChapterlistComponent]
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
