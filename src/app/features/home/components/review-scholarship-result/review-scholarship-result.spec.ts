import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { ReviewScholarshipResult } from './review-scholarship-result';

describe('ReviewScholarshipResult', () => {
  let fixture: ComponentFixture<ReviewScholarshipResult>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ReviewScholarshipResult] });
    fixture = TestBed.createComponent(ReviewScholarshipResult);
    fixture.detectChanges();
  });

  it('renders the result summary the person is asking us to review', () => {
    const labels = Array.from(fixture.nativeElement.querySelectorAll('.result-summary__label')).map(
      element => (element as Element).textContent?.trim()
    );

    expect(labels).toEqual(['Beca', 'Resultado de la prueba', 'Beneficio']);
  });

  it('emits closeReview when the person cancels', () => {
    const closeReview = vi.fn();
    fixture.componentInstance.closeReview.subscribe(closeReview);

    const cancel = Array.from(fixture.nativeElement.querySelectorAll('button')).find(button =>
      (button as Element).textContent?.includes('Cancelar')
    ) as HTMLButtonElement;
    cancel.click();

    expect(closeReview).toHaveBeenCalledOnce();
  });
});
