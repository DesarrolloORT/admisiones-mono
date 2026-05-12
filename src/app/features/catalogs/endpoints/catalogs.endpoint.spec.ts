import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';

import { CatalogRequestError } from '../models/catalog-error';
import { CatalogsEndpoint } from './catalogs.endpoint';

describe('CatalogsEndpoint', () => {
  let endpoint: CatalogsEndpoint;
  let httpController: HttpTestingController;

  const baseUrl = new URL('/Catalogos', environment.API_URL).toString();

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CatalogsEndpoint],
    });

    endpoint = TestBed.inject(CatalogsEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should expose resolved endpoint urls', () => {
    expect(endpoint.countryUrl).toBe(`${baseUrl}/Pais`);
    expect(endpoint.reasonsUrl).toBe(`${baseUrl}/MotivosEleccion`);
    expect(endpoint.advertisingUrl).toBe(`${baseUrl}/PublicidadesEleccion`);
    expect(endpoint.baccalaureateUrl).toBe(`${baseUrl}/Bachilleratos`);
    expect(endpoint.baccalaureateYearUrl).toBe(`${baseUrl}/AnioBachiller`);
    expect(endpoint.institutionUrl).toBe(`${baseUrl}/Instituciones`);
    expect(endpoint.universityUrl).toBe(`${baseUrl}/Universidades`);
    expect(endpoint.scholarshipProductUrl).toBe(`${baseUrl}/ProductosBeca`);
    expect(endpoint.scholarshipFundUrl).toBe(`${baseUrl}/FondosDeBecaPorProducto`);
  });

  it('should fetch countries', () => {
    const mockData = [
      { id: 1, label: 'Uruguay', code: 'UY' },
      { id: 2, label: 'Argentina', code: 'AR' },
    ];

    endpoint.getCountries().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.countryUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch reasons for choice', () => {
    const mockData = [{ id: 1, label: 'Recomendación' }];

    endpoint.getReasonsForChoice().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.reasonsUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch advertising choices', () => {
    const mockData = [{ id: 1, label: 'Redes sociales' }];

    endpoint.getAdvertisingChoices().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.advertisingUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch baccalaureates', () => {
    const mockData = [{ id: 1, label: 'Científico' }];

    endpoint.getBaccalaureates().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.baccalaureateUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch baccalaureate years', () => {
    const mockData = [{ id: 1, label: '2024', year: 2024 }];

    endpoint.getBaccalaureateYears().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.baccalaureateYearUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch institutions', () => {
    const mockData = [{ id: 1, label: 'Liceo 1', country: 'Uruguay' }];

    endpoint.getInstitutions().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.institutionUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch universities', () => {
    const mockData = [{ id: 1, label: 'ORT Uruguay', country: 'Uruguay' }];

    endpoint.getUniversities().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.universityUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch scholarship products', () => {
    const mockData = [{ id: 1, label: 'Beca Excelencia' }];

    endpoint.getScholarshipProducts().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.scholarshipProductUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should fetch scholarship funds', () => {
    const mockData = [{ id: 1, label: 'Fondo ORT', productId: 1 }];

    endpoint.getScholarshipFunds().subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const request = httpController.expectOne(endpoint.scholarshipFundUrl);
    expect(request.request.method).toBe('GET');
    request.flush(mockData);
  });

  it('should throw CatalogRequestError on HTTP error', () => {
    endpoint.getCountries().subscribe({
      error: (err: CatalogRequestError) => {
        expect(err).toBeInstanceOf(CatalogRequestError);
        expect(err.catalog).toBe('country');
        expect(err.status).toBe(500);
      },
    });

    const request = httpController.expectOne(endpoint.countryUrl);
    request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
  });
});

