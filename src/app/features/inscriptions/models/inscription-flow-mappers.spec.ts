import '@angular/compiler';

import type { InscripcionInitialSurvey, MetodoPago } from './inscription-flow';
import { createInscripcionForms } from './inscription-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  buildPaymentPayload,
  fromApiPaymentMethod,
  hasCompleteUniversityEducation,
  parseDate,
  patchBackendSurveyForms,
  serializeDate,
  toNullableNumber,
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

  it.each<{ api: string; method: MetodoPago }>([
    { api: 'CUENTA_PERSONAL', method: 'cuenta-personal' },
    { api: 'ABITAB', method: 'abitab' },
    { api: 'PAGANZA', method: 'paganza' },
    { api: 'BANRED', method: 'banred' },
    { api: 'GEOPAY', method: 'geopay' },
    { api: 'SISTARBANC', method: 'cuenta-bancaria' },
  ])('maps the API payment method $api back to $method', ({ api, method }) => {
    expect(fromApiPaymentMethod(api)).toBe(method);
  });

  it('returns null for unknown API payment methods', () => {
    expect(fromApiPaymentMethod('TRANSFERENCIA')).toBeNull();
    expect(fromApiPaymentMethod('')).toBeNull();
    expect(fromApiPaymentMethod(null)).toBeNull();
  });

  it('includes the bank only for the bank account payment method', () => {
    expect(
      buildPaymentPayload({
        idInscripcion: 7,
        metodoPago: 'cuenta-bancaria',
        idBancoSistarbanc: '110',
      })
    ).toEqual({ idInscripcion: 7, tipoPago: 'SISTARBANC', idBancoSistarbanc: '110' });

    expect(
      buildPaymentPayload({ idInscripcion: 7, metodoPago: 'abitab', idBancoSistarbanc: '110' })
    ).toEqual({ idInscripcion: 7, tipoPago: 'ABITAB', idBancoSistarbanc: null });
  });

  it('does not build a confirmation payload without a selected shift', () => {
    const forms = createInscripcionForms();

    expect(buildConfirmPreEnrollmentPayload(forms)).toBeNull();

    forms.academicForm.controls.turno.setValue('300');
    forms.regulationForm.controls.aceptaReglamento.setValue(true);

    expect(buildConfirmPreEnrollmentPayload(forms)).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idOfertasSeleccionadas: [300],
    });
  });

  it.each([false, true])('builds the AP confirmation payload with corporate=%s', isCorporate => {
    const forms = createInscripcionForms();
    forms.regulationForm.controls.aceptaReglamento.setValue(true);

    expect(buildConfirmPreEnrollmentPayload(forms, true)).toBeNull();

    forms.academicForm.controls.seminarios.setValue(['300', '301']);
    forms.workForm.controls.isCorporate.setValue(isCorporate);

    expect(buildConfirmPreEnrollmentPayload(forms, true)).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: isCorporate,
      idOfertasSeleccionadas: [300, 301],
    });
  });

  it('parses slash and ISO dates and rejects rolled-over or malformed values', () => {
    expect(parseDate('26/06/2027')).toEqual(new Date(2027, 5, 26));
    expect(parseDate('2026-06-26T16:29:20')).toEqual(new Date(2026, 5, 26));
    expect(parseDate('31/02/2027')).toBeNull();
    expect(parseDate('basura')).toBeNull();
    expect(parseDate('')).toBeNull();
    expect(parseDate(null)).toBeNull();
    expect(parseDate(undefined)).toBeNull();
  });

  it('serializes dates as yyyy-MM-dd and null as empty string', () => {
    expect(serializeDate(null)).toBe('');
    expect(serializeDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('parses nullable numbers defensively', () => {
    expect(toNullableNumber('')).toBeNull();
    expect(toNullableNumber('abc')).toBeNull();
    expect(toNullableNumber('Infinity')).toBeNull();
    expect(toNullableNumber('42')).toBe(42);
  });

  it('filters invalid ids out of number arrays and nulls empty ones', () => {
    const forms = createInscripcionForms();

    expect(buildInitialSurveyPayload(forms).motivoEleccionOrtIds).toBeNull();

    forms.academicDecisionForm.controls.motivosOrt.setValue(['abc', '5', '']);
    expect(buildInitialSurveyPayload(forms).motivoEleccionOrtIds).toEqual([5]);

    forms.academicDecisionForm.controls.motivosOrt.setValue(['abc']);
    expect(buildInitialSurveyPayload(forms).motivoEleccionOrtIds).toBeNull();
  });

  it('resolves the school place with ubicacion > institucion > nombre precedence', () => {
    expect(
      patchedSchoolPlace({
        ubicacionSecundariaId: 2,
        institucionSecundariaId: 99,
        nombreInstitucionSecundaria: 'Liceo X',
      })
    ).toBe('2');
    expect(
      patchedSchoolPlace({ institucionSecundariaId: 99, nombreInstitucionSecundaria: 'Liceo X' })
    ).toBe('1');
    expect(patchedSchoolPlace({ nombreInstitucionSecundaria: 'Liceo X' })).toBe('2');
    expect(patchedSchoolPlace({})).toBe('');
  });

  it('leaves the academic selection untouched when includeAcademicSelection is false', () => {
    const forms = createInscripcionForms();

    patchBackendSurveyForms(
      {
        ...emptySurvey,
        carreraId: 20,
        comienzoId: 200,
        cursaSecundaria: true,
        anioBachilleratoId: 6,
      },
      emptySurveyResponse,
      { forms, careers: [], includeAcademicSelection: false }
    );

    // El paso 1 queda virgen; el resto de la encuesta sí se patchea.
    expect(forms.academicForm.controls.carrera.value).toBe('');
    expect(forms.academicForm.controls.comienzo.value).toBe('');
    expect(forms.academicForm.controls.tipoPropuesta.value).toBe('');
    expect(forms.educationForm.controls.anioSecundaria.value).toBe('6');
  });

  it('keeps the current proposal type when the survey level cannot be resolved', () => {
    const forms = createInscripcionForms();
    forms.academicForm.controls.tipoPropuesta.setValue('3');

    const proposalType = patchBackendSurveyForms(
      { ...emptySurvey, carreraId: 20 },
      emptySurveyResponse,
      { forms, careers: [] }
    );

    expect(proposalType).toBe('3');
    expect(forms.academicForm.controls.tipoPropuesta.value).toBe('3');
    expect(forms.academicForm.controls.carrera.value).toBe('20');
  });

  it('derives the proposal type from the careers catalog when the survey has no level', () => {
    const forms = createInscripcionForms();

    const proposalType = patchBackendSurveyForms(
      { ...emptySurvey, carreraId: 20 },
      emptySurveyResponse,
      {
        forms,
        careers: [
          {
            idProducto: 20,
            idNivelProducto: 2,
            nombreProducto: 'Tecnicatura',
            nombreNivelProducto: 'Terciaria',
          },
        ],
      }
    );

    expect(proposalType).toBe('2');
    expect(forms.academicForm.controls.tipoPropuesta.value).toBe('2');
  });

  it('prefers the survey level over the careers catalog and blanks unknown levels', () => {
    const forms = createInscripcionForms();
    const careers = [
      {
        idProducto: 20,
        idNivelProducto: 2,
        nombreProducto: 'Tecnicatura',
        nombreNivelProducto: 'Terciaria',
      },
    ];

    expect(
      patchBackendSurveyForms(
        { ...emptySurvey, carreraId: 20, nivelProductoId: 1 },
        emptySurveyResponse,
        {
          forms,
          careers,
        }
      )
    ).toBe('1');

    expect(
      patchBackendSurveyForms(
        { ...emptySurvey, carreraId: 20, nivelProductoId: 99 },
        emptySurveyResponse,
        {
          forms,
          careers,
        }
      )
    ).toBe('');
  });

  function patchedSchoolPlace(overrides: Partial<InscripcionInitialSurvey>): string {
    const forms = createInscripcionForms();
    patchBackendSurveyForms({ ...emptySurvey, ...overrides }, emptySurveyResponse, {
      forms,
      careers: [],
    });
    return forms.educationForm.controls.lugarSecundaria.value;
  }
});
