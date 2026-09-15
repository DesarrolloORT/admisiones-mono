import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { ScholarshipAcademicData } from './scholarship-academic-data';

describe('ScholarshipAcademicData', () => {
  let service: ScholarshipAcademicData;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [ScholarshipAcademicData] });
    service = TestBed.inject(ScholarshipAcademicData);
  });

  // El adapter de becas todavia no expone este dato: hasta que exista, la fachada
  // del paso 1 tiene que poder trabajar con una lista vacia sin romperse.
  it('resolves to an empty list while the scholarships adapter has no data source', async () => {
    await expect(firstValueFrom(service.getAcademicStepData())).resolves.toEqual([]);
  });
});
