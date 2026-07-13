import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, Validators } from '@angular/forms';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { merge } from 'rxjs';

import {
  createFamilyMember,
  type FamilyMemberGroup,
  getVisiblePersonalSections,
  type ScholarshipPersonalSectionId,
  type ScholarshipVariant,
} from '../models/scholarship-personal-forms';
import { createSectionStatus, SectionStatus } from '../models/section-status';
import { ScholarshipFormsStore } from '../store/scholarship-forms';
import { ScholarshipProcessFacade } from './scholarship-process';

/**
 * Fachada de la sección "Información personal" de la postulación a becas.
 * Equivale a `InscripcionSurveyFacade`, adaptada a que acá todas las
 * subsecciones se muestran juntas en un mismo acordeón (no un wizard paso a
 * paso): centraliza los `FormGroup` (vía `ScholarshipFormsStore`), qué
 * subsecciones aplican según `variant`, los validadores condicionales y el
 * armado/borrado de integrantes del núcleo familiar.
 */
export class ScholarshipPersonalFacade {
  private readonly formsStore = inject(ScholarshipFormsStore);
  private readonly processFacade = inject(ScholarshipProcessFacade);
  private readonly destroyRef = inject(DestroyRef);

  public readonly personalDataForm = this.formsStore.personalDataForm;
  public readonly educationInfoForm = this.formsStore.educationInfoForm;
  public readonly educationInfoFbrForm = this.formsStore.educationInfoFbrForm;
  public readonly educationInfoFclForm = this.formsStore.educationInfoFclForm;
  public readonly workHistoryForm = this.formsStore.workHistoryForm;
  public readonly declarationForm = this.formsStore.declarationForm;
  public readonly familyMembers = this.declarationForm.controls.familyMembers;

  public readonly variant = signal<ScholarshipVariant | null>(null);
  public readonly submitted = signal(false);

  public readonly visibleSections = computed<readonly ScholarshipPersonalSectionId[]>(() => {
    const variant = this.variant();
    return variant ? getVisiblePersonalSections(variant) : [];
  });

  private nextMemberId = this.familyMembers.length + 1;

  constructor() {
    this.configureConditionalValidators();
  }

  public setVariant(variant: ScholarshipVariant): void {
    this.variant.set(variant);
  }

  public isSectionVisible(section: ScholarshipPersonalSectionId): boolean {
    return this.visibleSections().includes(section);
  }

  public sectionStatus(section: ScholarshipPersonalSectionId): SectionStatus {
    return createSectionStatus(() => this.isSectionValid(section), this.submitted);
  }

  /** La sección "Información educativa" es una sola en el acordeón, pero cuál de los tres forms aplica depende de `variant`. */
  public educationSectionId(): ScholarshipPersonalSectionId {
    return (
      this.visibleSections().find(
        section =>
          section === 'educacion' || section === 'educacion-fbr' || section === 'educacion-fcl'
      ) ?? 'educacion'
    );
  }

  public addFamilyMember(): void {
    this.familyMembers.push(createFamilyMember(this.nextMemberId++));
    this.updateConditionalValidators();
  }

  public removeFamilyMember(id: number): void {
    const index = this.familyMembers.controls.findIndex(member => member.controls.id.value === id);
    if (index < 0) return;

    this.familyMembers.removeAt(index);
    this.familyMembers.updateValueAndValidity();
    this.declarationForm.updateValueAndValidity();
  }

  /**
   * Handler genérico para los controles booleanos que representan "hay un
   * archivo adjunto" (certificado de secundaria, formulario de reválidas,
   * etc.), antes copiado en cada componente hijo.
   */
  public setFileFlag(control: FormControl<boolean | null>, event: OrtFileUploaderChange): void {
    const selectedFile = event.value.find(file => file.isValid)?.file ?? null;
    control.setValue(Boolean(selectedFile));
    control.markAsTouched();
  }

