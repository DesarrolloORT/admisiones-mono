import { Validators } from '@angular/forms';

import {
  createFamilyMember,
  createScholarshipPersonalForms,
  getVisiblePersonalSections,
  type ScholarshipVariant,
} from './scholarship-personal-forms';

describe('createScholarshipPersonalForms', () => {
  it('creates the six forms of the personal-information accordion', () => {
    const forms = createScholarshipPersonalForms();

    expect(Object.keys(forms)).toEqual([
      'personalDataForm',
      'educationInfoForm',
      'educationInfoFbrForm',
      'educationInfoFclForm',
      'workHistoryForm',
      'declarationForm',
    ]);
    for (const form of Object.values(forms)) {
      expect(form.invalid).toBe(true);
    }
  });

  it('starts the declaration with one family member', () => {
    const { declarationForm } = createScholarshipPersonalForms();

    expect(declarationForm.controls.familyMembers.length).toBe(1);
    expect(declarationForm.controls.familyMembers.at(0).controls.id.value).toBe(1);
  });

  it('requires attaching the secondary-school certificate', () => {
    const control = createScholarshipPersonalForms().educationInfoForm.controls.certificateFile;

    expect(control.value).toBe(false);
    expect(control.invalid).toBe(true);

    control.setValue(true);

    expect(control.valid).toBe(true);
  });

  it('leaves the vehicle and expense details optional', () => {
    const { declarationForm } = createScholarshipPersonalForms();

    for (const control of [
      declarationForm.controls.vehicleModel,
      declarationForm.controls.vehicleYear,
      declarationForm.controls.savingsAmount,
      declarationForm.controls.observations,
    ]) {
      expect(control.valid).toBe(true);
    }
  });
});

describe('createFamilyMember', () => {
  it('requires name, age and whether the member has income', () => {
    const member = createFamilyMember(3);

    expect(member.controls.id.value).toBe(3);
    expect(member.controls.name.hasError('required')).toBe(true);
    expect(member.controls.age.hasError('required')).toBe(true);
    expect(member.controls.percibeIngresos.hasError('required')).toBe(true);
  });

  // La relacion la pide la fachada solo desde el segundo integrante (el primero es
  // el propio postulante), asi que el form no la valida.
  it('does not require the relationship at the form level', () => {
    const member = createFamilyMember(1);

    expect(member.controls.relationship.hasValidator(Validators.required)).toBe(false);
    expect(member.controls.proofAttached.value).toBe(false);
  });

  it('leaves the income amounts optional until the facade turns them on', () => {
    const member = createFamilyMember(1);

    expect(member.controls.nominalIncome.valid).toBe(true);
    expect(member.controls.liquidIncome.valid).toBe(true);
  });
});

describe('getVisiblePersonalSections', () => {
  const cases: Array<[ScholarshipVariant, string[]]> = [
    ['fbr', ['datos-personales', 'educacion-fbr', 'declaracion']],
    ['fbc', ['datos-personales', 'educacion', 'declaracion']],
    ['fexaCon', ['datos-personales', 'educacion', 'declaracion']],
    ['fcl', ['educacion-fcl', 'antecedentes-laborales', 'declaracion']],
    ['fexaSin', ['educacion']],
  ];

  for (const [variant, sections] of cases) {
    it(`resolves the sections of ${variant} in accordion order`, () => {
      expect(getVisiblePersonalSections(variant)).toEqual(sections);
    });
  }

  it('shows exactly one education section per variant', () => {
    const educationSections = ['educacion', 'educacion-fbr', 'educacion-fcl'];

    for (const [variant] of cases) {
      const matches = getVisiblePersonalSections(variant).filter(section =>
        educationSections.includes(section)
      );

      expect(matches).toHaveLength(1);
    }
  });
});
