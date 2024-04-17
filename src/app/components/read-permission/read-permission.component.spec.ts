import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReadPermissionComponent } from './read-permission.component';

describe('ReadPermissionComponent', () => {
  let component: ReadPermissionComponent;
  let fixture: ComponentFixture<ReadPermissionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReadPermissionComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ReadPermissionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