  public onIncomeProofFilesChanged(memberId: number, event: OrtFileUploaderChange): void {
    const selectedFile = event.value.find(file => file.isValid)?.file ?? null;
    const member = this.familyMembers.controls.find(item => item.controls.id.value === memberId);
    member?.controls.proofAttached.setValue(Boolean(selectedFile));
  }

  public isIncomeProofFileMissing(member: FamilyMemberGroup): boolean {
    return member.controls.percibeIngresos.value === 'si' && !member.controls.proofAttached.value;
  }

  public continue(): void {
    this.submitted.set(true);
    const sections = this.visibleSections();

    if (sections.includes('datos-personales')) this.personalDataForm.markAllAsTouched();
    if (sections.includes('educacion')) this.educationInfoForm.markAllAsTouched();
    if (sections.includes('educacion-fbr')) this.educationInfoFbrForm.markAllAsTouched();
    if (sections.includes('educacion-fcl')) this.educationInfoFclForm.markAllAsTouched();
    if (sections.includes('antecedentes-laborales')) this.workHistoryForm.markAllAsTouched();
    if (sections.includes('declaracion')) {
      this.declarationForm.markAllAsTouched();
      this.familyMembers.controls.forEach(member => member.markAllAsTouched());
    }

    if (!this.isValid(sections)) return;

    this.processFacade.continue();
  }

  public back(): void {
    this.processFacade.back();
  }

  public showErrorAlert(): boolean {
    return this.submitted() && !this.isValid(this.visibleSections());
  }

  private isValid(sections: readonly ScholarshipPersonalSectionId[]): boolean {
    return sections.every(section => this.isSectionValid(section));
  }

  private isSectionValid(section: ScholarshipPersonalSectionId): boolean {
    switch (section) {
      case 'datos-personales':
        return this.personalDataForm.valid;
      case 'educacion':
        return this.educationInfoForm.valid;
      case 'educacion-fbr':
        return this.educationInfoFbrForm.valid;
      case 'educacion-fcl':
        return this.educationInfoFclForm.valid;
      case 'antecedentes-laborales':
        return this.workHistoryForm.valid;
      case 'declaracion':
        return this.isDeclarationValid();
    }
  }

  private isDeclarationValid(): boolean {
    const membersValid = this.familyMembers.controls.every((member, index) => {
      const isRelationshipValid = index === 0 || Boolean(member.controls.relationship.value);
      const isProofValid =
        member.controls.percibeIngresos.value !== 'si' ||
        Boolean(member.controls.proofAttached.value);
      return member.valid && isRelationshipValid && isProofValid;
    });

    this.declarationForm.updateValueAndValidity();
    return this.declarationForm.valid && membersValid;
  }

  private configureConditionalValidators(): void {
    merge(this.declarationForm.controls.vehicleOwn.valueChanges, this.familyMembers.valueChanges)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateConditionalValidators());
    this.updateConditionalValidators();
  }

  private updateConditionalValidators(): void {
    const declaration = this.declarationForm.controls;
    const vehicleOwned = declaration.vehicleOwn.value === 'si';
    this.setRequired(declaration.vehicleModel, vehicleOwned);
    this.setRequired(declaration.vehicleYear, vehicleOwned);
    if (!vehicleOwned) {
      declaration.vehicleModel.reset(null, { emitEvent: false });
      declaration.vehicleYear.reset(null, { emitEvent: false });
    }

    for (const member of this.familyMembers.controls) {
      const receivesIncome = member.controls.percibeIngresos.value === 'si';
      this.setRequired(member.controls.nominalIncome, receivesIncome);
      this.setRequired(member.controls.liquidIncome, receivesIncome);
      if (!receivesIncome) {
        member.controls.nominalIncome.reset(null, { emitEvent: false });
        member.controls.liquidIncome.reset(null, { emitEvent: false });
        member.controls.proofAttached.setValue(false, { emitEvent: false });
      }
    }
  }

  private setRequired(control: AbstractControl, required: boolean): void {
    control.setValidators(required ? Validators.required : null);
    control.updateValueAndValidity({ emitEvent: false });
  }
}
