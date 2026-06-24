import '@angular/compiler';

import { createInscripcionForms } from './inscripcion-flow-forms';
import { buildInitialSurveyPayload } from './inscripcion-flow-mappers';

describe('inscripcion flow mappers', () => {
  it('maps work status to trabajaActualmente when building the initial survey payload', () => {
    const forms = createInscripcionForms();
    forms.academicForm.patchValue({ carrera: '20', comienzo: '200' });
    forms.educationForm.patchValue({
      cursaSecundaria: 'cursando',
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
    });
  });
});
