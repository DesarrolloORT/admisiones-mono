import {
  ChangeDetectionStrategy,
  Component,
  computed,
  CUSTOM_ELEMENTS_SCHEMA,
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
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtPhoneInputValue,
  ortPhoneValidator,
  OrtSelectModule,
  OrtSkeletonModule,
} from '@desarrolloort/components';
import { forkJoin, of } from 'rxjs';
import { catchError, filter, finalize, map, take } from 'rxjs/operators';
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
import {
  toBackendPhone,
  toPhoneInputValue,
  toPhoneValidationValue,
} from 'src/app/shared/forms/phone';
import { SnackbarHandler } from 'src/app/shared/ui/snackbar/snackbar-handler';

import type { PersonalDataRecord } from '../../../auth/services/account';
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
  ],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
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

    return this.locations().find(country => country.countryCode === countryCode)?.states ?? [];
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

    // El telefono es el unico control validado contra el servidor: mientras esa validacion
    // esta pendiente el form no es `invalid` y el guardado se saltearia el chequeo.
    const phone = this.form.controls.phone;

    if (phone.pending) {
      phone.statusChanges
        .pipe(
          filter(status => status !== 'PENDING'),
          take(1)
        )
        .subscribe(() => this.save());

      // La validacion en curso pudo dispararse sin emitEvent (el control es updateOn:
      // 'blur'), asi que se relanza para garantizar la notificacion.
      phone.updateValueAndValidity();
      return;
    }

    this.save();
  }

  private save(): void {
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
        phone: toBackendPhone(value.phone),
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
    this.router.navigateByUrl('/inicio');
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
        phone: toPhoneInputValue(data.phone),
        email: data.email,
        emailConfirmation: data.emailVerification,
      },
      { emitEvent: false }
    );

    this.validateLoadedPhone();
  }

  /**
   * Los telefonos previos a la migracion no traen pais y se cargan asumiendo Uruguay: se
   * valida contra el servidor para que un numero extranjero no quede re-etiquetado en
   * silencio. Solo se marca `touched` si falla, para no abrir el formulario en rojo.
   */
  private validateLoadedPhone(): void {
    const phone = this.form.controls.phone;

    // La suscripcion va antes: si el validador resuelve de forma sincronica el cambio de
    // estado se emite dentro de updateValueAndValidity.
    phone.statusChanges
      .pipe(
        filter(status => status !== 'PENDING'),
        take(1)
      )
      .subscribe(status => {
        if (status === 'INVALID') {
          phone.markAsTouched();
        }
      });
    phone.updateValueAndValidity();
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

  private phoneValidator(): AsyncValidatorFn {
    return control => {
      const value = control.value as OrtPhoneInputValue | null;
      if (!value?.number.trim()) {
        return of(null);
      }

      return this.account.validatePhone(toPhoneValidationValue(value)).pipe(
        map(isValid => (isValid ? null : { phone: true })),
        catchError(() => of(null))
      );
    };
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

    const cleaned = value.replaceAll(/\D/g, '');

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
