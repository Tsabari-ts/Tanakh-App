import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ReadPermissionComponent } from './read-permission.component';

describe('ReadPermissionComponent', () => {
  let component: ReadPermissionComponent;
  let fixture: ComponentFixture<ReadPermissionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [ReadPermissionComponent],
    providers: [provideZonelessChangeDetection()]
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
