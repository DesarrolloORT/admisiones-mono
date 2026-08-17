import { ComponentFixture, TestBed } from '@angular/core/testing';

import type { SeminarSummaryItem } from '../../../../../models/enrollment-flow';
import { EnrollmentSeminarsSummary } from './enrollment-seminars-summary';

describe('EnrollmentSeminarsSummary', () => {
  let fixture: ComponentFixture<EnrollmentSeminarsSummary>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [EnrollmentSeminarsSummary] });
    fixture = TestBed.createComponent(EnrollmentSeminarsSummary);
  });

  function setSeminars(seminars: readonly SeminarSummaryItem[]): void {
    fixture.componentRef.setInput('seminars', seminars);
    fixture.detectChanges();
  }

  it('renders nothing when there are no seminarios', () => {
    setSeminars([]);

    expect(fixture.nativeElement.querySelector('#seminars-title')).toBeNull();
    expect(fixture.nativeElement.querySelector('dl')).toBeNull();
    expect(fixture.nativeElement.textContent.trim()).toBe('');
  });

  it('renders a single Seminarios term with one value per seminario', () => {
    setSeminars([
      { enrollmentId: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' },
      { enrollmentId: 2, name: 'Seminario B', intake: 'Abril', shift: 'Mañana' },
    ]);

    const terms = fixture.nativeElement.querySelectorAll('dt');
    const values = fixture.nativeElement.querySelectorAll('dd');

    expect(terms).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('#seminars-title')?.textContent).toBe('Seminarios');
    expect(terms[0].querySelectorAll('ort-icon')).toHaveLength(1);
    expect(values).toHaveLength(2);
    expect(values[0].textContent).toBe('Marzo · Noche');
    expect(values[1].textContent).toBe('Abril · Mañana');
  });
});
