import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipDeclarationSection } from './scholarship-declaration-section';

function incomeProofUpload(): OrtFileUploaderChange {
  return {
    value: [{ file: new File(['x'], 'recibo.pdf', { type: 'application/pdf' }), isValid: true }],
  } as unknown as OrtFileUploaderChange;
}

function submitEvent(): SubmitEvent {
  return { preventDefault: () => undefined, stopPropagation: () => undefined } as SubmitEvent;
}

describe('ScholarshipDeclarationSection', () => {
  let fixture: ComponentFixture<ScholarshipDeclarationSection>;
  let component: ScholarshipDeclarationSection;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipDeclarationSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    fixture = TestBed.createComponent(ScholarshipDeclarationSection);
    fixture.detectChanges();
    component = fixture.componentInstance;
  });

  function memberTitles(): (string | undefined)[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.member-card__title')).map(title =>
      (title as Element).textContent?.trim()
    );
  }

  // El primer integrante es el postulante: no se puede borrar ni declara parentesco.
  it('starts with the applicant as the only family member', () => {
    expect(memberTitles()).toEqual(['Integrante 1']);
    expect(fixture.nativeElement.textContent).toContain('Postulante');
    expect(fixture.nativeElement.querySelector('app-responsive-select')).toBeFalsy();
    expect(
      fixture.nativeElement.querySelector('button[aria-label="Eliminar integrante"]')
    ).toBeFalsy();
  });

  it('adds a family member that declares a relationship and can be removed', () => {
    component.addMember();
    fixture.detectChanges();

    expect(memberTitles()).toEqual(['Integrante 1', 'Integrante 2']);
    expect(fixture.nativeElement.querySelector('app-responsive-select')).toBeTruthy();

    (
      fixture.nativeElement.querySelector(
        'button[aria-label="Eliminar integrante"]'
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    expect(facade.familyMembers.controls.map(member => member.controls.id.value)).toEqual([1]);
    expect(memberTitles()).toEqual(['Integrante 1']);
  });

  it('asks for the income proof of a member that declares income', () => {
    const member = facade.familyMembers.at(0);
    member.controls.percibeIngresos.setValue('si');
    facade.submitted.set(true);
    fixture.detectChanges();

    expect(component.isIncomeProofFileMissing(member)).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(
      'Adjuntá al menos un comprobante de ingresos'
    );

    component.onIncomeProofFilesChanged(member.controls.id.value, incomeProofUpload());
    fixture.detectChanges();

    expect(component.isIncomeProofFileMissing(member)).toBe(false);
  });

  it('asks about housing, summer house and vehicle', () => {
    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      group => (group as Element).getAttribute('legend')
    );

    expect(legends).toContain('¿En qué tipo de vivienda residís?');
    expect(legends).toContain('¿Cuentan con casa de verano o descanso?');
    expect(legends).toContain('¿Cuentan con vehículo propio?');
  });

  it('records a monthly expense and closes the drawer', () => {
    const form = document.createElement('form');
    component.openDrawer();

    component.saveExpense(submitEvent(), 'Luz', '2500', form);

    expect(component.expenses()).toEqual([expect.objectContaining({ name: 'Luz', amount: 2500 })]);
    expect(component.drawer()).toBe(false);
  });

  // Un gasto de menos de 1000 no se guarda: se marca el error y el drawer queda abierto.
  it('rejects an expense below the minimum amount', () => {
    const form = document.createElement('form');
    component.openDrawer();

    component.saveExpense(submitEvent(), 'Luz', '500', form);

    expect(component['expenseAmountError']()).toBe(true);
    expect(component.expenses()).toEqual([]);
    expect(component.drawer()).toBe(true);
  });

  it('rejects an expense without a name', () => {
    const form = document.createElement('form');

    component.saveExpense(submitEvent(), '   ', '2500', form);

    expect(component.expenses()).toEqual([]);
  });

  it('removes a recorded expense', () => {
    const form = document.createElement('form');
    component.saveExpense(submitEvent(), 'Luz', '2500', form);
    const [expense] = component.expenses();

    component.removeExpense(expense.id);

    expect(component.expenses()).toEqual([]);
  });

  it('clears the amount error when the drawer is reopened', () => {
    const form = document.createElement('form');
    component.saveExpense(submitEvent(), 'Luz', '500', form);

    component.openDrawer();

    expect(component['expenseAmountError']()).toBe(false);
  });

  it('switches to the mobile layout on small screens', () => {
    expect(component['isMobile']()).toBe(false);

    breakpoint.set({ isXSmall: false, isSmall: true });

    expect(component['isMobile']()).toBe(true);
    expect(component['radioGroupOrientation']()).toBe('vertical');
  });
});
