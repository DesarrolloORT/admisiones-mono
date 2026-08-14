import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../services/catalogs';
import { LocationSelect } from './location-select';

@Component({
  imports: [LocationSelect, ReactiveFormsModule],
  selector: 'app-test-host',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form [formGroup]="form"><app-location-select formControlName="location" /></form>
  `,
})
class TestHostComponent {
  public readonly form = new FormGroup({
    location: new FormControl(null),
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
      providers: [{ provide: Catalogs, useValue: catalogsMock }],
    });

    fixture = TestBed.createComponent(LocationSelect);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should mirror parent touched invalid state into the country control', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [{ provide: Catalogs, useValue: catalogsMock }],
    });

    const hostFixture = TestBed.createComponent(TestHostComponent);
    hostFixture.detectChanges();

    const hostComponent = hostFixture.componentInstance;
    const locationControl = hostComponent.form.controls.location;
    const locationSelect = hostFixture.debugElement.query(By.directive(LocationSelect))
      .componentInstance as LocationSelect;

    locationControl.markAsTouched();
    locationControl.setErrors({ locationRequired: true });
    hostFixture.detectChanges();

    expect(locationSelect['countryControl'].touched).toBe(true);
    expect(locationSelect['countryControl'].hasError('required')).toBe(true);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load countries on init', () => {
    expect(catalogsMock.getCountryLocations).toHaveBeenCalled();
    expect(component['countries']().length).toBe(2);
  });

  it('should show states when a country is selected', () => {
    component['countryControl'].setValue('1');
    component['onCountryChange']();

    expect(component['selectedCountryCode']()).toBe(1);
    expect(component['states']().length).toBe(2);
    expect(component['selectedStateCode']()).toBeNull();
  });

  it('should show cities when a state is selected', () => {
    component['countryControl'].setValue('1');
    component['onCountryChange']();
    component['stateControl'].setValue('10');
    component['onStateChange']();

    expect(component['selectedStateCode']()).toBe(10);
    expect(component['cities']().length).toBe(2);
  });

  it('should reset state and city when country changes', () => {
    component['countryControl'].setValue('1');
    component['onCountryChange']();
    component['stateControl'].setValue('10');
    component['onStateChange']();
    component['cityControl'].setValue('100');
    component['onCityChange']();

    component['countryControl'].setValue('2');
    component['onCountryChange']();

    expect(component['selectedStateCode']()).toBeNull();
    expect(component['selectedCityCode']()).toBeNull();
  });

  it('should reset city when state changes', () => {
    component['countryControl'].setValue('1');
    component['onCountryChange']();
    component['stateControl'].setValue('10');
    component['onStateChange']();
    component['cityControl'].setValue('100');
    component['onCityChange']();

    component['stateControl'].setValue('11');
    component['onStateChange']();

    expect(component['selectedCityCode']()).toBeNull();
  });

  it('should emit composite value via CVA onChange', () => {
    const onChangeSpy = vi.fn();
    component.registerOnChange(onChangeSpy);

    component['countryControl'].setValue('1');
    component['onCountryChange']();
    component['stateControl'].setValue('10');
    component['onStateChange']();
    component['cityControl'].setValue('100');
    component['onCityChange']();

    expect(onChangeSpy).toHaveBeenLastCalledWith({
      countryCode: 1,
      stateCode: 10,
      cityCode: 100,
    });
  });

  it('should write value from parent form', () => {
    component.writeValue({ countryCode: 2, stateCode: 20, cityCode: 200 });

    expect(component['selectedCountryCode']()).toBe(2);
    expect(component['selectedStateCode']()).toBe(20);
    expect(component['selectedCityCode']()).toBe(200);
  });

  it('should handle null writeValue', () => {
    component.writeValue(null);

    expect(component['selectedCountryCode']()).toBeNull();
    expect(component['selectedStateCode']()).toBeNull();
    expect(component['selectedCityCode']()).toBeNull();
  });

  describe('visibility toggling on parent change', () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should hide states and cities when country changes, then re-show', () => {
      component['countryControl'].setValue('1');
      component['onCountryChange']();
      vi.runAllTimers();

      component['stateControl'].setValue('10');
      component['onStateChange']();
      vi.runAllTimers();

      component['cityControl'].setValue('100');
      component['onCityChange']();

      // Change country — states and cities should hide immediately
      component['countryControl'].setValue('2');
      component['onCountryChange']();

      expect(component['showStates']()).toBe(false);
      expect(component['showCities']()).toBe(false);

      // After tick, states re-appear for new country
      vi.runAllTimers();
      expect(component['showStates']()).toBe(true);
      expect(component['showCities']()).toBe(false); // no state selected yet
    });

    it('should hide cities when state changes, then re-show', () => {
      component['countryControl'].setValue('1');
      component['onCountryChange']();
      vi.runAllTimers();

      component['stateControl'].setValue('10');
      component['onStateChange']();
      vi.runAllTimers();

      component['cityControl'].setValue('100');
      component['onCityChange']();

      // Change state — cities should hide immediately
      component['stateControl'].setValue('11');
      component['onStateChange']();

      expect(component['showCities']()).toBe(false);

      // After tick, cities re-appear for new state
      vi.runAllTimers();
      expect(component['showCities']()).toBe(true);
      expect(component['cities']().length).toBe(1); // Canelones has 1 city
    });
  });
});
