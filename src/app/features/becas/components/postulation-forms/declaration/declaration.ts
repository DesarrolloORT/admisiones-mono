import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  FormArray,
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
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

type MonthlyExpense = {
  id: number;
  name: string;
  amount: number;
};

type FamilyMemberControls = {
  id: FormControl<number>;
  relationship: FormControl<string | null>;
  name: FormControl<string | null>;
  age: FormControl<string | null>;
  percibeIngresos: FormControl<'si' | 'no' | null>;
  nominalIncome: FormControl<string | null>;
  liquidIncome: FormControl<string | null>;
  proofAttached: FormControl<boolean | null>;
};

type FamilyMemberGroup = FormGroup<FamilyMemberControls>;

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
  protected readonly submitted = signal(false);
  protected readonly declarationForm = new FormGroup({
    familyMembers: new FormArray<FamilyMemberGroup>([this.createFamilyMember(1)]),
    housingType: new FormControl<string | null>(null, Validators.required),
    summerHouse: new FormControl<string | null>(null, Validators.required),
    vehicleOwn: new FormControl<string | null>(null, Validators.required),
    vehicleModel: new FormControl<string | null>(null),
    vehicleYear: new FormControl<string | null>(null),
    savingsAmount: new FormControl<number | null>(null, Validators.required),
    studyPlan: new FormControl<string | null>(null, Validators.required),
    housingExpenses: new FormControl<number | null>(null, Validators.required),
    otherServicesExpenses: new FormControl<number | null>(null, Validators.required),
    healthExpenses: new FormControl<number | null>(null, Validators.required),
    personalExpenses: new FormControl<number | null>(null, Validators.required),
    transportationExpenses: new FormControl<number | null>(null, Validators.required),
    observations: new FormControl<string | null>(null),
  });

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
  private nextMemberId = 2;

  private createFamilyMember(id: number): FamilyMemberGroup {
    const member = new FormGroup<FamilyMemberControls>({
      id: new FormControl(id, { nonNullable: true }),
      relationship: new FormControl<string | null>(null),
      name: new FormControl<string | null>(null, Validators.required),
      age: new FormControl<string | null>(null, Validators.required),
      percibeIngresos: new FormControl<'si' | 'no' | null>(null, Validators.required),
      nominalIncome: new FormControl<string | null>(null),
      liquidIncome: new FormControl<string | null>(null),
      proofAttached: new FormControl<boolean>(false),
    });

    this.configureIncomeValidation(member);

    return member;
  }
  private configureIncomeValidation(member: FamilyMemberGroup): void {
    member.controls.percibeIngresos.valueChanges.subscribe(value => {
      if (value === 'si') {
        member.controls.nominalIncome.setValidators(Validators.required);
        member.controls.liquidIncome.setValidators(Validators.required);
      } else {
        member.controls.nominalIncome.clearValidators();
        member.controls.liquidIncome.clearValidators();

        member.controls.nominalIncome.reset(null);
        member.controls.liquidIncome.reset(null);
        member.controls.proofAttached.setValue(false);
      }

      member.controls.nominalIncome.updateValueAndValidity();
      member.controls.liquidIncome.updateValueAndValidity();
      member.controls.proofAttached.updateValueAndValidity();

      member.updateValueAndValidity();
      this.familyMembers.updateValueAndValidity();
      this.declarationForm.updateValueAndValidity();
    });
  }

  addMember() {
    this.familyMembers.push(this.createFamilyMember(this.nextMemberId++));
  }

  removeMember(id: number) {
    const index = this.familyMembers.controls.findIndex(member => member.controls.id.value === id);

    if (index < 0) {
      return;
    }

    this.familyMembers.removeAt(index);
    this.familyMembers.updateValueAndValidity();
    this.declarationForm.updateValueAndValidity();
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
    const selectedFile = change.value.find(file => file.isValid)?.file ?? null;
    const member = this.familyMembers.controls.find(item => item.controls.id.value === memberId);
    member?.controls.proofAttached.setValue(Boolean(selectedFile));
  }

  public isIncomeProofFileMissing(member: FamilyMemberGroup): boolean {
    return (
      member.controls['percibeIngresos'].value === 'si' && !member.controls['proofAttached'].value
    );
  }

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);

    this.declarationForm.markAllAsTouched();
    this.familyMembers.controls.forEach(member => member.markAllAsTouched());

    const membersValid = this.familyMembers.controls.every((member, index) => {
      const isRelationshipValid = index === 0 || Boolean(member.controls.relationship.value);

      const isProofValid =
        member.controls.percibeIngresos.value !== 'si' ||
        Boolean(member.controls.proofAttached.value);

      return member.valid && isRelationshipValid && isProofValid;
    });

    this.declarationForm.updateValueAndValidity();

    return this.declarationForm.valid && membersValid;
  }

  private configureVehicleValidation(): void {
    this.vehicleOwnControl.valueChanges.subscribe(value => {
      if (value === 'si') {
        this.vehicleModelControl.setValidators(Validators.required);
        this.vehicleYearControl.setValidators(Validators.required);
      } else {
        this.vehicleModelControl.clearValidators();
        this.vehicleYearControl.clearValidators();

        this.vehicleModelControl.reset(null);
        this.vehicleYearControl.reset(null);
      }

      this.vehicleModelControl.updateValueAndValidity();
      this.vehicleYearControl.updateValueAndValidity();
      this.declarationForm.updateValueAndValidity();
    });
  }

  constructor() {
    this.configureVehicleValidation();
  }
}
