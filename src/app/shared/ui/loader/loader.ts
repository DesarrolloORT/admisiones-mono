import { Component, inject } from '@angular/core';
import { OrtSpinner } from '@desarrolloort/components';
import { LoaderService } from '@desarrolloort/ngx-utils';

@Component({
  selector: 'app-loader',
  imports: [OrtSpinner],
  templateUrl: './loader.html',
  styleUrl: './loader.scss',
})
export class Loader {
  private readonly loaderService = inject(LoaderService);

  protected readonly loading = this.loaderService.isLoading;
}
