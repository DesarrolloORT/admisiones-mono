import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  OrtButton,
  OrtCardModule,
  OrtDrawer,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-declaration',
  imports: [
    OrtCardModule,
    OrtFormFieldModule,
    OrtRadioModule,
    OrtInputModule,
    OrtButton,
    OrtIconModule,
    OrtDrawer,
  ],
  templateUrl: './declaration.html',
  styleUrls: ['../../../pages/fbr/fbr.scss', './declaration.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Declaration {
  drawer = signal(false);

  openDrawer() {
    this.drawer.set(true);
  }

  closeDrawer() {
    this.drawer.set(false);
  }
}
