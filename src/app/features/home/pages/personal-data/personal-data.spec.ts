import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import type { OrtPhoneInputValue } from '@desarrolloort/components';
import { NEVER, of, Subject } from 'rxjs';
import { AccountService } from 'src/app/features/auth/services/account';
import { Catalogs } from 'src/app/features/catalogs/services/catalogs';
import { SnackbarHandler } from 'src/app/shared/ui/snackbar/snackbar-handler';

import { PersonalData } from './personal-data';

interface TestPersonalDataForm {
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

type TestPersonalDataComponent = PersonalData & {
  form: FormGroup<TestPersonalDataForm>;
  submit: () => void;
};

const basePersonalData = {
  documentType: 'CI',
  documentNumber: '4123456-9',
  firstName: 'Gabriela',
  secondName: '',
  firstLastName: 'Ortiz',
  secondLastName: 'Morales',
  birthDate: '1988-05-31',
  sex: 'F',
  countryCode: 1,
  stateCode: 10,
  cityCode: 100,
  address: 'Av. 18 de Julio 1360',
  phone: { nationalNumber: '99123456', iso2: 'UY', e164: '+59899123456', isValid: true },
  email: 'gabrielaortiz@gmail.com',
  emailVerification: 'gabrielaortiz@gmail.com',
};

describe('PersonalData', () => {
  let fixture: ComponentFixture<PersonalData>;
  let component: TestPersonalDataComponent;
  let service: {
    getPersonalData: ReturnType<typeof vi.fn>;
    updatePersonalData: ReturnType<typeof vi.fn>;
    validatePhone: ReturnType<typeof vi.fn>;
  };
  let snackbar: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    service = {
      getPersonalData: vi.fn().mockReturnValue(of(basePersonalData)),
      updatePersonalData: vi.fn().mockReturnValue(of(true)),
      validatePhone: vi.fn().mockReturnValue(of(true)),
    };
    snackbar = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [PersonalData],
      providers: [
        provideRouter([]),
        { provide: AccountService, useValue: service },
        {
          provide: Catalogs,
          useValue: {
            getCountryLocations: vi.fn().mockReturnValue(
              of([
                {
                  countryCode: 1,
                  name: 'Uruguay',
                  states: [
                    {
                      countryCode: 1,
                      stateCode: 10,
                      name: 'Montevideo',
                      cities: [
                        {
                          countryCode: 1,
                          stateCode: 10,
                          cityCode: 100,
                          name: 'Montevideo',
                        },
                      ],
                    },
                  ],
                },
              ])
            ),
          },
        },
        { provide: SnackbarHandler, useValue: snackbar },
      ],
    });

    fixture = TestBed.createComponent(PersonalData);
    component = fixture.componentInstance as TestPersonalDataComponent;
  });

  it('should render loaded personal data', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Tipo de documento');
    expect(text).toContain('Datos de contacto');
    expect(text).toContain('Guardar');
    expect(service.getPersonalData).toHaveBeenCalled();
    expect(component.form.controls.firstName.value).toBe('Gabriela');
    expect(component.form.controls.documentNumber.value).toBe('4.123.456-9');
    expect(component.form.controls.phone.value).toEqual({
      iso2: 'UY',
      number: '99123456',
      numberE164: '+59899123456',
    });
  }, 10_000);

  it('should render the fdp organism when the identity is restricted', () => {
    service.getPersonalData.mockReturnValue(of({ ...basePersonalData, identityRestricted: true }));

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('fdp-datos-personales')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('form.personal-data-form')).toBeNull();
  });

  it('should render skeletons while personal data is loading', () => {
    service.getPersonalData.mockReturnValue(NEVER);

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('ort-skeleton')).toHaveLength(6);
  });

  it('should validate phone on blur through the backend validator', () => {
    fixture.detectChanges();
    service.validatePhone.mockClear();

    expect(component.form.controls.phone.updateOn).toBe('blur');

    component.form.controls.phone.setValue({
      iso2: 'UY',
      number: '99123456',
      numberE164: '+59899123456',
    });

    expect(service.validatePhone).toHaveBeenCalledWith({
      iso2: 'UY',
      countryPrefix: 598,
      number: '99123456',
      numberE164: '+59899123456',
    });
  });

  it('should submit editable fields to the backend service', () => {
    fixture.detectChanges();

    component.form.controls.address.setValue('Bulevar Artigas 1234');
    component.submit();

    expect(service.updatePersonalData).toHaveBeenCalledWith({
      countryCode: 1,
      stateCode: 10,
      cityCode: 100,
      address: 'Bulevar Artigas 1234',
      phone: {
        nationalNumber: '99123456',
        iso2: 'UY',
      },
      email: 'gabrielaortiz@gmail.com',
      emailVerification: 'gabrielaortiz@gmail.com',
    });
    expect(snackbar.success).toHaveBeenCalledWith('Datos personales actualizados.');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Datos personales actualizados.');
  });

  it('should submit international phone numbers with their prefix', () => {
    fixture.detectChanges();

    component.form.controls.phone.setValue({
      iso2: 'AR',
      number: '91123456789',
      numberE164: '+5491123456789',
    });
    component.submit();

    expect(service.updatePersonalData).toHaveBeenCalledWith(
      expect.objectContaining({
        phone: {
          nationalNumber: '91123456789',
          iso2: 'AR',
        },
      })
    );
  });

  it('should validate the loaded phone without waiting for user interaction', () => {
    fixture.detectChanges();

    expect(service.validatePhone).toHaveBeenCalledWith({
      iso2: 'UY',
      countryPrefix: 598,
      number: '99123456',
      numberE164: '+59899123456',
    });
    expect(component.form.controls.phone.touched).toBe(false);
  });

  it('should flag the loaded phone when the backend rejects the assumed country', () => {
    service.validatePhone.mockReturnValue(of(false));

    fixture.detectChanges();

    expect(component.form.controls.phone.touched).toBe(true);
    expect(component.form.controls.phone.hasError('phone')).toBe(true);
  });

  it('should wait for the pending phone validation before submitting', () => {
    const pending: Subject<boolean>[] = [];
    service.validatePhone.mockImplementation(() => {
      const validation = new Subject<boolean>();
      pending.push(validation);
      return validation;
    });

    fixture.detectChanges();
    component.submit();

    expect(component.form.controls.phone.status).toBe('PENDING');
    expect(service.updatePersonalData).not.toHaveBeenCalled();

    service.validatePhone.mockReturnValue(of(true));
    pending.forEach(validation => {
      validation.next(true);
      validation.complete();
    });

    expect(service.updatePersonalData).toHaveBeenCalled();
  });

  it('should fall back to Uruguay when the backend could not resolve the stored phone', () => {
    service.getPersonalData.mockReturnValue(
      of({
        ...basePersonalData,
        phone: { nationalNumber: '099123456', iso2: null, e164: null, isValid: false },
      })
    );

    fixture.detectChanges();

    expect(component.form.controls.phone.value).toEqual({
      iso2: 'UY',
      number: '99123456',
      numberE164: '+59899123456',
    });
  });

  it('should restore international phone numbers from backend values', () => {
    service.getPersonalData.mockReturnValue(
      of({
        ...basePersonalData,
        phone: {
          nationalNumber: '91123456789',
          iso2: 'AR',
          e164: '+5491123456789',
          isValid: true,
        },
      })
    );

    fixture.detectChanges();

    expect(component.form.controls.phone.value).toEqual({
      iso2: 'AR',
      number: '91123456789',
      numberE164: '+5491123456789',
    });
  });
});
