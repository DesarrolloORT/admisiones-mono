import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import type { PhoneInputValue } from '@desarrolloort/components';
import { NEVER, of } from 'rxjs';
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
  phone: FormControl<PhoneInputValue | null>;
  email: FormControl<string>;
  emailConfirmation: FormControl<string>;
}

type TestPersonalDataComponent = PersonalData & {
  form: FormGroup<TestPersonalDataForm>;
  submit: () => void;
};

describe('PersonalData', () => {
  let fixture: ComponentFixture<PersonalData>;
  let component: TestPersonalDataComponent;
  let service: {
    getPersonalData: ReturnType<typeof vi.fn>;
    updatePersonalData: ReturnType<typeof vi.fn>;
  };
  let snackbar: { success: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    service = {
      getPersonalData: vi.fn().mockReturnValue(
        of({
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
          phone: '99123456',
          email: 'gabrielaortiz@gmail.com',
          emailVerification: 'gabrielaortiz@gmail.com',
        })
      ),
      updatePersonalData: vi.fn().mockReturnValue(of(true)),
    };
    snackbar = { success: vi.fn() };

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
                  codigoPais: 1,
                  nombre: 'Uruguay',
                  estado: [
                    {
                      codigoPais: 1,
                      codigoEstado: 10,
                      nombre: 'Montevideo',
                      ciudad: [
                        {
                          codigoPais: 1,
                          codigoEstado: 10,
                          codigoCiudad: 100,
                          nombre: 'Montevideo',
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
    expect(service.getPersonalData).toHaveBeenCalledWith(true);
    expect(component.form.controls.firstName.value).toBe('Gabriela');
    expect(component.form.controls.documentNumber.value).toBe('4.123.456-9');
    expect(component.form.controls.phone.value).toEqual({
      iso2: 'UY',
      number: '99123456',
      numberE164: '+59899123456',
    });
  });

  it('should render skeletons while personal data is loading', () => {
    service.getPersonalData.mockReturnValue(NEVER);

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('ort-skeleton')).toHaveLength(6);
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
      phone: '99123456',
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
        phone: '+5491123456789',
      })
    );
  });

  it('should restore international phone numbers from backend values', () => {
    service.getPersonalData.mockReturnValue(
      of({
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
        phone: '+5491123456789',
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
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
