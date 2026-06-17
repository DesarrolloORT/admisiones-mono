import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DoCheck,
  forwardRef,
  inject,
  Injector,
  OnInit,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  ControlValueAccessor,
  FormControl,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  NgControl,
  ReactiveFormsModule,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { OrtFormFieldModule, OrtSelectModule } from '@desarrolloort/components';

import { LocationCountry, LocationState } from '../../../catalogs/models/catalog.interface';
import { Catalogs } from '../../../catalogs/services/catalogs';
import { LocationValue } from '../../models/location-value';

@Component({
  selector: 'app-location-select',
  imports: [OrtFormFieldModule, OrtSelectModule, ReactiveFormsModule],
  templateUrl: './location-select.html',
  styleUrl: './location-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LocationSelect),
      multi: true,
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => LocationSelect),
      multi: true,
    },
  ],
})
export class LocationSelect implements ControlValueAccessor, DoCheck, OnInit, Validator {
  private readonly catalogs = inject(Catalogs);
  private readonly injector = inject(Injector);

  protected readonly countries = signal<LocationCountry[]>([]);
  protected readonly isDisabled = signal(false);

  protected readonly countryControl = new FormControl<string>('', { nonNullable: true });
  protected readonly stateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });

  protected readonly selectedCountryCode = signal<number | null>(null);
  protected readonly selectedStateCode = signal<number | null>(null);
  protected readonly selectedCityCode = signal<number | null>(null);

  private readonly statesVisible = signal(true);
  private readonly citiesVisible = signal(true);

  protected readonly states = computed(() => {
    const countryCode = this.selectedCountryCode();
    if (countryCode === null) return [];
    const country = this.countries().find(c => c.codigoPais === countryCode);
    return country?.estado ?? [];
  });

  protected readonly cities = computed(() => {
    const stateCode = this.selectedStateCode();
    if (stateCode === null) return [];
    const state = this.states().find((s: LocationState) => s.codigoEstado === stateCode);
    return state?.ciudad ?? [];
  });

  protected readonly showStates = computed(
    () => this.statesVisible() && this.selectedCountryCode() !== null && this.states().length > 0
  );
  protected readonly showCities = computed(
    () => this.citiesVisible() && this.selectedStateCode() !== null && this.cities().length > 0
  );

  private onChange: (value: LocationValue) => void = () => {};
  private onTouched: () => void = () => {};
  private onValidatorChange: () => void = () => {};

  ngOnInit(): void {
    this.loadCountries();
  }

  ngDoCheck(): void {
    this.syncFieldVisualState();
  }

  writeValue(value: LocationValue | null): void {
    if (!value) {
      this.selectedCountryCode.set(null);
      this.selectedStateCode.set(null);
      this.selectedCityCode.set(null);
      this.countryControl.setValue('');
      this.stateControl.setValue('');
      this.cityControl.setValue('');
      return;
    }
    this.selectedCountryCode.set(value.codigoPais);
    this.selectedStateCode.set(value.codigoEstado);
    this.selectedCityCode.set(value.codigoCiudad);
    this.countryControl.setValue(value.codigoPais?.toString() ?? '', { emitEvent: false });
    this.stateControl.setValue(value.codigoEstado?.toString() ?? '', { emitEvent: false });
    this.cityControl.setValue(value.codigoCiudad?.toString() ?? '', { emitEvent: false });
  }

  registerOnChange(fn: (value: LocationValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  registerOnValidatorChange(fn: () => void): void {
    this.onValidatorChange = fn;
  }

  validate(control: AbstractControl<LocationValue | null>): ValidationErrors | null {
    const value = control.value;

    if (!value?.codigoPais) {
      return { locationRequired: true };
    }

    const country = this.countries().find(item => item.codigoPais === value.codigoPais);
    const states = country?.estado ?? [];

    if (states.length > 0 && !value.codigoEstado) {
      return { locationStateRequired: true };
    }

    const state = states.find(item => item.codigoEstado === value.codigoEstado);
    const cities = state?.ciudad ?? [];

    if (cities.length > 0 && !value.codigoCiudad) {
      return { locationCityRequired: true };
    }

    return null;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
    if (isDisabled) {
      this.countryControl.disable({ emitEvent: false });
      this.stateControl.disable({ emitEvent: false });
      this.cityControl.disable({ emitEvent: false });
    } else {
      this.countryControl.enable({ emitEvent: false });
      this.stateControl.enable({ emitEvent: false });
      this.cityControl.enable({ emitEvent: false });
    }
  }

  protected onCountryChange(): void {
    const code = this.countryControl.value ? Number(this.countryControl.value) : null;
    this.selectedCountryCode.set(code);
    this.selectedStateCode.set(null);
    this.selectedCityCode.set(null);
    this.stateControl.setValue('', { emitEvent: false });
    this.cityControl.setValue('', { emitEvent: false });
    this.statesVisible.set(false);
    this.citiesVisible.set(false);
    setTimeout(() => {
      this.statesVisible.set(true);
      this.citiesVisible.set(true);
    });
    this.emitValue();
    this.onValidatorChange();
  }

  protected onStateChange(): void {
    const code = this.stateControl.value ? Number(this.stateControl.value) : null;
    this.selectedStateCode.set(code);
    this.selectedCityCode.set(null);
    this.cityControl.setValue('', { emitEvent: false });
    this.citiesVisible.set(false);
    setTimeout(() => this.citiesVisible.set(true));
    this.emitValue();
    this.onValidatorChange();
  }

  protected onCityChange(): void {
    const code = this.cityControl.value ? Number(this.cityControl.value) : null;
    this.selectedCityCode.set(code);
    this.emitValue();
    this.onValidatorChange();
  }

  protected onBlur(): void {
    this.onTouched();
  }

  private emitValue(): void {
    this.onChange({
      codigoPais: this.selectedCountryCode(),
      codigoEstado: this.selectedStateCode(),
      codigoCiudad: this.selectedCityCode(),
    });
  }

  private loadCountries(): void {
    this.catalogs.getCountryLocations().subscribe(countries => {
      this.countries.set(countries);
      const code = this.selectedCountryCode();
      if (code !== null) {
        this.countryControl.setValue(code.toString(), { emitEvent: false });
      }

      this.onValidatorChange();
    });
  }

  private syncFieldVisualState(): void {
    const controlToSync = this.injector.get(NgControl, null, { self: true, optional: true })?.control;
    if (!controlToSync) {
      return;
    }

    const touched = controlToSync.touched;
    this.syncFieldError(this.countryControl, touched && controlToSync.hasError('locationRequired'));
    this.syncFieldError(this.stateControl, touched && controlToSync.hasError('locationStateRequired'));
    this.syncFieldError(this.cityControl, touched && controlToSync.hasError('locationCityRequired'));
  }

  private syncFieldError(control: FormControl<string>, hasError: boolean): void {
    if (hasError) {
      control.markAsTouched({ onlySelf: true });
      control.setErrors({ required: true });
      return;
    }

    if (control.hasError('required')) {
      control.setErrors(null);
    }
  }
}
