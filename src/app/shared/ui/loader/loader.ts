import { Component, inject } from '@angular/core';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { LoaderService } from '@desarrolloort/ngx-utils';

@Component({
  selector: 'app-loader',
  imports: [MatProgressSpinner, MatProgressBar],
  templateUrl: './loader.html',
  styleUrl: './loader.scss',
})
export class Loader {
  private readonly loaderService = inject(LoaderService);

  protected readonly loading = this.loaderService.isLoading;
}
