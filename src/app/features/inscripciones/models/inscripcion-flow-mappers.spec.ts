import '@angular/compiler';

import { createInscripcionForms } from './inscripcion-flow-forms';
import {
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  patchBackendSurveyForms,
} from './inscripcion-flow-mappers';

describe('inscripcion flow mappers', () => {
  it('maps work status and secondary fields when building the initial survey payload', () => {
    const forms = createInscripcionForms();
    forms.academicForm.patchValue({ carrera: '20', comienzo: '200' });
    forms.educationForm.patchValue({
      cursaSecundaria: 'cursando',
      anioSecundaria: '11',
      tipoBachillerato: '12',
      lugarSecundaria: '1',
      institucionEducativa: '99',
      estadoEducacionSuperior: '3',
      formacionMadre: '4',
      tituloOrtMadre: 'si',
      formacionPadre: '4',
      tituloOrtPadre: 'no',
    });
    forms.academicDecisionForm.patchValue({
      anioDecisionCarrera: '1',
      anioDecisionOrt: '7',
      apoyoDecision: ['5'],
      otrasUniversidades: 'si',
      universidadesInformadas: ['udelar'],
      certezaDecision: 'decidido',
      motivosOrt: ['2'],
    });
    forms.ortExperienceForm.patchValue({
      reunionAsesoramiento: 'si',
      calificacionAsesoramiento: 4,
      visitoWeb: 'no',
      calificacionWeb: null,
      visitoSede: 'si',
      recuerdaPublicidad: 'no',
      mediosPublicidad: [],
    });
    forms.workForm.patchValue({
      situacionLaboral: 'trabaja',
      tipoJornadaLaboral: 'tiempo-completo',
    });

    const payload = buildInitialSurveyPayload({
      forms,
      previousCareerOptions: [{ value: '3', label: 'No cursé estudios superiores' }],
      educationLevelOptions: [{ value: '4', label: 'Universitaria completa' }],
      motivesOptions: [{ value: '2', label: 'Prestigio académico' }],
    });

    expect(payload).toMatchObject({
      trabajaActualmente: true,
      idProducto: 20,
      idProceso: 200,
      ultimoAnioSecundaria: 1,
      ultimoAnioSexto: 11,
      codigoTitulo: 12,
      informarEncuesta: '1',
      codigoInstitucionBac: 99,
      instruccionMadreOrt: true,
      instruccionPadreOrt: false,
      decisionUniversidad: 7,
      tipoJornadaLaboral: 'tiempo-completo',
      opcionesMotivosSeleccionados: [{ idMotivo: 2, nombreMotivo: 'Prestigio académico' }],
    });
  });

  it('detects complete university education without matching non-university labels', () => {
    expect(
      hasCompleteUniversityEducation('1', [
        { value: '1', label: 'Formación universitaria completa' },
      ])
    ).toBe(true);
    expect(
      hasCompleteUniversityEducation('2', [
        { value: '2', label: 'Terciaria no universitaria completa' },
      ])
    ).toBe(false);
  });

  it('patches backend secondary values into the education form', () => {
    const forms = createInscripcionForms();

    patchBackendSurveyForms(
      {
        ultimoanioSecundariaEncuestaIni: true,
        ultimoAnioSextoEncuestaIni: '6',
        codigoTitulo: 2,
        informarEncuestaIni: '1',
        codigoInstitucionBac: 99,
      },
      { tieneDerechoEncuesta: true, encuesta: null, opcionesMotivosSeleccionados: null },
      { forms, careers: [], previousCareerOptions: [] }
    );

    expect(forms.educationForm.getRawValue()).toMatchObject({
      cursaSecundaria: 'cursando',
      anioSecundaria: '6',
      tipoBachillerato: '2',
      lugarSecundaria: '1',
    });
  });
});
