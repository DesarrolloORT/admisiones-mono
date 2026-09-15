import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  forwardRef,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  ControlValueAccessor,
  FormControl,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  ReactiveFormsModule,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { OrtFormFieldModule, OrtSearchableSelectModule } from '@desarrolloort/components';

import { CatalogsApi } from '../../api/catalogs.api';
import { LocationCountry, LocationState } from '../../models/catalog.interface';
import { LocationValue } from '../../models/location-value';

@Component({
  selector: 'app-location-select',
  imports: [OrtFormFieldModule, OrtSearchableSelectModule, ReactiveFormsModule],
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
export class LocationSelect implements ControlValueAccessor, OnInit, Validator {
  private readonly catalogs = inject(CatalogsApi);

  /** Marca los campos sin completar cuando el formulario ya se intentó enviar. */
  public readonly submitted = input(false);

  protected readonly countries = signal<LocationCountry[]>([]);
  protected readonly isDisabled = signal(false);

  protected readonly countryControl = new FormControl<number | null>(null);
  protected readonly stateControl = new FormControl<number | null>(null);
  protected readonly cityControl = new FormControl<number | null>(null);

  protected readonly selectedCountryCode = signal<number | null>(null);
  protected readonly selectedStateCode = signal<number | null>(null);
  protected readonly selectedCityCode = signal<number | null>(null);

  protected readonly states = computed(() => {
    const countryCode = this.selectedCountryCode();
    if (countryCode === null) return [];
    const country = this.countries().find(c => c.countryCode === countryCode);
    return country?.states ?? [];
  });

  protected readonly cities = computed(() => {
    const stateCode = this.selectedStateCode();
    if (stateCode === null) return [];
    const state = this.states().find((s: LocationState) => s.stateCode === stateCode);
    return state?.cities ?? [];
  });

  protected readonly showStates = computed(() => this.states().length > 0);
  protected readonly showCities = computed(() => this.cities().length > 0);

  private onChange: (value: LocationValue) => void = () => {};
  private onTouched: () => void = () => {};
  private onValidatorChange: () => void = () => {};

  constructor() {
    // El searchable select también confirma valores al escribir o al limpiar el texto,
    // no solo con selectionChange, por eso la cascada escucha el control. Los handlers
    // ignoran emisiones sin cambio real: revalidar el control reemite valueChanges.
    this.countryControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.onCountryChange());
    this.stateControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.onStateChange());
    this.cityControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.onCityChange());

    // Cada campo se marca en rojo por sí solo al perder el foco sin completar
    // (validador required + touched del control). Al enviar el formulario hay que
    // marcarlos aunque el usuario nunca los haya visitado.
    effect(() => {
      if (!this.submitted()) return;
      this.countryControl.markAsTouched();
      this.stateControl.markAsTouched();
      this.cityControl.markAsTouched();
    });
  }

  ngOnInit(): void {
    this.loadCountries();
  }

  writeValue(value: LocationValue | null): void {
    this.selectedCountryCode.set(value?.countryCode ?? null);
    this.selectedStateCode.set(value?.stateCode ?? null);
    this.selectedCityCode.set(value?.cityCode ?? null);
    this.countryControl.setValue(value?.countryCode ?? null, { emitEvent: false });
    this.stateControl.setValue(value?.stateCode ?? null, { emitEvent: false });
    this.cityControl.setValue(value?.cityCode ?? null, { emitEvent: false });
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

    if (!value?.countryCode) {
      return { locationRequired: true };
    }

    const country = this.countries().find(item => item.countryCode === value.countryCode);
    const states = country?.states ?? [];

    if (states.length > 0 && !value.stateCode) {
      return { locationStateRequired: true };
    }

    const state = states.find(item => item.stateCode === value.stateCode);
    const cities = state?.cities ?? [];

    if (cities.length > 0 && !value.cityCode) {
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

  protected onBlur(): void {
    this.onTouched();
  }

  // El valor puede llegar antes que las opciones (precarga por documento): el select
  // necesita estas funciones para mostrar el nombre en lugar del código.
  protected readonly countryName = (code: number): string =>
    this.countries().find(item => item.countryCode === code)?.name ?? '';

  protected readonly stateName = (code: number): string =>
    this.states().find(item => item.stateCode === code)?.name ?? '';

  protected readonly cityName = (code: number): string =>
    this.cities().find(item => item.cityCode === code)?.name ?? '';

  protected fieldError(control: FormControl<number | null>): string {
    return control.hasError('unresolvedOption')
      ? 'Seleccioná una opción de la lista'
      : 'Este campo es obligatorio';
  }

  private onCountryChange(): void {
    const code = this.countryControl.value;
    if (code === this.selectedCountryCode()) return;
    this.selectedCountryCode.set(code);
    this.selectedStateCode.set(null);
    this.selectedCityCode.set(null);
    this.stateControl.setValue(null, { emitEvent: false });
    this.cityControl.setValue(null, { emitEvent: false });
    this.emitValue();
    this.onValidatorChange();
  }

  private onStateChange(): void {
    const code = this.stateControl.value;
    if (code === this.selectedStateCode()) return;
    this.selectedStateCode.set(code);
    this.selectedCityCode.set(null);
    this.cityControl.setValue(null, { emitEvent: false });
    this.emitValue();
    this.onValidatorChange();
  }

  private onCityChange(): void {
    const code = this.cityControl.value;
    if (code === this.selectedCityCode()) return;
    this.selectedCityCode.set(code);
    this.emitValue();
    this.onValidatorChange();
  }

  private emitValue(): void {
    this.onChange({
      countryCode: this.selectedCountryCode(),
      stateCode: this.selectedStateCode(),
      cityCode: this.selectedCityCode(),
    });
  }

  private loadCountries(): void {
    this.catalogs.getCountryLocations().subscribe(countries => {
      this.countries.set(countries);
      const code = this.selectedCountryCode();
      if (code !== null) {
        // Reaplica el valor para que el select resuelva el nombre ahora que existen las opciones.
        this.countryControl.setValue(code, { emitEvent: false });
      }

      this.onValidatorChange();
    });
  }
}
