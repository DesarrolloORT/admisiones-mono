import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { HomeService } from './home';

describe('HomeService', () => {
  let service: HomeService;
  let endpointMock: {
    getMyEnrollments: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getMyEnrollments: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
      providers: [HomeService, { provide: HomeEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(HomeService);
  });

  it('delegates getMisEnrollments to the endpoint', async () => {
    const enrollments = [
      {
        enrollmentId: 100,
        offeringIds: [300],
        productId: 1,
        admissionProcessId: 4,
        intakeId: 2,
        shiftId: 3,
        degreeProgramName: 'Analista Programador',
        intakeName: 'Marzo 2027',
        shiftName: 'Noche',
        status: 'Confirmada',
      },
    ];
    endpointMock.getMyEnrollments.mockReturnValue(of(enrollments));

    await expect(firstValueFrom(service.getMyEnrollments())).resolves.toEqual(enrollments);
    expect(endpointMock.getMyEnrollments).toHaveBeenCalled();
  });
});
