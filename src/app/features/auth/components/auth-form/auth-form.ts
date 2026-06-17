import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

@Component({
  selector: 'app-auth-form',
  imports: [OrtIconModule],
  templateUrl: './auth-form.html',
  styleUrl: './auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthForm {
  public readonly title = input.required<string>();
  public readonly description = input<string>();
  public readonly heroTitle = input.required<string>();
  public readonly heroDescription = input.required<string>();
  public readonly heroIcon = input.required<string>();
  public readonly stepLabel = input<string | null>(null);
  public readonly stepTitle = input<string | null>(null);
  public readonly cardSize = input<'default' | 'long'>('default');
  public readonly hideHeader = input<boolean>(false);
  public readonly centerContent = input<boolean>(false);

  protected readonly isStep = computed(() => Boolean(this.stepLabel() && this.stepTitle()));
  protected readonly showHeader = computed(() => !this.hideHeader());
}
