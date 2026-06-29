import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import type { FormControl, FormGroup } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButton,
  OrtDrawer,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
  OrtSelectModule,
  OrtSpinnerModule,
} from '@desarrolloort/components';

import type { AcademicProposalForm, AcademicProposalOption } from '../../models/academic-proposal';
import { AcademicProposalSelection } from '../../services/academic-proposal-selection';

type MobileSelectField = 'career' | 'start' | 'shift';

@Component({
  selector: 'app-academic-proposal-select',
  imports: [
    OrtButton,
    OrtDrawer,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtRadioModule,
    OrtSelectModule,
    OrtSpinnerModule,
    ReactiveFormsModule,
  ],
  providers: [AcademicProposalSelection],
  templateUrl: './academic-proposal-select.html',
  styleUrl: './academic-proposal-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcademicProposalSelect implements OnInit {
  public readonly form = input.required<FormGroup<AcademicProposalForm>>();
  public readonly selection = input(inject(AcademicProposalSelection));

  protected readonly mobileDrawerField = signal<MobileSelectField | null>(null);
  protected readonly mobilePendingValue = signal('');
  protected readonly mobileSearch = signal('');

  ngOnInit(): void {
    this.selection().connect(this.form());
  }

  protected proposalTypeErrorId(): string | null {
    const control = this.form().controls.tipoPropuesta;
    return control.touched && control.hasError('required') ? 'academic-proposal-type-error' : null;
  }

  protected proposalTypeInvalid(): boolean {
    return this.proposalTypeErrorId() !== null;
  }

  protected proposalTypeOptionDescription(optionValue: string): string {
    return ['academic-proposal-type-hint-' + optionValue, this.proposalTypeErrorId()]
      .filter(Boolean)
      .join(' ');
  }

  protected openMobileDrawer(field: MobileSelectField): void {
    if (this.mobileFieldDisabled(field)) return;

    this.mobileDrawerField.set(field);
    this.mobilePendingValue.set(this.mobileControl(field).value);
    this.mobileSearch.set('');
  }

  protected onMobileDrawerOpenChange(open: boolean): void {
    if (!open) {
      this.closeMobileDrawer(true);
    }
  }

  protected onMobileDrawerClosed(): void {
    this.closeMobileDrawer(true);
  }

  protected selectMobileOption(value: string): void {
    this.mobilePendingValue.set(value);
  }

  protected confirmMobileSelection(): void {
    const field = this.mobileDrawerField();
    if (!field || !this.mobilePendingValue()) return;

    const control = this.mobileControl(field);
    control.setValue(this.mobilePendingValue());
    control.markAsTouched();
    this.closeMobileDrawer();
  }

  protected onMobileSearchInput(event: Event): void {
    this.mobileSearch.set((event.target as HTMLInputElement).value);
  }

  protected mobileFieldDisabled(field: MobileSelectField): boolean {
    switch (field) {
      case 'career':
        return !this.selection().canSelectCareer();
      case 'start':
        return !this.selection().canSelectStart();
      case 'shift':
        return !this.selection().canSelectShift();
    }
  }

  protected mobileFieldInvalid(field: MobileSelectField): boolean {
    const control = this.mobileControl(field);
    return control.touched && control.hasError('required');
  }

  protected mobileFieldErrorId(field: MobileSelectField): string {
    return `academic-proposal-${field}-mobile-error`;
  }

  protected mobileFieldErrorMessage(field: MobileSelectField): string {
    switch (field) {
      case 'career':
        return 'Seleccioná una carrera';
      case 'start':
        return 'Seleccioná un comienzo';
      case 'shift':
        return 'Seleccioná un turno';
    }
  }

  protected mobileFieldLabel(field: MobileSelectField | null = this.mobileDrawerField()): string {
    switch (field) {
      case 'career':
        return 'Carrera';
      case 'start':
        return 'Comienzo';
      case 'shift':
        return 'Turno';
      default:
        return 'opción';
    }
  }

  protected mobileFieldDescribedBy(field: MobileSelectField): string | null {
    return this.mobileFieldInvalid(field) ? this.mobileFieldErrorId(field) : null;
  }

  protected mobileSelectedLabel(field: MobileSelectField): string {
    const value = this.mobileControl(field).value;
    return (
      this.mobileOptions(field).find(option => option.value === value)?.label ?? 'Seleccioná...'
    );
  }

  protected filteredMobileOptions(): readonly AcademicProposalOption[] {
    const field = this.mobileDrawerField();
    if (!field) return [];

    const query = this.mobileSearch().trim().toLocaleLowerCase('es-UY');
    const options = this.mobileOptions(field);
    return query
      ? options.filter(option => option.label.toLocaleLowerCase('es-UY').includes(query))
      : options;
  }

  private closeMobileDrawer(markTouched = false): void {
    const field = this.mobileDrawerField();
    if (field && markTouched) {
      this.mobileControl(field).markAsTouched();
    }

    this.mobileDrawerField.set(null);
    this.mobileSearch.set('');
  }

  private mobileControl(field: MobileSelectField): FormControl<string> {
    switch (field) {
      case 'career':
        return this.form().controls.carrera;
      case 'start':
        return this.form().controls.comienzo;
      case 'shift':
        return this.form().controls.turno;
    }
  }

  private mobileOptions(field: MobileSelectField): readonly AcademicProposalOption[] {
    switch (field) {
      case 'career':
        return this.selection().careerOptions();
      case 'start':
        return this.selection().startOptions();
      case 'shift':
        return this.selection().shiftOptions();
    }
  }
}
