import { A11yModule } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  OnDestroy,
  signal,
  ViewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtIconModule } from '@desarrolloort/components';

import { AuthSessionService } from '../../../auth/services/auth-session';

const DRAWER_CLOSE_THRESHOLD_PX = 96;
const DRAWER_EXPANDED_OFFSET_PX = 104;
const DRAWER_SNAP_THRESHOLD_PX = 48;

type DrawerState = 'collapsed' | 'expanded';

@Component({
  selector: 'app-home-header',
  imports: [A11yModule, OrtIconModule, RouterLink],
  templateUrl: './home-header.html',
  styleUrl: './home-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeHeader implements OnDestroy {
  private readonly authSession = inject(AuthSessionService);
  private readonly document = inject(DOCUMENT);

  protected readonly profileMenuOpen = signal(false);
  protected readonly drawerDragOffset = signal(0);
  protected readonly drawerState = signal<DrawerState>('collapsed');
  protected readonly isDraggingDrawer = signal(false);
  protected readonly drawerTransform = computed(() => {
    const expandedOffset = this.drawerState() === 'expanded' ? -DRAWER_EXPANDED_OFFSET_PX : 0;
    const offset = expandedOffset + this.drawerDragOffset();

    return `translateY(${Math.max(-DRAWER_EXPANDED_OFFSET_PX, offset)}px)`;
  });
  private touchStartY: number | null = null;
  private previousBodyOverflow: string | null = null;

  @ViewChild('profileButton')
  private profileButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('firstProfileMenuItem')
  private firstProfileMenuItem?: ElementRef<HTMLAnchorElement>;

  ngOnDestroy(): void {
    this.unlockPageScroll();
  }

  protected toggleProfileMenu(): void {
    this.profileMenuOpen.update(isOpen => !isOpen);

    if (!this.profileMenuOpen()) {
      this.resetDrawer();
      this.unlockPageScroll();
      this.restoreProfileButtonFocus();
      return;
    }

    this.resetDrawer();
    this.lockPageScroll();
    setTimeout(() => this.firstProfileMenuItem?.nativeElement.focus());
  }

  protected closeProfileMenu(restoreFocus = false): void {
    this.profileMenuOpen.set(false);
    this.resetDrawer();
    this.unlockPageScroll();

    if (restoreFocus) {
      this.restoreProfileButtonFocus();
    }
  }

  protected onDrawerTouchStart(event: TouchEvent): void {
    this.touchStartY = event.touches[0]?.clientY ?? null;
    this.isDraggingDrawer.set(true);
    this.drawerDragOffset.set(0);
  }

  protected onDrawerTouchMove(event: TouchEvent): void {
    if (this.touchStartY === null) {
      return;
    }

    if (event.cancelable) {
      event.preventDefault();
    }

    const currentY = event.touches[0]?.clientY ?? this.touchStartY;
    const deltaY = currentY - this.touchStartY;

    if (this.drawerState() === 'expanded') {
      this.drawerDragOffset.set(Math.max(0, deltaY));
      return;
    }

    this.drawerDragOffset.set(Math.max(-DRAWER_EXPANDED_OFFSET_PX, deltaY));
  }

  protected onDrawerTouchEnd(): void {
    const offset = this.drawerDragOffset();
    this.isDraggingDrawer.set(false);

    if (this.drawerState() === 'collapsed' && offset > DRAWER_CLOSE_THRESHOLD_PX) {
      this.closeProfileMenu(true);
      this.touchStartY = null;
      return;
    }

    if (this.drawerState() === 'collapsed' && offset < -DRAWER_SNAP_THRESHOLD_PX) {
      this.drawerState.set('expanded');
      this.drawerDragOffset.set(0);
      this.touchStartY = null;
      return;
    }

    if (this.drawerState() === 'expanded' && offset > DRAWER_SNAP_THRESHOLD_PX) {
      this.drawerState.set('collapsed');
      this.drawerDragOffset.set(0);
      this.touchStartY = null;
      return;
    }

    this.drawerDragOffset.set(0);
    this.touchStartY = null;
  }

  protected logout(): void {
    this.closeProfileMenu();
    this.authSession.logout();
  }

  private restoreProfileButtonFocus(): void {
    setTimeout(() => this.profileButton?.nativeElement.focus());
  }

  private resetDrawer(): void {
    this.drawerState.set('collapsed');
    this.drawerDragOffset.set(0);
    this.isDraggingDrawer.set(false);
    this.touchStartY = null;
  }

  private lockPageScroll(): void {
    const body = this.document.body;

    if (this.previousBodyOverflow !== null || !body) {
      return;
    }

    this.previousBodyOverflow = body.style.overflow;
    body.style.overflow = 'hidden';
  }

  private unlockPageScroll(): void {
    const body = this.document.body;

    if (this.previousBodyOverflow === null || !body) {
      return;
    }

    body.style.overflow = this.previousBodyOverflow;
    this.previousBodyOverflow = null;
  }
}
