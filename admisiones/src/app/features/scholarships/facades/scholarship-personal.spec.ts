import { TestBed } from '@angular/core/testing';
import type { OrtFileUploaderChange } from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from './scholarship-personal';
import { ScholarshipProcessFacade } from './scholarship-process';

function fileUploaderChange(isValid: boolean): OrtFileUploaderChange {
  return {
    value: [{ file: new File(['x'], 'certificado.pdf', { type: 'application/pdf' }), isValid }],
  } as unknown as OrtFileUploaderChange;
}

describe('ScholarshipPersonalFacade', () => {
  let facade: ScholarshipPersonalFacade;
  let process: ScholarshipProcessFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ScholarshipProcessFacade, ScholarshipPersonalFacade],
    });

    process = TestBed.inject(ScholarshipProcessFacade);
    facade = TestBed.inject(ScholarshipPersonalFacade);
  });

  it('reads the forms from the process facade so they survive changing step', () => {
    expect(facade.personalDataForm).toBe(process.personalDataForm);
    expect(facade.educationInfoForm).toBe(process.educationInfoForm);
    expect(facade.declarationForm).toBe(process.declarationForm);
  });

  it('has no visible section until the variant is known', () => {
    expect(facade.visibleSections()).toEqual([]);
    expect(facade.isSectionVisible('declaracion')).toBe(false);
  });

  it('resolves the visible sections and the education section of the variant', () => {
    facade.setVariant('fbr');

    expect(facade.visibleSections()).toEqual(['datos-personales', 'educacion-fbr', 'declaracion']);
    expect(facade.educationSectionId()).toBe('educacion-fbr');
    expect(facade.isSectionVisible('antecedentes-laborales')).toBe(false);

    facade.setVariant('fcl');

    expect(facade.educationSectionId()).toBe('educacion-fcl');
    expect(facade.isSectionVisible('antecedentes-laborales')).toBe(true);
  });

  // fbc no pide el certificado de secundaria: el validador se apaga y el flag se
  // limpia para que un cambio de variante no deje el form invalido por un archivo
  // que ya no se pide.
  it('drops the certificate requirement for fbc', () => {
    const certificate = facade.educationInfoForm.controls.certificateFile;

    facade.setVariant('fexaCon');
    expect(certificate.invalid).toBe(true);

    facade.setVariant('fbc');

    expect(certificate.valid).toBe(true);
    expect(certificate.value).toBe(false);
  });

  it('requires the vehicle details only while a vehicle is declared', () => {
    const declaration = facade.declarationForm.controls;

    declaration.vehicleOwn.setValue('si');

    expect(declaration.vehicleModel.invalid).toBe(true);
    expect(declaration.vehicleYear.invalid).toBe(true);

    declaration.vehicleModel.setValue('Gol');
    declaration.vehicleYear.setValue('2015');
    declaration.vehicleOwn.setValue('no');

    expect(declaration.vehicleModel.valid).toBe(true);
    expect(declaration.vehicleModel.value).toBeNull();
    expect(declaration.vehicleYear.value).toBeNull();
  });

  it('requires the income amounts only for members who declare income', () => {
    const member = facade.familyMembers.at(0);

    member.controls.percibeIngresos.setValue('si');

    expect(member.controls.nominalIncome.invalid).toBe(true);
    expect(member.controls.liquidIncome.invalid).toBe(true);

    member.controls.nominalIncome.setValue('30000');
    member.controls.percibeIngresos.setValue('no');

    expect(member.controls.nominalIncome.valid).toBe(true);
    expect(member.controls.nominalIncome.value).toBeNull();
    expect(member.controls.proofAttached.value).toBe(false);
  });

  it('adds and removes family members keeping ids stable', () => {
    facade.addFamilyMember();
    facade.addFamilyMember();

    expect(facade.familyMembers.controls.map(member => member.controls.id.value)).toEqual([
      1, 2, 3,
    ]);

    facade.removeFamilyMember(2);

    expect(facade.familyMembers.controls.map(member => member.controls.id.value)).toEqual([1, 3]);
  });

  it('ignores removing a member that is not there', () => {
    facade.removeFamilyMember(99);

    expect(facade.familyMembers.length).toBe(1);
  });

  it('turns a valid upload into the file flag and marks the control touched', () => {
    const control = facade.educationInfoForm.controls.certificateFile;

    facade.setFileFlag(control, fileUploaderChange(true));

    expect(control.value).toBe(true);
    expect(control.touched).toBe(true);

    facade.setFileFlag(control, fileUploaderChange(false));

    expect(control.value).toBe(false);
  });

  it('tracks the income proof per family member', () => {
    facade.addFamilyMember();
    const member = facade.familyMembers.at(1);
    member.controls.percibeIngresos.setValue('si');

    expect(facade.isIncomeProofFileMissing(member)).toBe(true);

    facade.onIncomeProofFilesChanged(2, fileUploaderChange(true));

    expect(member.controls.proofAttached.value).toBe(true);
    expect(facade.isIncomeProofFileMissing(member)).toBe(false);
  });

  it('does not advance the process while a visible section is invalid', () => {
    const continueSpy = vi.spyOn(process, 'continue');
    facade.setVariant('fexaSin');

    facade.continue();

    expect(facade.submitted()).toBe(true);
    expect(facade.showErrorAlert()).toBe(true);
    expect(facade.educationInfoForm.touched).toBe(true);
    expect(continueSpy).not.toHaveBeenCalled();
  });

  it('advances the process once every visible section is valid', () => {
    const continueSpy = vi.spyOn(process, 'continue');
    // fexaSin es la variante mas corta: solo "Informacion educativa".
    facade.setVariant('fexaSin');
    facade.educationInfoForm.controls.averageSecondYear.setValue(9);
    facade.educationInfoForm.controls.averageThirdYear.setValue(10);
    facade.educationInfoForm.controls.certificateFile.setValue(true);

    facade.continue();

    expect(continueSpy).toHaveBeenCalledOnce();
    expect(facade.showErrorAlert()).toBe(false);
  });

  // El primer integrante es el postulante y no declara vinculo; del segundo en
  // adelante el vinculo es obligatorio aunque el form no lo valide.
  it('requires the relationship from the second family member on', () => {
    const continueSpy = vi.spyOn(process, 'continue');
    facade.setVariant('fexaSin');
    facade.educationInfoForm.controls.averageSecondYear.setValue(9);
    facade.educationInfoForm.controls.averageThirdYear.setValue(10);
    facade.educationInfoForm.controls.certificateFile.setValue(true);
    facade.addFamilyMember();

    facade.continue();

    expect(continueSpy).toHaveBeenCalledOnce();

    facade.setVariant('fbc');
    facade.continue();

    expect(facade.showErrorAlert()).toBe(true);
  });

  it('delegates going back to the process facade', () => {
    const backSpy = vi.spyOn(process, 'back');

    facade.back();

    expect(backSpy).toHaveBeenCalledOnce();
  });
});
