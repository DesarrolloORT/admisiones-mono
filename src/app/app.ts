import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Loader } from './shared/ui/loader/loader';
import { Snackbar } from './shared/ui/snackbar/snackbar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Loader, Snackbar],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
