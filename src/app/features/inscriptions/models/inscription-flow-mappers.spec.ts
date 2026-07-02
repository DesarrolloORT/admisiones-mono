import '@angular/compiler';

import type { InscripcionInitialSurvey } from './inscripcion-flow';
import { createInscripcionForms } from './inscripcion-flow-forms';
import {
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  patchBackendSurveyForms,
} from './inscripcion-flow-mappers';

const emptySurveyResponse = {
  tieneDerechoEncuesta: true,
  encuesta: null,
  universidadesConsideradas: [],
  universidadesEducacionSuperior: [],
  opcionesMotivosSeleccionados: [],
  opcionesPublicidadSeleccionadas: [],
};

const emptySurvey: InscripcionInitialSurvey = {
  carreraId: null,
  comienzoId: null,
  turnoId: null,
  nivelProductoId: null,
  completa: false,
  seccionActiva: null,
  cursaSecundaria: null,
  orientacionBachilleratoId: null,
  anioBachilleratoId: null,
  institucionSecundariaId: null,
  ubicacionSecundariaId: null,
  nombreInstitucionSecundaria: null,
  tieneEducacionSuperior: null,
  nivelFormacionMadreId: null,
  nivelFormacionPadreId: null,
  madreEgresadaOrt: null,
  padreEgresadoOrt: null,
  anioDecisionCarreraId: null,
  anioDecisionOrtId: null,
  seInformoEnOtrasUniversidades: null,
  apoyoPadres: null,
  apoyoOtros: null,
  apoyoAmigosFamiliares: null,
  apoyoNadie: null,
  apoyoAmigoPropuesta: null,
  decisionConfirmada: null,
  tuvoAsesoramientoOrt: null,
  valoracionAsesoramientoOrt: null,
  visitoSitioWebOrt: null,
  valoracionSitioWebOrt: null,
  visitoInstalacionesOrt: null,
  valoracionInstalacionesOrt: null,
  recuerdaPublicidadOrt: null,
};

