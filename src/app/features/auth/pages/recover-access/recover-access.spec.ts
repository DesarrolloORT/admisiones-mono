/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RecoverAccess } from 'recover-access';

describe('RecoverAccess', () => {
  let component: RecoverAccess;
  let fixture: ComponentFixture<RecoverAccess>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecoverAccess],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RecoverAccess);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});
