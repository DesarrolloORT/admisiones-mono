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
      apoyoDecision: '5',
      otrasUniversidades: 'si',
      certezaDecision: 'decidido',
      motivosOrt: '2',
    });
    forms.ortExperienceForm.patchValue({
      reunionAsesoramiento: 'si',
      visitoWeb: 'no',
      visitoSede: 'si',
      recuerdaPublicidad: 'no',
    });
    forms.workForm.controls.situacionLaboral.setValue('trabaja');

    expect(
      buildInitialSurveyPayload({
        forms,
        previousCareerOptions: [{ value: '3', label: 'No cursé estudios superiores' }],
        motivesOptions: [{ value: '2', label: 'Prestigio académico' }],
      })
    ).toMatchObject({
      trabajaActualmente: 'trabaja',
      idProducto: 20,
      idProceso: 200,
    });
  });
});
