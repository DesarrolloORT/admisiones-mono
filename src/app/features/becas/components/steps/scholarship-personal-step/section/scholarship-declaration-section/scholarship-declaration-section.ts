import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  OrtButton,
  OrtCardModule,
  OrtDialogModule,
  OrtDivider,
  OrtDrawer,
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtIconButton,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import {
  ResponsiveSelect,
  ResponsiveSelectOption,
} from 'src/app/shared/ui/responsive-select/responsive-select';

import { ScholarshipPersonalFacade } from '../../../../../facades/scholarship-personal';
import type { FamilyMemberGroup } from '../../../../../models/scholarship-personal-forms';

type MonthlyExpense = {
  id: number;
  name: string;
  amount: number;
};

@Component({
  selector: 'app-scholarship-declaration-section',
  imports: [
    FormsModule,
    OrtCardModule,
    OrtFormFieldModule,
    OrtRadioModule,
    OrtInputModule,
    OrtButton,
    OrtIconModule,
    OrtDrawer,
    OrtIconButton,
    OrtFileUploaderModule,
    OrtDialogModule,
    ReactiveFormsModule,
    OrtError,
    OrtDivider,
    ResponsiveSelect,
  ],
  templateUrl: './scholarship-declaration-section.html',
  styleUrls: [
    '../../../../../pages/scholarship-process/scholarship-process.scss',
    './scholarship-declaration-section.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipDeclarationSection {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly declarationForm = this.facade.declarationForm;
  protected readonly familyMembers = this.declarationForm.controls
    .familyMembers as FormArray<FamilyMemberGroup>;
  protected readonly housingTypeControl = this.declarationForm.controls.housingType;
  protected readonly summerHouseControl = this.declarationForm.controls.summerHouse;
  protected readonly vehicleOwnControl = this.declarationForm.controls.vehicleOwn;
  protected readonly vehicleModelControl = this.declarationForm.controls.vehicleModel;
  protected readonly vehicleYearControl = this.declarationForm.controls.vehicleYear;
  protected readonly savingsAmountControl = this.declarationForm.controls.savingsAmount;
  protected readonly studyPlanControl = this.declarationForm.controls.studyPlan;
  protected readonly housingExpensesControl = this.declarationForm.controls.housingExpenses;
  protected readonly otherServicesExpensesControl =
    this.declarationForm.controls.otherServicesExpenses;
  protected readonly healthExpensesControl = this.declarationForm.controls.healthExpenses;
  protected readonly personalExpensesControl = this.declarationForm.controls.personalExpenses;
  protected readonly transportationExpensesControl =
    this.declarationForm.controls.transportationExpenses;
  protected readonly observationsControl = this.declarationForm.controls.observations;

  protected readonly relationshipOptions: ResponsiveSelectOption[] = [
    { value: 'padre', label: 'Padre' },
    { value: 'madre', label: 'Madre' },
    { value: 'hermano', label: 'Hermano/a' },
    { value: 'hijo', label: 'Hijo/a' },
    { value: 'conyuge', label: 'Cónyuge' },
    { value: 'otro', label: 'Otro' },
  ];

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });

  protected readonly radioGroupIndicatorPosition = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'right' : 'left';
  });

  protected readonly fileUploaderDisplay = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'inline' : 'block';
  });

  openDialog: boolean = false;

  addMember(): void {
    this.facade.addFamilyMember();
  }

  removeMember(id: number): void {
    this.facade.removeFamilyMember(id);
  }

  drawer = signal(false);

  expenses = signal<MonthlyExpense[]>([]);

  protected readonly expenseAmountError = signal(false);

  openDrawer() {
    this.expenseAmountError.set(false);
    this.drawer.set(true);
  }

  closeDrawer() {
    this.drawer.set(false);
  }

  saveExpense(event: SubmitEvent, name: string, amountValue: string, form: HTMLFormElement) {
    event.preventDefault();
    event.stopPropagation();

    const amount = Number(amountValue);
    const isAmountValid = amount > 1000;

    this.expenseAmountError.set(!isAmountValid);

    if (!name.trim() || !isAmountValid) {
      return;
    }

    this.expenses.update(expenses => [
      ...expenses,
      {
        id: Date.now(),
        name: name.trim(),
        amount,
      },
    ]);

    form.reset();
    this.expenseAmountError.set(false);
    this.closeDrawer();
  }

  removeExpense(id: number) {
    this.expenses.update(expenses => expenses.filter(expense => expense.id !== id));
  }

  public onIncomeProofFilesChanged(memberId: number, change: OrtFileUploaderChange): void {
    this.facade.onIncomeProofFilesChanged(memberId, change);
  }

  public isIncomeProofFileMissing(member: FamilyMemberGroup): boolean {
    return this.facade.isIncomeProofFileMissing(member);
  }
}
