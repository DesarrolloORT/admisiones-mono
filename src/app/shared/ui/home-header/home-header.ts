import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Location, NgTemplateOutlet } from '@angular/common';
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
import { OrtDrawerModule, OrtIconModule } from '@desarrolloort/components';
import { map } from 'rxjs/operators';

type DrawerCloseReason = 'backdrop' | 'escape' | 'drag' | 'programmatic';

@Component({
  selector: 'app-home-header',
  imports: [A11yModule, NgTemplateOutlet, OrtDrawerModule, OrtIconModule, RouterLink],
  templateUrl: './home-header.html',
  styleUrl: './home-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeHeader {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly location = inject(Location);

  readonly showBack = input(false);
  readonly logoutRequested = output<void>();

  protected readonly profileMenuOpen = signal(false);
  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe('(max-width: 760px)').pipe(map(state => state.matches)),
    { initialValue: false }
  );

  @ViewChild('profileButton')
  private readonly profileButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('firstProfileMenuItem')
  private readonly firstProfileMenuItem?: ElementRef<HTMLAnchorElement>;

  protected toggleProfileMenu(): void {
    if (this.profileMenuOpen()) {
      this.closeProfileMenu(true);
      return;
    }

    this.profileMenuOpen.set(true);

    if (!this.isMobile()) {
      setTimeout(() => this.firstProfileMenuItem?.nativeElement.focus());
    }
  }

  protected closeProfileMenu(restoreFocus = false): void {
    this.profileMenuOpen.set(false);

    if (restoreFocus) {
      this.restoreProfileButtonFocus();
    }
  }

  protected onDrawerClosed(reason: DrawerCloseReason): void {
    this.profileMenuOpen.set(false);

    if (reason !== 'programmatic') {
      this.restoreProfileButtonFocus();
    }
  }

  protected logout(): void {
    this.closeProfileMenu();
    this.logoutRequested.emit();
  }

  protected goBack(): void {
    this.location.back();
  }

  private restoreProfileButtonFocus(): void {
    setTimeout(() => this.profileButton?.nativeElement.focus());
  }
}
