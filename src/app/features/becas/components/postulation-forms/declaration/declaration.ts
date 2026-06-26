import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  OrtButton,
  OrtCardModule,
  OrtDrawer,
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

type Member = {
  id: number;
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
  ],
  templateUrl: './declaration.html',
  styleUrls: ['../../../pages/fbr/fbr.scss', './declaration.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Declaration {
  members = signal<Member[]>([
    {
      id: 1,
    },
  ]);

  private nextMemberId = 2;

  agregarIntegrante() {
    this.members.update(members => [
      ...members,
      {
        id: this.nextMemberId++,
      },
    ]);
  }

  removeMember(id: number) {
    this.members.update(members => members.filter(member => member.id !== id));
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
}
