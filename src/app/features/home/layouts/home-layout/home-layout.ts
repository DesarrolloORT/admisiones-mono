import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { HomeHeader } from '../../components/home-header/home-header';

@Component({
  selector: 'app-home-layout',
  imports: [HomeHeader, RouterOutlet],
  templateUrl: './home-layout.html',
  styleUrl: './home-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeLayout {}
