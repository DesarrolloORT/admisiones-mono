import '@angular/compiler';

import { createInscripcionForms } from './inscripcion-flow-forms';
import { buildInitialSurveyPayload, patchBackendSurveyForms } from './inscripcion-flow-mappers';

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
      formacionPadre: '4',
    });
    forms.academicDecisionForm.patchValue({
      anioDecisionCarrera: '1',
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
    forms.workForm.controls.situacionLaboral.setValue('trabaja');

    expect(
      buildInitialSurveyPayload({
        forms,
        previousCareerOptions: [{ value: '3', label: 'No cursé estudios superiores' }],
        motivesOptions: [{ value: '2', label: 'Prestigio académico' }],
      })
    ).toMatchObject({
      trabajaActualmente: true,
      idProducto: 20,
      idProceso: 200,
      ultimoAnioSecundaria: 1,
      ultimoAnioSexto: 11,
      codigoTitulo: 12,
      informarEncuesta: '1',
      codigoInstitucionBac: 99,
    });
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
