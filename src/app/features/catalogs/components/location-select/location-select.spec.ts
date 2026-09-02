import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../api/catalogs.api';
import { LocationValue } from '../../models/location-value';
import { LocationSelect } from './location-select';

@Component({
  imports: [LocationSelect, ReactiveFormsModule],
  selector: 'app-test-host',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form [formGroup]="form">
      <app-location-select formControlName="location" [submitted]="submitted()" />
    </form>
  `,
})
class TestHostComponent {
  public readonly submitted = signal(false);
  public readonly form = new FormGroup({
    location: new FormControl<LocationValue | null>(null),
  });
}

describe('LocationSelect', () => {
  let fixture: ComponentFixture<LocationSelect>;
  let component: LocationSelect;
  let catalogsMock: { getCountryLocations: ReturnType<typeof vi.fn> };

  const mockData = {
    success: true,
    data: [
      {
        countryCode: 1,
        name: 'Uruguay',
        states: [
          {
            countryCode: 1,
            stateCode: 10,
            name: 'Montevideo',
            cities: [
              { countryCode: 1, stateCode: 10, cityCode: 100, name: 'Montevideo' },
              { countryCode: 1, stateCode: 10, cityCode: 101, name: 'Ciudad de la Costa' },
            ],
          },
          {
            countryCode: 1,
            stateCode: 11,
            name: 'Canelones',
            cities: [{ countryCode: 1, stateCode: 11, cityCode: 110, name: 'Canelones' }],
          },
        ],
      },
      {
        countryCode: 2,
        name: 'Argentina',
        states: [
          {
            countryCode: 2,
            stateCode: 20,
            name: 'Buenos Aires',
            cities: [{ countryCode: 2, stateCode: 20, cityCode: 200, name: 'CABA' }],
          },
        ],
      },
    ],
  };

  beforeEach(() => {
    catalogsMock = {
      getCountryLocations: vi.fn().mockReturnValue(of(mockData.data)),
    };

    TestBed.configureTestingModule({
      imports: [LocationSelect, ReactiveFormsModule],
      providers: [{ provide: CatalogsApi, useValue: catalogsMock }],
    });

    fixture = TestBed.createComponent(LocationSelect);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  const createHost = () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [{ provide: CatalogsApi, useValue: catalogsMock }],
    });

    const hostFixture = TestBed.createComponent(TestHostComponent);
    hostFixture.detectChanges();

    const isCountryFieldInvalid = () =>
      hostFixture.debugElement
        .query(By.css('ort-form-field'))
        .nativeElement.classList.contains('ort-form-field-invalid');

    return {
      hostFixture,
      host: hostFixture.componentInstance,
      isCountryFieldInvalid,
      countryInput: hostFixture.debugElement.query(By.css('#location-country'))
        .nativeElement as HTMLInputElement,
      locationSelect: hostFixture.debugElement.query(By.directive(LocationSelect))
        .componentInstance as LocationSelect,
    };
  };

  it('should show the option names when the location is preloaded', () => {
    const { hostFixture, host } = createHost();

    host.form.controls.location.setValue({ countryCode: 1, stateCode: 10, cityCode: 101 });
    hostFixture.detectChanges();

    const values = hostFixture.debugElement
      .queryAll(By.css('input'))
      .map(input => (input.nativeElement as HTMLInputElement).value);
    expect(values).toEqual(['Uruguay', 'Montevideo', 'Ciudad de la Costa']);
  });

  it('should not show errors while the user is still typing on the field', () => {
    const { hostFixture, countryInput, locationSelect } = createHost();

    countryInput.dispatchEvent(new Event('focus'));
    countryInput.value = 'Uru';
    countryInput.dispatchEvent(new Event('input'));
    hostFixture.detectChanges();

    const field = hostFixture.debugElement.query(By.css('ort-form-field')).nativeElement;
    expect(locationSelect['countryControl'].touched).toBe(false);
    expect(field.querySelector('ort-error')).toBeNull();
    // El campo sigue con foco: location-select.scss neutraliza el rojo en ese estado.
    expect(field.classList.contains('ort-form-field-focused')).toBe(true);
  });

  it('should not flag a field the user has not visited yet', () => {
    const { hostFixture, host, isCountryFieldInvalid } = createHost();

    // El blur de cualquier campo marca todo el control compuesto como touched.
    host.form.controls.location.markAsTouched();
    hostFixture.detectChanges();

    expect(isCountryFieldInvalid()).toBe(false);
  });

  it('should flag the country field once it lost focus without a selection', () => {
    const { hostFixture, countryInput, isCountryFieldInvalid, locationSelect } = createHost();

    countryInput.dispatchEvent(new Event('focus'));
    countryInput.dispatchEvent(new Event('blur'));
    hostFixture.detectChanges();

    expect(locationSelect['countryControl'].touched).toBe(true);
    expect(isCountryFieldInvalid()).toBe(true);
  });

  it('should flag pending fields once the form was submitted', () => {
    const { hostFixture, host, isCountryFieldInvalid, locationSelect } = createHost();

    host.submitted.set(true);
    hostFixture.detectChanges();

    expect(locationSelect['countryControl'].touched).toBe(true);
    expect(isCountryFieldInvalid()).toBe(true);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load countries on init', () => {
    expect(catalogsMock.getCountryLocations).toHaveBeenCalled();
    expect(component['countries']().length).toBe(2);
  });

  it('should show states when a country is selected', () => {
    component['countryControl'].setValue(1);

    expect(component['selectedCountryCode']()).toBe(1);
    expect(component['states']().length).toBe(2);
    expect(component['showStates']()).toBe(true);
    expect(component['selectedStateCode']()).toBeNull();
    expect(component['showCities']()).toBe(false);
  });

  it('should show cities when a state is selected', () => {
    component['countryControl'].setValue(1);
    component['stateControl'].setValue(10);

    expect(component['selectedStateCode']()).toBe(10);
    expect(component['cities']().length).toBe(2);
    expect(component['showCities']()).toBe(true);
  });

  it('should reset state and city when country changes', () => {
    component['countryControl'].setValue(1);
    component['stateControl'].setValue(10);
    component['cityControl'].setValue(100);

    component['countryControl'].setValue(2);

    expect(component['selectedStateCode']()).toBeNull();
    expect(component['selectedCityCode']()).toBeNull();
    expect(component['stateControl'].value).toBeNull();
    expect(component['cityControl'].value).toBeNull();
    expect(component['showCities']()).toBe(false);
  });

  it('should reset city when state changes', () => {
    component['countryControl'].setValue(1);
    component['stateControl'].setValue(10);
    component['cityControl'].setValue(100);

    component['stateControl'].setValue(11);

    expect(component['selectedCityCode']()).toBeNull();
    expect(component['cities']().length).toBe(1); // Canelones has 1 city
  });

  it('should reset the cascade when the country is cleared from the search input', () => {
    component['countryControl'].setValue(1);
    component['stateControl'].setValue(10);
    component['cityControl'].setValue(100);

    component['countryControl'].setValue(null);

    expect(component['selectedCountryCode']()).toBeNull();
    expect(component['showStates']()).toBe(false);
    expect(component['showCities']()).toBe(false);
  });

  it('should emit composite value via CVA onChange', () => {
    const onChangeSpy = vi.fn();
    component.registerOnChange(onChangeSpy);

    component['countryControl'].setValue(1);
    component['stateControl'].setValue(10);
    component['cityControl'].setValue(100);

    expect(onChangeSpy).toHaveBeenLastCalledWith({
      countryCode: 1,
      stateCode: 10,
      cityCode: 100,
    });
  });

  it('should write value from parent form without emitting the cascade', () => {
    const onChangeSpy = vi.fn();
    component.registerOnChange(onChangeSpy);

    component.writeValue({ countryCode: 2, stateCode: 20, cityCode: 200 });

    expect(component['selectedCountryCode']()).toBe(2);
    expect(component['selectedStateCode']()).toBe(20);
    expect(component['selectedCityCode']()).toBe(200);
    expect(component['cityControl'].value).toBe(200);
    expect(onChangeSpy).not.toHaveBeenCalled();
  });

  it('should handle null writeValue', () => {
    component.writeValue(null);

    expect(component['selectedCountryCode']()).toBeNull();
    expect(component['selectedStateCode']()).toBeNull();
    expect(component['selectedCityCode']()).toBeNull();
  });
});
