import '@angular/compiler';

import type { InscripcionInitialSurvey } from './inscription-flow';
import { createInscripcionForms } from './inscription-flow-forms';
import {
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  patchBackendSurveyForms,
} from './inscription-flow-mappers';

const emptySurveyResponse = {
  tieneDerechoEncuesta: true,
  encuesta: null,
  universidadesConsideradas: [],
  universidadesConsideradasOtros: [],
  universidadesEducacionSuperior: [],
  universidadesEducacionSuperiorOtros: [],
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
  recursaAnioBachillerato: null,
  vecesRecursaAnioBachillerato: null,
  institucionSecundariaId: null,
  ubicacionSecundariaId: null,
  nombreInstitucionSecundaria: null,
  estadoEducacionSuperiorPreviaId: null,
  nivelFormacionMadreId: null,
  nivelFormacionPadreId: null,
  madreEgresadaOrt: null,
  padreEgresadoOrt: null,
  anioDecisionCarreraId: null,
  anioDecisionOrtId: null,
  seInformoEnOtrasUniversidades: null,
  apoyoDecisionId: null,
  nivelDecisionId: null,
  tuvoAsesoramientoOrt: null,
  valoracionAsesoramientoOrt: null,
  visitoSitioWebOrt: null,
  valoracionSitioWebOrt: null,
  visitoInstalacionesOrt: null,
  valoracionInstalacionesOrt: null,
  recuerdaPublicidadOrt: null,
};

describe('inscription flow mappers', () => {
  it('maps every initial survey contract field when building the payload', () => {
    const forms = createInscripcionForms();
    forms.academicForm.patchValue({ carrera: '20', comienzo: '200' });
    forms.educationForm.patchValue({
      cursaSecundaria: 'cursando',
      anioSecundaria: '11',
      orientacion: '12',
      recursaAnioBachillerato: 'si',
      vecesRecursaAnioBachillerato: 2,
      lugarSecundaria: '1',
      institucionEducativa: '99',
      estadoEducacionSuperior: '1',
      universidadesEducacionSuperior: ['10', '0'],
      universidadEducacionSuperiorOtro: ' Universidad inventada ',
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
      universidadesInformadas: ['11', '0'],
      universidadInformadaOtro: ' Otra consultada ',
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
        'carreraId',
        'comienzoId',
        'cursaSecundariaActualmente',
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
        'universidadConsideradaOtros',
        'universidadEducacionSuperiorIds',
        'universidadEducacionSuperiorOtros',
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
      cursaSecundariaActualmente: true,
      recursaAnioBachillerato: true,
      vecesRecursaAnioBachillerato: 2,
      institucionSecundariaId: 99,
      nombreInstitucionSecundaria: null,
      nivelFormacionMadreTutorId: 5,
      madreTutorEgresadoOrt: true,
      padreTutorEgresadoOrt: false,
      anioDecisionCarreraId: 1,
      anioDecisionOrtId: 7,
      apoyoDecisionId: 5,
      seInformoEnOtrasUniversidades: true,
      universidadConsideradaIds: [11, 0],
      universidadConsideradaOtros: ['Otra consultada'],
      estadoEducacionSuperiorPreviaId: 1,
      universidadEducacionSuperiorIds: [10, 0],
      universidadEducacionSuperiorOtros: ['Universidad inventada'],
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
      orientacion: '12',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.orientacionBachilleratoId).toBeNull();
  });

  it('does not send other-university text unless option 0 is selected', () => {
    const forms = createInscripcionForms();
    forms.educationForm.patchValue({
      estadoEducacionSuperior: '1',
      universidadesEducacionSuperior: ['10'],
      universidadEducacionSuperiorOtro: 'Ignorada',
    });
    forms.academicDecisionForm.patchValue({
      otrasUniversidades: 'si',
      universidadesInformadas: ['11'],
      universidadInformadaOtro: 'Ignorada',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.universidadEducacionSuperiorOtros).toBeNull();
    expect(payload.universidadConsideradaOtros).toBeNull();
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
        recursaAnioBachillerato: true,
        vecesRecursaAnioBachillerato: 2,
        ubicacionSecundariaId: 1,
        institucionSecundariaId: 99,
        seInformoEnOtrasUniversidades: true,
        nivelDecisionId: 2,
        tuvoAsesoramientoOrt: true,
        visitoInstalacionesOrt: true,
        recuerdaPublicidadOrt: true,
      },
      {
        ...emptySurveyResponse,
        universidadesConsideradas: [10, 0],
        universidadesConsideradasOtros: ['Otra consultada'],
        universidadesEducacionSuperior: [11, 0],
        universidadesEducacionSuperiorOtros: ['Otra superior'],
        opcionesMotivosSeleccionados: [8],
        opcionesPublicidadSeleccionadas: [9],
      },
      { forms, careers: [] }
    );

    expect(forms.educationForm.getRawValue()).toMatchObject({
      cursaSecundaria: 'cursando',
      anioSecundaria: '6',
      orientacion: '2',
      recursaAnioBachillerato: 'si',
      vecesRecursaAnioBachillerato: 2,
      lugarSecundaria: '1',
      institucionEducativa: '99',
      universidadesEducacionSuperior: ['11', '0'],
      universidadEducacionSuperiorOtro: 'Otra superior',
    });
    expect(forms.academicDecisionForm.getRawValue()).toMatchObject({
      otrasUniversidades: 'si',
      universidadesInformadas: ['10', '0'],
      universidadInformadaOtro: 'Otra consultada',
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
