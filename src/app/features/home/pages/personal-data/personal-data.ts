import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AsyncValidatorFn,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import {
  findCountryByIso2,
  getIso2Codes,
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtPhoneInputValue,
  ortPhoneValidator,
  OrtSelectModule,
  OrtSkeletonModule,
} from '@desarrolloort/components';
import { ComponentModeService, DatosPersonalesComponent } from '@desarrolloort/fdp-components';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map } from 'rxjs/operators';
import { isCedulaDocumentType } from 'src/app/features/auth/models/document-number';
import { Catalogs } from 'src/app/features/catalogs/services/catalogs';
import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';
import {
  matchingFieldsValidator,
  normalizeEmailValue,
} from 'src/app/shared/forms/matching-fields.validator';
import { SnackbarHandler } from 'src/app/shared/ui/snackbar/snackbar-handler';

import type { PersonalDataRecord, PhoneValidationPayload } from '../../../auth/services/account';
import { AccountService } from '../../../auth/services/account';
import { LocationCountry, LocationState } from '../../../catalogs/models/catalog.interface';

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
  phone: FormControl<OrtPhoneInputValue | null>;
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
    OrtSkeletonModule,
    ReactiveFormsModule,
    DatosPersonalesComponent,
  ],
  providers: [ComponentModeService],
  templateUrl: './personal-data.html',
  styleUrl: './personal-data.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData implements OnInit {
  private readonly account = inject(AccountService);
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
      phone: new FormControl<OrtPhoneInputValue | null>(null, {
        validators: [Validators.required, ortPhoneValidator],
        asyncValidators: [this.phoneValidator()],
        updateOn: 'blur',
      }),
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
  protected readonly identityRestricted = signal(false);
  private readonly documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });
  protected readonly isCedulaInput = computed(() => isCedulaDocumentType(this.documentTypeValue()));

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

    return buildFormErrorSummary(
      this.form,
      [
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
      ],
      ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
    );
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

    if (this.form.invalid) {
      this.snackbar.error('Completá todos los datos obligatorios con un formato válido.');
      return;
    }

    if (this.isSubmitting()) {
      return;
    }

    const value = this.form.getRawValue();
    this.isSubmitting.set(true);

    this.account
      .updatePersonalData({
        countryCode: this.toOptionalNumber(value.countryCode),
        stateCode: this.toOptionalNumber(value.stateCode),
        cityCode: this.toOptionalNumber(value.cityCode),
        address: value.address.trim(),
        phone: this.toBackendPhone(value.phone).trim(),
        email: value.email.trim(),
        emailVerification: value.emailConfirmation.trim(),
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: success => {
          if (!success) {
            this.snackbar.error('No se pudieron guardar los datos personales.');
            return;
          }

          this.snackbar.success('Datos personales actualizados.');
        },
        error: () => {
          this.snackbar.error('No se pudieron guardar los datos personales.');
        },
      });
  }

  protected cancel(): void {
    void this.router.navigateByUrl('/inicio');
  }

  private loadData(): void {
    this.isLoading.set(true);

    forkJoin({
      data: this.account.getPersonalData(),
      locations: this.catalogs.getCountryLocations(),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ data, locations }) => {
          this.locations.set(locations);
          this.patchForm(data);
        },
        error: () => {
          this.snackbar.error('No se pudieron cargar los datos personales.');
        },
      });
  }

  private patchForm(data: PersonalDataRecord): void {
    this.identityRestricted.set(data.identityRestricted);
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
        phone: this.toPhoneInputValue(data.phone),
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

  private toOptionalNumber(value: string): number | undefined {
    return value ? Number(value) : undefined;
  }

  private toPhoneInputValue(value: string): OrtPhoneInputValue | null {
    const trimmed = value.trim();

    if (!trimmed) {
      return null;
    }

    if (trimmed.startsWith('+')) {
      const digits = trimmed.replace(/\D/g, '');
      const country = this.findPhoneCountryByPrefix(digits);

      if (country) {
        const number = digits.slice(country.prefix.toString().length);
        return { iso2: country.iso2, number, numberE164: `+${digits}` };
      }
    }

    const number = trimmed.replace(/\D/g, '');
    return { iso2: 'UY', number, numberE164: `+598${number}` };
  }

  private toBackendPhone(value: OrtPhoneInputValue | null): string {
    if (!value) {
      return '';
    }

    if (value.iso2 === 'UY') {
      return value.number.trim();
    }

    return (value.numberE164 || value.number).trim();
  }

  private phoneValidator(): AsyncValidatorFn {
    return control => {
      const value = control.value as OrtPhoneInputValue | null;
      if (!value?.number.trim()) {
        return of(null);
      }

      return this.account.validatePhone(this.toPhoneValidationPayload(value)).pipe(
        map(isValid => (isValid ? null : { phone: true })),
        catchError(() => of(null))
      );
    };
  }

  private toPhoneValidationPayload(value: OrtPhoneInputValue): PhoneValidationPayload {
    const iso2 = value.iso2 || null;
    const country = this.findPhoneCountryByIso2(iso2);

    return {
      iso2,
      countryPrefix: country?.prefix ?? null,
      number: value.number.trim(),
      numberE164: value.numberE164?.trim() || null,
    };
  }

  private findPhoneCountryByIso2(iso2: string | null) {
    const code = getIso2Codes().find(item => item === iso2);
    return code ? findCountryByIso2(code) : undefined;
  }

  private findPhoneCountryByPrefix(digits: string) {
    return getIso2Codes()
      .map(iso2 => findCountryByIso2(iso2))
      .filter(country => !!country)
      .sort((a, b) => b.prefix - a.prefix)
      .find(country => digits.startsWith(country.prefix.toString()));
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
