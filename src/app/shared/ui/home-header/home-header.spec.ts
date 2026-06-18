import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HomeHeader } from './home-header';

interface TouchLike {
  touches: Array<{ clientY: number }>;
  cancelable?: boolean;
  preventDefault?: () => void;
}

type TestHomeHeader = HomeHeader & {
  closeProfileMenu: (restoreFocus?: boolean) => void;
  drawerTransform: () => string;
  onDrawerTouchEnd: () => void;
  onDrawerTouchMove: (event: TouchEvent) => void;
  onDrawerTouchStart: (event: TouchEvent) => void;
  profileMenuOpen: () => boolean;
};

describe('HomeHeader', () => {
  let fixture: ComponentFixture<HomeHeader>;
  let component: TestHomeHeader;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HomeHeader],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(HomeHeader);
    component = fixture.componentInstance as TestHomeHeader;
  });

  afterEach(() => {
    component.closeProfileMenu();
    document.body.style.overflow = '';
  });

  it('should keep the menu button and logo visible in the shared header', () => {
    fixture.detectChanges();

    const logo = fixture.nativeElement.querySelector('.home-brand__logo') as HTMLImageElement;
    const brandName = fixture.nativeElement.querySelector('.home-brand__name') as HTMLSpanElement;
    const profileButton = fixture.nativeElement.querySelector('.home-avatar') as HTMLButtonElement;

    expect(logo?.getAttribute('src')).toBe('assets/auth/ort-logo-white.svg');
    expect(logo?.getAttribute('alt')).toBe('ORT');
    expect(brandName?.textContent?.trim()).toBe('Admisiones');
    expect(profileButton?.getAttribute('aria-label')).toBe('Abrir menú de usuario');
    expect(profileButton?.getAttribute('aria-haspopup')).toBe('dialog');
    expect(profileButton?.getAttribute('aria-controls')).toBe('home-profile-menu');
  });

  it('should expose profile links through the user menu', async () => {
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.home-avatar').click();
    fixture.detectChanges();
    await waitForTimers();

    const text = fixture.nativeElement.textContent as string;
    const profileLink = fixture.nativeElement.querySelector(
      'a[routerLink="/inicio/datos-personales"]'
    );
    const dialog = fixture.nativeElement.querySelector('.home-profile-menu');
    const backdrop = fixture.nativeElement.querySelector('.home-profile-menu__backdrop-button');
    const nav = fixture.nativeElement.querySelector(
      'nav[aria-labelledby="home-profile-menu-title"]'
    );

    expect(text).toContain('Editar perfil');
    expect(text).toContain('Cambiar contraseña');
    expect(profileLink).toBeTruthy();
    expect(dialog?.getAttribute('role')).toBe('dialog');
    expect(dialog?.getAttribute('aria-modal')).toBe('true');
    expect(dialog?.getAttribute('aria-labelledby')).toBe('home-profile-menu-title');
    expect(backdrop?.getAttribute('aria-label')).toBe('Cerrar menú de usuario');
    expect(backdrop?.getAttribute('tabindex')).toBe('-1');
    expect(nav).toBeTruthy();
    expect(document.activeElement).toBe(profileLink);
  });

  it('should restore focus when closing the profile drawer from backdrop', async () => {
    fixture.detectChanges();

    const profileButton = fixture.nativeElement.querySelector('.home-avatar') as HTMLButtonElement;
    profileButton.click();
    fixture.detectChanges();
    await waitForTimers();

    fixture.nativeElement.querySelector('.home-profile-menu__backdrop-button').click();
    fixture.detectChanges();
    await waitForTimers();

    expect(component.profileMenuOpen()).toBe(false);
    expect(document.activeElement).toBe(profileButton);
  });

  it('should expand, collapse and close the profile drawer with touch gestures', () => {
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.home-avatar').click();
    fixture.detectChanges();

    component.onDrawerTouchStart(touchEvent(420));
    component.onDrawerTouchMove(touchEvent(330));
    component.onDrawerTouchEnd();
    expect(component.drawerTransform()).toBe('translateY(-104px)');

    component.onDrawerTouchStart(touchEvent(330));
    component.onDrawerTouchMove(touchEvent(390));
    component.onDrawerTouchEnd();
    expect(component.drawerTransform()).toBe('translateY(0px)');

    component.onDrawerTouchStart(touchEvent(390));
    component.onDrawerTouchMove(touchEvent(510));
    component.onDrawerTouchEnd();
    expect(component.profileMenuOpen()).toBe(false);
  });

  it('should emit logout from the user menu', () => {
    const logoutRequested = vi.fn();
    component.logoutRequested.subscribe(logoutRequested);

    fixture.detectChanges();

    fixture.nativeElement.querySelector('.home-avatar').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.home-profile-menu__item:last-child').click();

    expect(logoutRequested).toHaveBeenCalledOnce();
    expect(component.profileMenuOpen()).toBe(false);
  });
});

function touchEvent(clientY: number): TouchEvent {
  const event: TouchLike = {
    touches: [{ clientY }],
    cancelable: true,
    preventDefault: vi.fn(),
  };

  return event as unknown as TouchEvent;
}

function waitForTimers(): Promise<void> {
  return new Promise(resolve => setTimeout(resolve));
}
