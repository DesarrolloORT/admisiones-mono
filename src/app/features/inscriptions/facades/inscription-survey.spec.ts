import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionDetail } from '../models/inscription-detail';
import {
  deriveInitialInscripcionState,
  type InscripcionInitialSurveyResolved,
} from '../models/inscription-entry';
import type { InscripcionInitialSurvey } from '../models/inscription-flow';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';
import { InscripcionSurveyIdentityFacade } from './inscription-survey-identity';
import { InscripcionSurveyOptionsFacade } from './inscription-survey-options';

const AP_CAREERS = [
  {
    idProducto: 30,
    idNivelProducto: 3,
    nombreProducto: 'Programa de Asesoramiento Financiero',
    nombreNivelProducto: 'Actualización profesional',
  },
];

describe('InscripcionSurveyFacade', () => {
  const saveInitialSurvey = vi.fn();
  const confirmPreEnrollment = vi.fn();
  const uploadIdentityDocument = vi.fn();
  const uploadIdentityPhoto = vi.fn();
  const getIdentityPreload = vi.fn();
  const getStudentRegulationAcceptance = vi.fn();
  const payment = { outcome: signal(null) };

  beforeEach(() => {
    payment.outcome.set(null);
    saveInitialSurvey.mockReset().mockReturnValue(of(true));
    confirmPreEnrollment.mockReset().mockReturnValue(
      of({
        confirmada: true,
        enEspera: false,
        fechaVencimientoPago: null,
        seniaInscripcion: null,
        saldoCuenta: null,
        resumen: null,
      })
    );
    uploadIdentityDocument.mockReset().mockReturnValue(of(true));
    uploadIdentityPhoto.mockReset().mockReturnValue(of(true));
    getIdentityPreload
      .mockReset()
      .mockReturnValue(of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null }));
    getStudentRegulationAcceptance
      .mockReset()
      .mockReturnValue(of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null }));
  });

  it('resumes an incomplete backend survey at its active section', () => {
    const { survey, process } = createFacade({
      tieneDerechoEncuesta: true,
      encuesta: createInitialSurvey({ seccionActiva: 'decision-academica' }),
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });

    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.activeSection()).toBe('decision-academica');
  });

  it('keeps only identity and regulation when the survey is not required', async () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });

    expect(survey.visibleSections()).toEqual(['identidad', 'reglamento']);
    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('asks only for the inscription owner when AP has survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ tieneDerechoEncuesta: true }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('educacion');

    forms.academicForm.controls.tipoPropuesta.setValue('3');
    forms.academicForm.controls.carrera.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['situacion-laboral', 'identidad', 'reglamento']);
    expect(survey.activeSection()).toBe('situacion-laboral');
    expect(forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
  });

  it('asks for a corporate inscription before identity when AP has no survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ tieneDerechoEncuesta: false }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('identidad');

    forms.academicForm.controls.tipoPropuesta.setValue('3');
    forms.academicForm.controls.carrera.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['situacion-laboral', 'identidad', 'reglamento']);
    expect(survey.activeSection()).toBe('situacion-laboral');
  });

  it('does not persist the initial survey for AP even with survey rights', async () => {
    const { survey, forms } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.tipoPropuesta.setValue('3');

    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('confirms a personal AP pre-enrollment and advances to payment', () => {
    const { survey, forms, process } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.tipoPropuesta.setValue('3');
    forms.academicForm.controls.carrera.setValue('30');
    forms.academicForm.controls.seminarios.setValue(['300']);
    process.flow.goTo('encuesta');
    TestBed.tick();

    expect(forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
    forms.workForm.controls.isCorporate.setValue(false);
    const frente = preloadFile('frente.png');
    const dorso = preloadFile('dorso.png');
    const selfie = preloadFile('selfie.png');
    applyIdentityPreload(survey, {
      frente,
      dorso,
      selfie,
      fechaVencimiento: '2030-02-04',
    });
    survey.identityForm.controls.identidadCorrecta.setValue(true);
    survey.regulationForm.controls.aceptaReglamento.setValue(true);

    survey.continue();

    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idOfertasSeleccionadas: [300],
    });
    expect(process.flow.currentStep()).toBe('pago');
    expect(payment.outcome()).toBeNull();
  });

  it('finishes a corporate AP pre-enrollment without opening payment', () => {
    const { survey, forms, process } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.tipoPropuesta.setValue('3');
    forms.academicForm.controls.carrera.setValue('30');
    forms.academicForm.controls.seminarios.setValue(['300']);
    forms.workForm.controls.isCorporate.setValue(true);
    process.flow.goTo('encuesta');
    TestBed.tick();

    const identity = preloadFile('identidad.png');
    applyIdentityPreload(survey, {
      frente: identity,
      dorso: identity,
      selfie: identity,
      fechaVencimiento: '2030-02-04',
    });
    survey.identityForm.controls.identidadCorrecta.setValue(true);
    survey.regulationForm.controls.aceptaReglamento.setValue(true);

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      esInscripcionCorporativa: true,
      idOfertasSeleccionadas: [300],
    });
    expect(process.flow.currentStep()).toBe('encuesta');
    expect(payment.outcome()).toBe('inscription-en-proceso');
  });

  it('completes identity and advances when confirming a complete backend preload', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const file = preloadFile('identidad.png');

    applyIdentityPreload(survey, {
      frente: file,
      dorso: file,
      selfie: file,
      fechaVencimiento: '2030-02-04',
    });

    expect(survey.identity.requiresIdentityConfirmation()).toBe(true);
    expect(survey.identityForm.controls.identidadCorrecta.hasError('required')).toBe(true);

    survey.identityForm.controls.identidadCorrecta.setValue(true);

    expect(survey.getSectionState('identidad')).toBe('completa');
    expect(survey.activeSection()).toBe('reglamento');
  });

  it('does not upload preloaded identity files when only confirmation changes', () => {
    const { survey, forms } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = preloadFile('frente.png');
    const dorso = preloadFile('dorso.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      frente,
      dorso,
      selfie,
      fechaVencimiento: '2030-02-04',
    });
    survey.identity.updateIdentityFile('frente', preloadedFileEvent(frente));
    survey.identity.updateIdentityFile('dorso', preloadedFileEvent(dorso));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.identidadCorrecta.setValue(true);
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    forms.academicForm.controls.turno.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idOfertasSeleccionadas: [300],
    });
  });

  it('does not upload preloaded identity document when the expiration control is dirty but unchanged', () => {
    const { survey, forms } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = preloadFile('frente.png');
    const dorso = preloadFile('dorso.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      frente,
      dorso,
      selfie,
      fechaVencimiento: '2030-02-04',
    });
    survey.identity.updateIdentityFile('frente', preloadedFileEvent(frente));
    survey.identity.updateIdentityFile('dorso', preloadedFileEvent(dorso));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    survey.identityForm.controls.identidadCorrecta.setValue(true);
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    forms.academicForm.controls.turno.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
  });

  it('uploads preloaded identity document when the expiration changes', () => {
    const { survey, forms } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = preloadFile('frente.png');
    const dorso = preloadFile('dorso.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      frente,
      dorso,
      selfie,
      fechaVencimiento: '2030-02-04',
    });
    survey.identity.updateIdentityFile('frente', preloadedFileEvent(frente));
    survey.identity.updateIdentityFile('dorso', preloadedFileEvent(dorso));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2031, 1, 4));
    survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    survey.identityForm.controls.identidadCorrecta.setValue(true);
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    forms.academicForm.controls.turno.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      fecha: '2031-02-04',
      frente,
      dorso,
    });
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
  });

  it('does not confirm when a previous visible section is invalid', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    survey.activeSection.set('reglamento');

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identidad');
    expect(survey.preEnrollmentError()).toBe(
      'Completá la información pendiente antes de confirmar la preinscripción.'
    );
  });

  it('uploads touched identity files before confirming pre-enrollment', () => {
    const { survey, forms } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = new File(['front'], 'frente.png', { type: 'image/png' });
    const dorso = new File(['back'], 'dorso.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    survey.identity.updateIdentityFile('frente', fileEvent(frente));
    survey.identity.updateIdentityFile('dorso', fileEvent(dorso));
    survey.identity.updateIdentityFile('selfie', fileEvent(selfie));
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    forms.academicForm.controls.turno.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      fecha: '2030-02-04',
      frente,
      dorso,
    });
    expect(uploadIdentityPhoto).toHaveBeenCalledWith(selfie);
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idOfertasSeleccionadas: [300],
    });
  });

  it('waits for document and photo before saving the survey and confirming', () => {
    const documentResult = new Subject<boolean>();
    const photoResult = new Subject<boolean>();
    uploadIdentityDocument.mockReturnValue(documentResult);
    uploadIdentityPhoto.mockReturnValue(photoResult);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledOnce();
    expect(uploadIdentityPhoto).toHaveBeenCalledOnce();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    documentResult.next(true);
    documentResult.complete();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    photoResult.next(true);
    photoResult.complete();

    expect(saveInitialSurvey).toHaveBeenCalledOnce();
    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('pago');
  });

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('reopens identity and stops the chain when photo returns $failure', ({ result }) => {
    uploadIdentityPhoto.mockReturnValue(result);
    const { survey, frente, dorso, selfie } = prepareFinalizableSurvey();

    survey.continue();

    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identidad');
    expect(survey.getSectionState('identidad')).toBe('activa');
    expect(survey.identity.identityFiles()).toEqual({ frente, dorso, selfie });
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar la verificación de identidad. Intentá nuevamente.'
    );
  });

  it('marks identity complete reactively once files and expiry are set, without pressing Continuar', () => {
    const { survey } = createFacade(createSurveyResponse({ tieneDerechoEncuesta: false }));
    const frente = new File(['front'], 'frente.png', { type: 'image/png' });
    const dorso = new File(['back'], 'dorso.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    survey.identity.updateIdentityFile('frente', fileEvent(frente));
    survey.identity.updateIdentityFile('dorso', fileEvent(dorso));
    survey.identity.updateIdentityFile('selfie', fileEvent(selfie));

    expect(survey.getSectionState('identidad')).toBe('completa');
    // El check aparece sin avanzar de sección (eso sigue siendo tarea de Continuar).
    expect(survey.activeSection()).toBe('identidad');
  });

  it.each([
    {
      name: 'changing an identity file',
      mutate: (survey: InscripcionSurveyFacade) =>
        survey.identity.updateIdentityFile(
          'selfie',
          fileEvent(new File(['new'], 'selfie-2.png', { type: 'image/png' }))
        ),
    },
    {
      name: 'changing the document expiry',
      mutate: (survey: InscripcionSurveyFacade) =>
        survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2031, 0, 1)),
    },
  ])(
    'restores reactive identity completion after an upload failure when $name',
    ({ mutate }: { name: string; mutate: (survey: InscripcionSurveyFacade) => void }) => {
      uploadIdentityPhoto.mockReturnValue(of(false));
      const { survey } = prepareFinalizableSurvey();

      survey.continue();
      expect(survey.getSectionState('identidad')).toBe('activa');

      mutate(survey);

      expect(survey.getSectionState('identidad')).toBe('completa');
    }
  );

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('does not confirm when the final survey save returns $failure', ({ result }) => {
    saveInitialSurvey.mockReturnValue(result);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
    );
  });

  it('uses only enEspera to decide between payment and manual review screens', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmada: false,
        enEspera: false,
        fechaVencimientoPago: null,
        seniaInscripcion: null,
        saldoCuenta: null,
        resumen: null,
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('pago');
    expect(payment.outcome()).toBeNull();
    expect(survey.preEnrollmentError()).toBeNull();
  });

  it('shows the in-process outcome when pre-enrollment is waiting for manual review', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmada: true,
        enEspera: true,
        idInscripcion: null,
        fechaVencimientoPago: null,
        seniaInscripcion: 0,
        saldoCuenta: null,
        resumen: { carrera: 'Sistemas', comienzo: 'Marzo', turno: 'Noche' },
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(process.flow.currentStep()).toBe('encuesta');
    expect(payment.outcome()).toBe('inscription-en-proceso');
  });

  it('uses school year orientations directly from the selected year catalog', () => {
    const { survey } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: createInitialSurvey({ seccionActiva: 'educacion' }),
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [],
          nivelesFormacionTutores: [],
          aniosBachillerato: [
            { id: 10, label: '1 EMS', baccalaureates: [] },
            {
              id: 11,
              label: '2 EMS',
              baccalaureates: [{ id: 20, label: 'Bachillerato A', orientation: 'Cientifico' }],
            },
            {
              id: 12,
              label: '3 EMS',
              baccalaureates: [{ id: 30, label: 'Bachillerato B', orientation: 'Economia' }],
            },
          ],
        },
      }
    );
    TestBed.tick();

    expect(survey.options.schoolYearOptions()).toEqual([
      { value: '10', label: '1 EMS' },
      { value: '11', label: '2 EMS' },
      { value: '12', label: '3 EMS' },
    ]);

    survey.educationForm.patchValue({ cursaSecundaria: 'cursando', anioSecundaria: '10' });

    expect(survey.shouldAskBaccalaureateOrientation()).toBe(false);
    expect(survey.options.orientationOptions()).toEqual([]);

    survey.educationForm.controls.anioSecundaria.setValue('11');

    expect(survey.shouldAskBaccalaureateOrientation()).toBe(true);
    expect(survey.options.orientationOptions()).toEqual([{ value: '20', label: 'Cientifico' }]);

    survey.educationForm.controls.orientacion.setValue('20');
    survey.educationForm.controls.anioSecundaria.setValue('12');

    expect(survey.options.orientationOptions()).toEqual([{ value: '30', label: 'Economia' }]);
    expect(survey.educationForm.controls.orientacion.value).toBe('');
  });

  it('requires free-text details for recursado and Otro university options', () => {
    const { survey } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          aniosBachillerato: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [
            { id: 0, label: 'Otra' },
            { id: 10, label: 'Udelar' },
          ],
          nivelesFormacionTutores: [],
        },
        decisionAcademica: {
          aniosEducacionMediaSuperior: [],
          apoyosDecision: [],
          nivelesDecision: [],
          universidades: [
            { id: 0, label: 'Otra' },
            { id: 11, label: 'UCU' },
          ],
          motivosEleccionOrt: [],
        },
      }
    );

    survey.educationForm.controls.recursaAnioBachillerato.setValue('si');
    expect(survey.shouldAskRecursaCount()).toBe(true);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.hasError('required')).toBe(
      true
    );
    survey.educationForm.controls.vecesRecursaAnioBachillerato.setValue(0);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.hasError('min')).toBe(true);
    survey.educationForm.controls.vecesRecursaAnioBachillerato.setValue(2);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.valid).toBe(true);

    survey.educationForm.patchValue({
      estadoEducacionSuperior: '1',
      universidadesEducacionSuperior: ['0'],
    });
    expect(survey.shouldAskHigherEducationOtherUniversity()).toBe(true);
    expect(
      survey.educationForm.controls.universidadEducacionSuperiorOtro.hasError('required')
    ).toBe(true);
    survey.educationForm.controls.universidadEducacionSuperiorOtro.setValue('Otra superior');
    expect(survey.educationForm.controls.universidadEducacionSuperiorOtro.valid).toBe(true);

    survey.academicDecisionForm.patchValue({
      otrasUniversidades: 'si',
      universidadesInformadas: ['0'],
    });
    expect(survey.shouldAskInformedOtherUniversity()).toBe(true);
    expect(survey.academicDecisionForm.controls.universidadInformadaOtro.hasError('required')).toBe(
      true
    );
    survey.academicDecisionForm.controls.universidadInformadaOtro.setValue('Otra consultada');
    expect(survey.academicDecisionForm.controls.universidadInformadaOtro.valid).toBe(true);
  });
  it('blocks pre-enrollment when a university career has a disallowed baccalaureate year', () => {
    const { survey, forms } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [],
          nivelesFormacionTutores: [],
          aniosBachillerato: [
            { id: 4, label: '4º año', baccalaureates: [] },
            { id: 5, label: '5º año', baccalaureates: [] },
          ],
        },
      },
      [
        {
          idProducto: 100,
          idNivelProducto: 1,
          nombreProducto: 'Ingeniería',
          nombreNivelProducto: 'Universitaria',
        },
      ]
    );

    forms.academicForm.controls.tipoPropuesta.setValue('1');
    forms.academicForm.controls.carrera.setValue('100');
    survey.educationForm.controls.cursaSecundaria.setValue('cursando');
    survey.educationForm.controls.anioSecundaria.setValue('4');

    expect(survey.isUniversityCareer()).toBe(true);
    expect(
      survey.educationForm.controls.anioSecundaria.hasError('bachilleratoNoUniversitario')
    ).toBe(true);

    survey.educationForm.controls.anioSecundaria.setValue('5');
    expect(
      survey.educationForm.controls.anioSecundaria.hasError('bachilleratoNoUniversitario')
    ).toBe(false);
  });

  it('does not flag disallowed years for non-university careers', () => {
    const { survey, forms } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [],
          nivelesFormacionTutores: [],
          aniosBachillerato: [{ id: 4, label: '4º año', baccalaureates: [] }],
        },
      },
      [
        {
          idProducto: 200,
          idNivelProducto: 2,
          nombreProducto: 'Tecnicatura',
          nombreNivelProducto: 'Terciaria',
        },
      ]
    );

    forms.academicForm.controls.tipoPropuesta.setValue('2');
    forms.academicForm.controls.carrera.setValue('200');
    survey.educationForm.controls.cursaSecundaria.setValue('cursando');
    survey.educationForm.controls.anioSecundaria.setValue('4');

    expect(survey.isUniversityCareer()).toBe(false);
    expect(
      survey.educationForm.controls.anioSecundaria.hasError('bachilleratoNoUniversitario')
    ).toBe(false);
  });

  it('sets the pre-enrollment error and stops the spinner when confirmation fails', () => {
    confirmPreEnrollment.mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
    );
    expect(survey.finalizingPreEnrollment()).toBe(false);
    expect(process.flow.currentStep()).toBe('encuesta');
    expect(payment.outcome()).toBeNull();
  });

  it('reports an error instead of confirming when no shift is selected', () => {
    const { survey, forms } = prepareFinalizableSurvey();
    forms.academicForm.controls.turno.setValue('');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo confirmar la preinscripción con la oferta seleccionada.'
    );
    expect(survey.finalizingPreEnrollment()).toBe(false);
  });

  it('defers the regulation acceptance lookup until the regulation section is reached', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ aceptoReglamentoEstudiantil: true, fechaAceptacion: '10/05/2026' })
    );
    const { survey, process } = createFacade(createSurveyResponse());

    expect(getStudentRegulationAcceptance).not.toHaveBeenCalled();

    process.flow.goTo('encuesta');
    survey.activeSection.set('reglamento');
    TestBed.tick();

    expect(getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('does not repeat the regulation lookup when navigating back and forth', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null })
    );
    const { survey, process } = createFacade(createSurveyResponse());

    process.flow.goTo('encuesta');
    survey.activeSection.set('reglamento');
    TestBed.tick();
    survey.activeSection.set('educacion');
    TestBed.tick();
    survey.activeSection.set('reglamento');
    TestBed.tick();

    expect(getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('prefills the regulation section when the student already accepted it', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ aceptoReglamentoEstudiantil: true, fechaAceptacion: '10/05/2026' })
    );
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('encuesta');
    survey.activeSection.set('reglamento');
    TestBed.tick();

    expect(survey.hasAcceptedStudentRegulation()).toBe(true);
    expect(survey.regulationForm.controls.aceptaReglamento.value).toBe(true);
    expect(survey.submittedAcceptanceDate()).toEqual(new Date(2026, 4, 10));
    expect(survey.getSectionState('reglamento')).toBe('completa');
  });

  it('keeps the regulation unaccepted when the acceptance lookup fails', () => {
    getStudentRegulationAcceptance.mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('encuesta');
    survey.activeSection.set('reglamento');
    TestBed.tick();

    expect(survey.hasAcceptedStudentRegulation()).toBe(false);
    expect(survey.regulationForm.controls.aceptaReglamento.value).toBe(false);
    expect(survey.submittedAcceptanceDate()).toBeNull();
  });

  it('marks the regulation as accepted from the reader', () => {
    const { survey } = createFacade(createSurveyResponse());
    survey.openRegulationReader();
    expect(survey.readerOpen()).toBe(true);

    survey.acceptRegulation();

    expect(survey.regulationForm.controls.aceptaReglamento.value).toBe(true);
    expect(survey.readerOpen()).toBe(false);
    expect(survey.activeSection()).toBe('reglamento');
    expect(survey.getSectionState('reglamento')).toBe('completa');
  });

  it('closes the reader on back, retreats sections and never leaves the step', () => {
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('encuesta');
    survey.activeSection.set('decision-academica');
    survey.openRegulationReader();

    expect(survey.canGoBack()).toBe(true);
    survey.back();

    expect(survey.readerOpen()).toBe(false);
    expect(survey.activeSection()).toBe('decision-academica');
    expect(process.flow.currentStep()).toBe('encuesta');

    survey.back();

    expect(survey.activeSection()).toBe('educacion');
    expect(process.flow.currentStep()).toBe('encuesta');

    // Primera sección visible: no hay a dónde volver, el paso 1 queda inalcanzable.
    expect(survey.canGoBack()).toBe(false);
    survey.back();

    expect(survey.activeSection()).toBe('educacion');
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('starts an empty survey when the backend has no survey yet', () => {
    const { survey } = createFacade(null);

    expect(survey.surveyLoadError()).toBeNull();
    expect(survey.hasInitialSurveyRight()).toBe(true);
    expect(survey.scenario()).toBe('primera-vez');
    expect(survey.activeSection()).toBe('educacion');
    expect(survey.loadingSurveyState()).toBe(false);
  });

  it('shows the survey load error when the resolver reports a failure', () => {
    const { survey } = createFacade(null, {}, [], { loadFailed: true });

    expect(survey.surveyLoadError()).toBe(
      'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
    );
  });

  it('fetchResolvedInitialSurvey maps errors to loadFailed and toggles the loading flag', async () => {
    const getInitialSurvey = vi.fn().mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey } = createFacade(null, {}, [], { skipApply: true, getInitialSurvey });

    await expect(firstValueFrom(survey.fetchResolvedInitialSurvey())).resolves.toEqual({
      initialSurvey: null,
      loadFailed: true,
    });
    expect(survey.loadingSurveyState()).toBe(false);
  });

  it('fetchResolvedInitialSurvey maps a 404 to an empty survey with right', async () => {
    const getInitialSurvey = vi.fn().mockReturnValue(throwError(() => ({ status: 404 })));
    const { survey } = createFacade(null, {}, [], { skipApply: true, getInitialSurvey });

    await expect(firstValueFrom(survey.fetchResolvedInitialSurvey())).resolves.toEqual({
      initialSurvey: expect.objectContaining({ tieneDerechoEncuesta: true, encuesta: null }),
      loadFailed: false,
    });
    expect(survey.loadingSurveyState()).toBe(false);
  });

  function createSurveyResponse(overrides: Record<string, unknown> = {}) {
    return {
      tieneDerechoEncuesta: true,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
      ...overrides,
    };
  }

  // Detalle mínimo "En proceso" sin bloque `detalle` ni ofertas de interés: no hay
  // precarga posible, así que la encuesta prellena el paso 1 y el flujo arranca en el
  // paso 2. El posicionamiento del flujo y la selección de slice los deriva la función
  // pura (testeada en inscription-entry.spec); acá solo se aplican.
  const RESUME_DETAIL: InscripcionDetail = {
    estado: 'En proceso',
    detalle: null,
    intereses: [],
    pagoPendiente: null,
    seniaMinima: null,
    confirmada: null,
  };

  function createFacade(
    initialSurvey: unknown,
    catalogOverrides: Record<string, unknown> = {},
    careers: unknown[] = [],
    options: { loadFailed?: boolean; getInitialSurvey?: () => unknown; skipApply?: boolean } = {}
  ): {
    survey: InscripcionSurveyFacade;
    process: InscripcionProcessStore;
    forms: InscripcionFormsStore;
  } {
    const getInitialSurvey = options.getInitialSurvey ?? (() => of(initialSurvey));
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        InscripcionSurveyOptionsFacade,
        InscripcionSurveyIdentityFacade,
        InscripcionSurveyFacade,
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of(careers),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
            getSeminarios: () =>
              of([{ idOferta: 300, idProceso: 200, nombre: 'Marco legal', fechaComienzo: null }]),
            getCountryLocations: () => of([]),
            getInstituciones: () => of([]),
            getInitialSurveyCatalogs: () =>
              of({
                educacion: {
                  ubicacionesUltimoAnioSecundaria: [],
                  aniosBachillerato: [],
                  estadosEducacionSuperiorPrevia: [],
                  universidades: [],
                  nivelesFormacionTutores: [],
                },
                decisionAcademica: {
                  aniosEducacionMediaSuperior: [],
                  apoyosDecision: [],
                  nivelesDecision: [],
                  universidades: [],
                  motivosEleccionOrt: [],
                },
                experienciaOrt: { valoraciones: [], publicidadesOrt: [] },
                ...catalogOverrides,
              }),
          },
        },
        { provide: InscripcionPaymentFacade, useValue: payment },
        {
          provide: Inscripciones,
          useValue: {
            getStudentRegulationAcceptance,
            getIdentityPreload,
            getInitialSurvey,
            saveInitialSurvey,
            uploadIdentityDocument,
            uploadIdentityPhoto,
            confirmPreEnrollment,
            registerProductInterest: vi.fn(),
          },
        },
      ],
    });
    const survey = TestBed.inject(InscripcionSurveyFacade);
    const process = TestBed.inject(InscripcionProcessStore);
    const forms = TestBed.inject(InscripcionFormsStore);

    // Réplica de lo que hace InscripcionProcessFacade.applyInitialState para el slice
    // de encuesta: deriva y aplica, posicionando el paso al final.
    if (!options.skipApply) {
      const resolved: InscripcionInitialSurveyResolved = {
        initialSurvey: (initialSurvey ?? null) as InscripcionInitialSurveyResolved['initialSurvey'],
        loadFailed: options.loadFailed ?? false,
      };
      const state = deriveInitialInscripcionState({
        entry: {
          intent: 'retomar',
          detail: RESUME_DETAIL,
          idProducto: 20,
          idProceso: 200,
          idOfertas: [],
          idNivelProducto: null,
        },
        survey: resolved,
      });
      survey.applyInitialState(state.survey);
      process.flow.goTo(state.step);
    }

    return { survey, process, forms };
  }

  function prepareFinalizableSurvey() {
    const result = createFacade({
      tieneDerechoEncuesta: true,
      encuesta: createInitialSurvey({ completa: true }),
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = new File(['front'], 'frente.png', { type: 'image/png' });
    const dorso = new File(['back'], 'dorso.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    result.survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    result.survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    result.survey.identity.updateIdentityFile('frente', fileEvent(frente));
    result.survey.identity.updateIdentityFile('dorso', fileEvent(dorso));
    result.survey.identity.updateIdentityFile('selfie', fileEvent(selfie));
    result.survey.regulationForm.controls.aceptaReglamento.setValue(true);
    result.forms.academicForm.controls.turno.setValue('300');

    return { ...result, frente, dorso, selfie };
  }

  function preloadFile(name: string): File {
    const bytes = new Uint8Array([1, 2, 3]).buffer;
    return {
      name,
      size: 3,
      type: 'image/png',
      arrayBuffer: () => Promise.resolve(bytes),
    } as File;
  }
  function applyIdentityPreload(
    survey: InscripcionSurveyFacade,
    preload: InscripcionIdentityPreload
  ): void {
    (
      survey.identity as unknown as {
        applyIdentityPreload(preload: InscripcionIdentityPreload): void;
      }
    ).applyIdentityPreload(preload);
  }
  function preloadedFileEvent(
    file: File
  ): Parameters<InscripcionSurveyIdentityFacade['updateIdentityFile']>[1] {
    return { value: [{ isValid: true, isPreloaded: true, file }] } as Parameters<
      InscripcionSurveyIdentityFacade['updateIdentityFile']
    >[1];
  }
  function fileEvent(
    file: File
  ): Parameters<InscripcionSurveyIdentityFacade['updateIdentityFile']>[1] {
    return { value: [{ isValid: true, file }] } as Parameters<
      InscripcionSurveyIdentityFacade['updateIdentityFile']
    >[1];
  }

  function createInitialSurvey(
    values: Partial<InscripcionInitialSurvey> = {}
  ): InscripcionInitialSurvey {
    return {
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
      ...values,
    };
  }
});
