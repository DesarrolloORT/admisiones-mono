import { TestBed } from '@angular/core/testing';

import type { BorradorInscripcion } from '../models/inscription-flow';
import { InscripcionDraft } from './inscription-draft';

describe('InscripcionDraft', () => {
  let storage: InscripcionDraft;
  const key = 'inscripcion-borrador:v1:primera-vez';

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    storage = TestBed.inject(InscripcionDraft);
  });

  it('persists and restores a serializable draft', () => {
    const draft = createDraft();

    storage.save(draft);

    expect(storage.load('primera-vez')).toEqual({
      ...draft,
      identidad: { vencimientoDocumento: '' },
      pago: { metodoPago: '' },
      preinscription: null,
    });
  });

  it('discards a draft with an unknown step', () => {
    sessionStorage.setItem(key, JSON.stringify({ ...createDraft(), paso: 'desconocido' }));

    expect(storage.load('primera-vez')).toBeNull();
    expect(sessionStorage.getItem(key)).toBeNull();
  });

  it('invalidates drafts from version 1', () => {
    sessionStorage.setItem(key, JSON.stringify({ ...createDraft(), version: 1 }));

    expect(storage.load('primera-vez')).toBeNull();
    expect(sessionStorage.getItem(key)).toBeNull();
  });
});

function createDraft(): BorradorInscripcion {
  return {
    version: 2,
    escenario: 'primera-vez',
    paso: 'encuesta',
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
        anioSecundaria: '2-ems',
        tipoBachillerato: '1',
        orientacion: 'cientifico',
        lugarSecundaria: 'uruguay',
        departamento: 'montevideo',
        institucionEducativa: 'liceo-publico',
        estadoEducacionSuperior: '3',
        universidadesEducacionSuperior: [],
        formacionMadre: '4',
        tituloOrtMadre: 'si',
        formacionPadre: '4',
        tituloOrtPadre: 'no',
      },
      decisionAcademica: {
        anioDecisionCarrera: '',
        apoyoDecision: '',
        anioDecisionOrt: '',
        otrasUniversidades: '',
        universidadesInformadas: [],
        certezaDecision: '',
        motivosOrt: [],
      },
      experienciaOrt: {
        reunionAsesoramiento: '',
        calificacionAsesoramiento: null,
        visitoWeb: '',
        calificacionWeb: null,
        visitoSede: '',
        calificacionSede: null,
        recuerdaPublicidad: '',
        mediosPublicidad: [],
      },
      situacionLaboral: { situacionLaboral: 'trabaja', tipoJornadaLaboral: '1' },
    },
    identidad: { vencimientoDocumento: '2030-02-04' },
    reglamento: { aceptaReglamento: false },
    pago: { metodoPago: '' },
    preinscription: null,
  };
}
