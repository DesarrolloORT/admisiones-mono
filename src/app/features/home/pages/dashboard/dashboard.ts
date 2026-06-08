import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';

import { HomeEndpoint } from '../../endpoints/home.endpoint';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard implements OnInit {
  private readonly endpoint = inject(HomeEndpoint);

  ngOnInit(): void {
    this.endpoint.getMisInscripciones().subscribe(inscripciones => {
      console.log('Mis inscripciones:', inscripciones);
    });
  }
}

