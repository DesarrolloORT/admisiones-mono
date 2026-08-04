import { ComponentFixture, TestBed } from '@angular/core/testing';

import type { ItemSeminarioResumen } from '../../../../../models/inscription-flow';
import { InscripcionSeminariosSummary } from './inscription-seminarios-summary';

describe('InscripcionSeminariosSummary', () => {
  let fixture: ComponentFixture<InscripcionSeminariosSummary>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [InscripcionSeminariosSummary] });
    fixture = TestBed.createComponent(InscripcionSeminariosSummary);
  });

  function setSeminarios(seminarios: readonly ItemSeminarioResumen[]): void {
    fixture.componentRef.setInput('seminarios', seminarios);
    fixture.detectChanges();
  }

  it('renders nothing when there are no seminarios', () => {
    setSeminarios([]);

    expect(fixture.nativeElement.querySelector('#seminarios-title')).toBeNull();
    expect(fixture.nativeElement.querySelector('dl')).toBeNull();
    expect(fixture.nativeElement.textContent.trim()).toBe('');
  });

  it('renders the heading and one row per seminario with comienzo and turno', () => {
    setSeminarios([
      { idInscripcion: 1, nombre: 'Seminario A', comienzo: 'Marzo', turno: 'Noche' },
      { idInscripcion: 2, nombre: 'Seminario B', comienzo: 'Abril', turno: 'Mañana' },
    ]);

    const title = fixture.nativeElement.querySelector('#seminarios-title');
    const rows = fixture.nativeElement.querySelectorAll('.inscription-summary-list__item');

    expect(title?.textContent).toBe('Seminarios');
    expect(fixture.nativeElement.querySelector('dl')?.getAttribute('aria-labelledby')).toBe(
      'seminarios-title'
    );
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelector('dt > span')?.textContent).toBe('Seminario A');
    expect(rows[0].querySelector('dd')?.textContent).toBe('Marzo · Noche');
    expect(rows[1].querySelector('dt > span')?.textContent).toBe('Seminario B');
    expect(rows[1].querySelector('dd')?.textContent).toBe('Abril · Mañana');
  });
});
