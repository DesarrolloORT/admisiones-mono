import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Location } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { HomeHeader } from './home-header';

type TestHomeHeader = HomeHeader & {
  closeProfileMenu: (restoreFocus?: boolean) => void;
  onDrawerClosed: (reason: 'backdrop' | 'escape' | 'drag' | 'programmatic') => void;
  profileMenuOpen: () => boolean;
};

describe('HomeHeader', () => {
  let fixture: ComponentFixture<HomeHeader>;
  let component: TestHomeHeader;
  let location: { back: ReturnType<typeof vi.fn> };
  let viewport: BehaviorSubject<BreakpointState>;

  beforeEach(() => {
    location = { back: vi.fn() };
    viewport = new BehaviorSubject<BreakpointState>({
      matches: false,
      breakpoints: {},
    });

    TestBed.configureTestingModule({
      imports: [HomeHeader],
      providers: [
        provideRouter([]),
        { provide: Location, useValue: location },
        {
          provide: BreakpointObserver,
          useValue: { observe: vi.fn(() => viewport.asObservable()) },
        },
      ],
    });

    fixture = TestBed.createComponent(HomeHeader);
    component = fixture.componentInstance as TestHomeHeader;
  });

  afterEach(() => {
    component.closeProfileMenu();
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
    expect(fixture.nativeElement.querySelector('.home-header__back')).toBeNull();
  });

  it('should navigate back when the optional action is enabled', async () => {
    fixture.componentRef.setInput('showBack', true);
    await fixture.whenStable();

    const backButton = fixture.nativeElement.querySelector(
      '.home-header__back'
    ) as HTMLButtonElement;

    expect(backButton?.getAttribute('aria-label')).toBe('Volver al inicio');

    backButton.click();

    expect(location.back).toHaveBeenCalledOnce();
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
    const nav = fixture.nativeElement.querySelector('nav[aria-label="Opciones de usuario"]');

    expect(text).toContain('Editar perfil');
    expect(text).toContain('Cambiar contraseña');
    expect(profileLink).toBeTruthy();
    expect(dialog?.getAttribute('role')).toBe('dialog');
    expect(dialog?.getAttribute('aria-modal')).toBe('true');
    expect(dialog?.getAttribute('aria-labelledby')).toBe('home-profile-menu-title');
    expect(nav).toBeTruthy();
    expect(document.activeElement?.getAttribute('href')).toBe('/inicio/datos-personales');
  });

  it('should open the ORT drawer on mobile', async () => {
    viewport.next({ matches: true, breakpoints: {} });
    await fixture.whenStable();

    const profileButton = fixture.nativeElement.querySelector('.home-avatar') as HTMLButtonElement;
    profileButton.click();
    await fixture.whenStable();

    const drawer = fixture.debugElement.query(By.css('ort-drawer')).componentInstance as {
      open: () => boolean;
    };

    expect(drawer.open()).toBe(true);
    expect(fixture.nativeElement.querySelector('.home-profile-menu')).toBeNull();
  });

  it('should restore focus when the ORT drawer closes interactively', async () => {
    await fixture.whenStable();

    const profileButton = fixture.nativeElement.querySelector('.home-avatar') as HTMLButtonElement;

    component.onDrawerClosed('backdrop');
    await waitForTimers();

    expect(component.profileMenuOpen()).toBe(false);
    expect(document.activeElement).toBe(profileButton);
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

function waitForTimers(): Promise<void> {
  return new Promise(resolve => setTimeout(resolve));
}
