import { A11yModule } from '@angular/cdk/a11y';
import { NgOptimizedImage } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  signal,
  ViewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  OrtButton,
  OrtButtonBaseDirective,
  OrtExpandableStepperModule,
  OrtExpandableStepperStep,
  OrtIconModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-process-layout',
  imports: [
    A11yModule,
    NgOptimizedImage,
    OrtExpandableStepperModule,
    OrtIconModule,
    RouterLink,
    OrtButtonBaseDirective,
    OrtButton,
  ],
  templateUrl: './process-layout.html',
  styleUrl: './process-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProcessLayout {
  public readonly brandName = input('Admisiones');
  public readonly backLabel = input('Volver al paso anterior');
  public readonly closeLabel = input('Cerrar proceso');
  public readonly closeVisible = input(true);
  public readonly currentStepId = input<string | undefined>(undefined);
  public readonly logoAlt = input('ORT');
  public readonly logoSrc = input('/assets/auth/ort-logo-white.svg');
  public readonly processTitle = input('');
  public readonly showBack = input(false);
  public readonly showStepper = input(true);
  public readonly stepperAriaLabel = input('Pasos del proceso');
  public readonly stepperExpanded = input(false);
  public readonly stepperSubtitle = input<string | undefined>(undefined);
  public readonly steps = input<OrtExpandableStepperStep[]>([]);

  public readonly back = output<void>();
  public readonly closeFlow = output<void>();
  public readonly logoutRequested = output<void>();

  protected readonly profileMenuOpen = signal(false);

  @ViewChild('profileButton')
  private readonly profileButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('firstProfileMenuItem')
  private readonly firstProfileMenuItem?: ElementRef<HTMLAnchorElement>;

  protected backClick(): void {
    this.back.emit();
  }

  protected toggleProfileMenu(): void {
    if (this.profileMenuOpen()) {
      this.closeProfileMenu(true);
      return;
    }

    this.profileMenuOpen.set(true);
    setTimeout(() => this.firstProfileMenuItem?.nativeElement.focus());
  }

  protected closeProfileMenu(restoreFocus = false): void {
    this.profileMenuOpen.set(false);

    if (restoreFocus) {
      this.restoreProfileButtonFocus();
    }
  }

  protected logout(): void {
    this.closeProfileMenu();
    this.logoutRequested.emit();
  }

  private restoreProfileButtonFocus(): void {
    setTimeout(() => this.profileButton?.nativeElement.focus());
  }
}
