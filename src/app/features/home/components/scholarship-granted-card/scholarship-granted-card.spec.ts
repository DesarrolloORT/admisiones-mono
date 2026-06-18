/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScholarshipGrantedCard } from 'scholarship-granted-card';

describe('ScholarshipGrantedCard', () => {
  let component: ScholarshipGrantedCard;
  let fixture: ComponentFixture<ScholarshipGrantedCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScholarshipGrantedCard],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ScholarshipGrantedCard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});
