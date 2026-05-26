import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { Catalogs } from 'src/app/features/catalogs/services/catalogs';
import { buildFormErrorSummary } from 'src/app/shared/forms/form-error-summary';
import {
  matchingFieldsValidator,
  normalizeEmailValue,
} from 'src/app/shared/forms/matching-fields.validator';
import { SnackbarHandler } from 'src/app/shared/ui/snackbar/snackbar-handler';

import { LocationCountry, LocationState } from '../../../catalogs/models/catalog.interface';
import { PersonalDataRecord, PersonalDataService } from '../../services/personal-data';

interface PersonalDataForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
  firstName: FormControl<string>;
  secondName: FormControl<string>;
  firstLastName: FormControl<string>;
  secondLastName: FormControl<string>;
  birthDate: FormControl<string>;
  sex: FormControl<string>;
  countryCode: FormControl<string>;
  stateCode: FormControl<string>;
  cityCode: FormControl<string>;
  address: FormControl<string>;
  phone: FormControl<string>;
  email: FormControl<string>;
  emailConfirmation: FormControl<string>;
}

@Component({
  selector: 'app-personal-data',
  imports: [
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './personal-data.html',
  styleUrl: './personal-data.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData implements OnInit {
  private readonly personalData = inject(PersonalDataService);
  private readonly catalogs = inject(Catalogs);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  protected readonly form = new FormGroup<PersonalDataForm>(
    {
      documentType: new FormControl('', { nonNullable: true }),
      documentNumber: new FormControl('', { nonNullable: true }),
      firstName: new FormControl('', { nonNullable: true }),
      secondName: new FormControl('', { nonNullable: true }),
      firstLastName: new FormControl('', { nonNullable: true }),
      secondLastName: new FormControl('', { nonNullable: true }),
      birthDate: new FormControl('', { nonNullable: true }),
      sex: new FormControl('', { nonNullable: true }),
      countryCode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      stateCode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      cityCode: new FormControl('', { nonNullable: true }),
      address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      phone: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
      }),
      emailConfirmation: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
      }),
    },
    {
      validators: [
        matchingFieldsValidator('email', 'emailConfirmation', {
          errorKey: 'emailMismatch',
          normalize: normalizeEmailValue,
        }),
      ],
    }
  );

  protected readonly locations = signal<LocationCountry[]>([]);
  protected readonly selectedCountryCode = signal<number | null>(null);
  protected readonly selectedStateCode = signal<number | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSubmitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly states = computed<LocationState[]>(() => {
    const countryCode = this.selectedCountryCode();

    if (countryCode === null) {
      return [];
    }

    return this.locations().find(country => country.codigoPais === countryCode)?.estado ?? [];
  });
  protected readonly errorSummary = computed(() => {
    if (!this.submitted()) {
      return [];
    }

    return buildFormErrorSummary(this.form, [
      { controlName: 'countryCode', fieldId: 'profile-country', label: 'País de residencia' },
      { controlName: 'stateCode', fieldId: 'profile-state', label: 'Departamento' },
      { controlName: 'address', fieldId: 'profile-address', label: 'Dirección' },
      { controlName: 'phone', fieldId: 'profile-phone', label: 'Celular' },
      { controlName: 'email', fieldId: 'profile-email', label: 'E-mail' },
      {
        controlName: 'emailConfirmation',
        fieldId: 'profile-email-confirmation',
        label: 'Confirmar e-mail',
      },
    ]);
  });

  ngOnInit(): void {
    this.loadData();
  }

  protected onCountryChange(): void {
    this.selectedCountryCode.set(this.toNullableNumber(this.form.controls.countryCode.value));
    this.selectedStateCode.set(null);
    this.form.controls.stateCode.setValue('');
    this.form.controls.cityCode.setValue('');
  }

  protected onStateChange(): void {
    this.selectedStateCode.set(this.toNullableNumber(this.form.controls.stateCode.value));
    this.form.controls.cityCode.setValue('');
  }

  protected submit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();
    this.form.updateValueAndValidity();
    this.successMessage.set(null);
    this.error.set(null);

    if (this.form.invalid) {
      this.error.set('Completá todos los datos obligatorios con un formato válido.');
      return;
    }

    if (this.isSubmitting()) {
      return;
    }

    const value = this.form.getRawValue();
    this.isSubmitting.set(true);

    this.personalData
      .updatePersonalData({
        countryCode: this.toNullableNumber(value.countryCode),
        stateCode: this.toNullableNumber(value.stateCode),
        cityCode: this.toNullableNumber(value.cityCode),
        address: value.address,
        phone: value.phone,
        email: value.email,
        emailVerification: value.emailConfirmation,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: success => {
          if (!success) {
            this.error.set('No se pudieron guardar los datos personales.');
            return;
          }

          this.successMessage.set('Datos personales actualizados.');
          this.snackbar.success('Datos personales actualizados.');
        },
        error: () => {
          this.error.set('No se pudieron guardar los datos personales.');
        },
      });
  }

  protected cancel(): void {
    void this.router.navigateByUrl('/inicio');
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.error.set(null);

    forkJoin({
      data: this.personalData.getPersonalData(),
      locations: this.catalogs.getCountryLocations(),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ data, locations }) => {
          this.locations.set(locations);
          this.patchForm(data);
        },
        error: () => {
          this.error.set('No se pudieron cargar los datos personales.');
        },
      });
  }

  private patchForm(data: PersonalDataRecord): void {
    this.selectedCountryCode.set(data.countryCode);
    this.selectedStateCode.set(data.stateCode);

    this.form.patchValue(
      {
        documentType: this.formatDocumentType(data.documentType),
        documentNumber: this.formatDocumentNumber(data.documentType, data.documentNumber),
        firstName: data.firstName,
        secondName: data.secondName,
        firstLastName: data.firstLastName,
        secondLastName: data.secondLastName,
        birthDate: this.formatDate(data.birthDate),
        sex: this.formatSex(data.sex),
        countryCode: this.toControlValue(data.countryCode),
        stateCode: this.toControlValue(data.stateCode),
        cityCode: this.toControlValue(data.cityCode),
        address: data.address,
        phone: data.phone,
        email: data.email,
        emailConfirmation: data.emailVerification,
      },
      { emitEvent: false }
    );
  }

  private toControlValue(value: number | null): string {
    return value === null ? '' : value.toString();
  }

  private toNullableNumber(value: string): number | null {
    return value ? Number(value) : null;
  }

  private formatDocumentType(value: string): string {
    const labels: Record<string, string> = {
      CI: 'Cédula de identidad',
      DE: 'Documento extranjero',
      PS: 'Pasaporte',
    };

    return labels[value] ?? value;
  }

  private formatDocumentNumber(documentType: string, value: string): string {
    if (documentType !== 'CI') {
      return value;
    }

    const cleaned = value.replace(/\D/g, '');

    if (cleaned.length < 2) {
      return value;
    }

    const number = cleaned.slice(0, -1);
    const verifier = cleaned.slice(-1);
    const groups: string[] = [];

    for (let index = number.length; index > 0; index -= 3) {
      groups.unshift(number.slice(Math.max(0, index - 3), index));
    }

    return `${groups.join('.')}-${verifier}`;
  }

  private formatDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);

    if (!match) {
      return value;
    }

    return `${match[3]}/${match[2]}/${match[1]}`;
  }

  private formatSex(value: string): string {
    const labels: Record<string, string> = {
      F: 'Femenino',
      M: 'Masculino',
    };

    return labels[value] ?? value;
  }
}