describe('inscripcion flow mappers', () => {
  it('maps every initial survey contract field when building the payload', () => {
    const forms = createInscripcionForms();
    forms.academicForm.patchValue({ carrera: '20', comienzo: '200' });
    forms.educationForm.patchValue({
      cursaSecundaria: 'cursando',
      anioSecundaria: '11',
      tipoBachillerato: '1',
      orientacion: '12',
      lugarSecundaria: '1',
      institucionEducativa: '99',
      estadoEducacionSuperior: '1',
      universidadesEducacionSuperior: ['10'],
      formacionMadre: '5',
      tituloOrtMadre: 'si',
      formacionPadre: '6',
      tituloOrtPadre: 'no',
    });
    forms.academicDecisionForm.patchValue({
      anioDecisionCarrera: '1',
      anioDecisionOrt: '7',
      apoyoDecision: '5',
      otrasUniversidades: 'si',
      universidadesInformadas: ['11'],
      certezaDecision: '1',
      motivosOrt: ['2'],
    });
    forms.ortExperienceForm.patchValue({
      reunionAsesoramiento: 'si',
      calificacionAsesoramiento: 4,
      visitoWeb: 'no',
      calificacionWeb: null,
      visitoSede: 'si',
      calificacionSede: 5,
      recuerdaPublicidad: 'si',
      mediosPublicidad: ['9'],
    });
    forms.workForm.patchValue({
      situacionLaboral: 'trabaja',
      tipoJornadaLaboral: '3',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(Object.keys(payload).sort()).toEqual(
      [
        'anioBachillerato',
        'anioDecisionCarreraId',
        'anioDecisionOrtId',
        'apoyoDecisionId',
        'autorizaInformarEncuesta',
        'carreraId',
        'comienzoId',
        'estadoEducacionSuperiorPreviaId',
        'informacionOtrasUniversidadesLinea1',
        'informacionOtrasUniversidadesLinea2',
        'institucionSecundariaId',
        'madreTutorEgresadoOrt',
        'motivoEleccionOrtIds',
        'nivelDecisionId',
        'nivelFormacionMadreTutorId',
        'nivelFormacionPadreTutorId',
        'nombreInstitucionSecundaria',
        'orientacionBachilleratoId',
        'padreTutorEgresadoOrt',
        'publicidadOrtIds',
        'recuerdaPublicidadOrt',
        'recursaAnioBachillerato',
        'seInformoEnOtrasUniversidades',
        'tipoJornadaId',
        'trabajaActualmente',
        'tuvoAsesoramientoOrt',
        'ubicacionUltimoAnioSecundariaId',
        'universidadConsideradaIds',
        'universidadEducacionSuperiorIds',
        'valoracionAsesoramientoOrtId',
        'valoracionInstalacionesOrtId',
        'valoracionSitioWebOrtId',
        'vecesRecursaAnioBachillerato',
        'visitoInstalacionesOrt',
        'visitoSitioWebOrt',
      ].sort()
    );
    expect(payload).toMatchObject({
      carreraId: 20,
      comienzoId: 200,
      orientacionBachilleratoId: 12,
      anioBachillerato: 11,
      institucionSecundariaId: 99,
      nombreInstitucionSecundaria: null,
      nivelFormacionMadreTutorId: 5,
      madreTutorEgresadoOrt: true,
      padreTutorEgresadoOrt: false,
      anioDecisionCarreraId: 1,
      anioDecisionOrtId: 7,
      apoyoDecisionId: 5,
      seInformoEnOtrasUniversidades: true,
      universidadConsideradaIds: [11],
      estadoEducacionSuperiorPreviaId: 1,
      universidadEducacionSuperiorIds: [10],
      nivelDecisionId: 1,
      tuvoAsesoramientoOrt: true,
      valoracionAsesoramientoOrtId: 4,
      valoracionSitioWebOrtId: null,
      visitoInstalacionesOrt: true,
      valoracionInstalacionesOrtId: 5,
      recuerdaPublicidadOrt: true,
      publicidadOrtIds: [9],
      trabajaActualmente: true,
      tipoJornadaId: 3,
      motivoEleccionOrtIds: [2],
    });
  });

  it('does not send orientation when secondary school is already complete', () => {
    const forms = createInscripcionForms();
    forms.educationForm.patchValue({
      cursaSecundaria: 'no-cursando',
      anioSecundaria: '11',
      tipoBachillerato: '12',
      orientacion: '12',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.orientacionBachilleratoId).toBeNull();
  });
  it('does not send orientation for 1 EMS even if stale baccalaureate values exist', () => {
    const forms = createInscripcionForms();
    forms.educationForm.patchValue({
      cursaSecundaria: 'cursando',
      anioSecundaria: '10',
      tipoBachillerato: '2',
      orientacion: '12',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.orientacionBachilleratoId).toBeNull();
  });
  it('detects complete university education by contract id', () => {
    expect(hasCompleteUniversityEducation('5')).toBe(true);
    expect(hasCompleteUniversityEducation('6')).toBe(true);
    expect(hasCompleteUniversityEducation('4')).toBe(false);
  });

  it('patches backend survey selections into the forms', () => {
    const forms = createInscripcionForms();

    patchBackendSurveyForms(
      {
        ...emptySurvey,
        cursaSecundaria: true,
        anioBachilleratoId: 6,
        orientacionBachilleratoId: 2,
        ubicacionSecundariaId: 1,
        institucionSecundariaId: 99,
        seInformoEnOtrasUniversidades: true,
        decisionConfirmada: false,
        tuvoAsesoramientoOrt: true,
        visitoInstalacionesOrt: true,
        recuerdaPublicidadOrt: true,
      },
      {
        ...emptySurveyResponse,
        universidadesConsideradas: [10],
        universidadesEducacionSuperior: [11],
        opcionesMotivosSeleccionados: [8],
        opcionesPublicidadSeleccionadas: [9],
      },
      { forms, careers: [], previousCareerOptions: [], supportOptions: [] }
    );

    expect(forms.educationForm.getRawValue()).toMatchObject({
      cursaSecundaria: 'cursando',
      anioSecundaria: '6',
      tipoBachillerato: '2',
      orientacion: '2',
      lugarSecundaria: '1',
      institucionEducativa: '99',
      universidadesEducacionSuperior: ['11'],
    });
    expect(forms.academicDecisionForm.getRawValue()).toMatchObject({
      otrasUniversidades: 'si',
      universidadesInformadas: ['10'],
      certezaDecision: '2',
      motivosOrt: ['8'],
    });
    expect(forms.ortExperienceForm.getRawValue()).toMatchObject({
      reunionAsesoramiento: 'si',
      visitoSede: 'si',
      recuerdaPublicidad: 'si',
      mediosPublicidad: ['9'],
    });
  });
});
