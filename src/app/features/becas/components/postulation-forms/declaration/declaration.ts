import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormArray, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  OrtButton,
  OrtCardModule,
  OrtDialogModule,
  OrtDrawer,
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtIconButton,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';
import type { FamilyMemberGroup } from '../../../models/scholarship-personal-forms';

type MonthlyExpense = {
  id: number;
  name: string;
  amount: number;
};

@Component({
  selector: 'app-declaration',
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
    OrtSelectModule,
    OrtFileUploaderModule,
    OrtDialogModule,
    ReactiveFormsModule,
    OrtError,
  ],
  templateUrl: './declaration.html',
  styleUrls: ['../../../pages/fbr/fbr.scss', './declaration.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Declaration {
  protected readonly facade = inject(ScholarshipPersonalFacade);
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

  openDialog: boolean = false;

  addMember(): void {
    this.facade.addFamilyMember();
  }

  removeMember(id: number): void {
    this.facade.removeFamilyMember(id);
  }

  drawer = signal(false);

  expenses = signal<MonthlyExpense[]>([]);

  openDrawer() {
    this.drawer.set(true);
  }

  closeDrawer() {
    this.drawer.set(false);
  }

  saveExpense(event: SubmitEvent, name: string, amountValue: string, form: HTMLFormElement) {
    event.preventDefault();

    const amount = Number(amountValue);

    if (!name.trim()) {
      return;
    }

    if (!amount || amount <= 1000) {
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
