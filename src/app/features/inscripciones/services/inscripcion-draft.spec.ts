import { TestBed } from '@angular/core/testing';

import type { BorradorInscripcion } from '../models/inscripcion-flow';
import { InscripcionDraft } from './inscripcion-draft';

describe('InscripcionDraft', () => {
  let storage: InscripcionDraft;
  const key = 'inscripcion-borrador:v1:12345672:primera-vez';

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    localStorage.setItem('auth-session', JSON.stringify({ documentNumber: '12345672' }));

    TestBed.configureTestingModule({});
    storage = TestBed.inject(InscripcionDraft);
  });

  it('persists and restores a serializable draft', () => {
    const draft = createDraft();

    storage.save(draft);

    expect(storage.load('primera-vez')).toEqual(draft);
  });

  it('discards a draft with an unknown screen', () => {
    sessionStorage.setItem(key, JSON.stringify({ ...createDraft(), pantalla: 'desconocida' }));

    expect(storage.load('primera-vez')).toBeNull();
    expect(sessionStorage.getItem(key)).toBeNull();
  });
});

function createDraft(): BorradorInscripcion {
  return {
    version: 1,
    escenario: 'primera-vez',
    pantalla: 'encuesta',
    seccionActiva: 'identidad',
    seccionesCompletas: ['educacion'],
    propuesta: {
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '200',
      turno: '300',
    },
    encuesta: {
      educacion: {
        cursaSecundaria: 'cursando',
        lugarSecundaria: 'uruguay',
        estadoEducacionSuperior: '3',
        formacionMadre: '4',
        formacionPadre: '4',
      },
      decisionAcademica: {
        anioDecisionCarrera: '',
        apoyoDecision: '',
        anioDecisionOrt: '',
        otrasUniversidades: '',
        certezaDecision: '',
        motivosOrt: '',
      },
      experienciaOrt: {
        reunionAsesoramiento: '',
        visitoWeb: '',
        visitoSede: '',
        recuerdaPublicidad: '',
      },
      situacionLaboral: { situacionLaboral: 'trabaja' },
    },
    identidad: { vencimientoDocumento: '2030-02-04' },
    reglamento: { aceptaReglamento: false },
    pago: { metodoPago: '' },
  };
}
