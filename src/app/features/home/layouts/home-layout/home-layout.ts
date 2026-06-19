import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';

import { HomeHeader } from '../../../../shared/ui/home-header/home-header';
import { AuthSessionService } from '../../../auth/services/auth-session';

@Component({
  selector: 'app-home-layout',
  imports: [HomeHeader, RouterOutlet],
  templateUrl: './home-layout.html',
  styleUrl: './home-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeLayout {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly showBack = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => event.urlAfterRedirects !== '/inicio'),
      startWith(this.router.url !== '/inicio')
    ),
    { initialValue: false }
  );

  protected logout(): void {
    this.authSession.logout();
  }
}
