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

  it('renders the heading and one row per seminario with comienzo and turno', () => {
    setSeminars([
      { enrollmentId: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' },
      { enrollmentId: 2, name: 'Seminario B', intake: 'Abril', shift: 'Mañana' },
    ]);

    const title = fixture.nativeElement.querySelector('#seminars-title');
    const rows = fixture.nativeElement.querySelectorAll('.enrollment-summary-list__item');

    expect(title?.textContent).toBe('Seminarios');
    expect(fixture.nativeElement.querySelector('dl')?.getAttribute('aria-labelledby')).toBe(
      'seminars-title'
    );
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelector('dt > span')?.textContent).toBe('Seminario A');
    expect(rows[0].querySelector('dd')?.textContent).toBe('Marzo · Noche');
    expect(rows[1].querySelector('dt > span')?.textContent).toBe('Seminario B');
    expect(rows[1].querySelector('dd')?.textContent).toBe('Abril · Mañana');
  });
});
