import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { NgOptimizedImage } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
  ViewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  OrtButton,
  OrtExpandableStepperModule,
  OrtExpandableStepperStep,
  OrtIconModule,
} from '@desarrolloort/components';
import { map } from 'rxjs/operators';

const COMPACT = '(width < 26.25rem)';
const WIDE_MOBILE = '(width >= 26.25rem) and (width < 40.625rem)';
const TABLET = '(width >= 40.625rem) and (width < 52.5rem)';
const DESKTOP = '(width >= 52.5rem)';
const VIEWPORTS = [COMPACT, WIDE_MOBILE, TABLET, DESKTOP] as const;

type ProcessLayoutViewport = 'compact' | 'wide-mobile' | 'tablet' | 'desktop';

@Component({
  selector: 'app-process-layout',
  imports: [
    A11yModule,
    NgOptimizedImage,
    OrtExpandableStepperModule,
    OrtIconModule,
    RouterLink,
    OrtButton,
  ],
  templateUrl: './process-layout.html',
  styleUrl: './process-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProcessLayout {
  private readonly breakpointObserver = inject(BreakpointObserver);

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
  protected readonly viewport = toSignal(
    this.breakpointObserver.observe(VIEWPORTS).pipe(
      map(state => {
        if (state.breakpoints[DESKTOP]) return 'desktop';
        if (state.breakpoints[TABLET]) return 'tablet';
        if (state.breakpoints[WIDE_MOBILE]) return 'wide-mobile';
        return 'compact';
      })
    ),
    { initialValue: 'compact' as ProcessLayoutViewport }
  );
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
