import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { shareReplay } from 'rxjs/operators';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import {
  Baccalaureate,
  BaccalaureateYear,
  Country,
  Institution,
  ReasonForChoice,
  AdvertisingChoice,
  ScholarshipFund,
  ScholarshipProduct,
  University,
} from '../models/catalog.interface';

/**
 * Servicio de catálogos con cache en memoria.
 *
 * Cada getter retorna un Observable cacheado con shareReplay(1).
 * El cache vive mientras el servicio esté instanciado (root).
 * Si necesitás invalidar, llamá a `clearCache()`.
 */
@Injectable({
  providedIn: 'root',
})
export class Catalogs {
  private readonly endpoint = inject(CatalogsEndpoint);

  private countriesCache$?: Observable<Country[]>;
  private reasonsCache$?: Observable<ReasonForChoice[]>;
  private advertisingCache$?: Observable<AdvertisingChoice[]>;
  private baccalaureatesCache$?: Observable<Baccalaureate[]>;
  private baccalaureateYearsCache$?: Observable<BaccalaureateYear[]>;
  private institutionsCache$?: Observable<Institution[]>;
  private universitiesCache$?: Observable<University[]>;
  private scholarshipProductsCache$?: Observable<ScholarshipProduct[]>;
  private scholarshipFundsCache$?: Observable<ScholarshipFund[]>;

  public getCountries(): Observable<Country[]> {
    return (this.countriesCache$ ??= this.endpoint.getCountries().pipe(shareReplay(1)));
  }

  public getReasonsForChoice(): Observable<ReasonForChoice[]> {
    return (this.reasonsCache$ ??= this.endpoint.getReasonsForChoice().pipe(shareReplay(1)));
  }

  public getAdvertisingChoices(): Observable<AdvertisingChoice[]> {
    return (this.advertisingCache$ ??= this.endpoint.getAdvertisingChoices().pipe(shareReplay(1)));
  }

  public getBaccalaureates(): Observable<Baccalaureate[]> {
    return (this.baccalaureatesCache$ ??= this.endpoint.getBaccalaureates().pipe(shareReplay(1)));
  }

  public getBaccalaureateYears(): Observable<BaccalaureateYear[]> {
    return (this.baccalaureateYearsCache$ ??= this.endpoint
      .getBaccalaureateYears()
      .pipe(shareReplay(1)));
  }

  public getInstitutions(): Observable<Institution[]> {
    return (this.institutionsCache$ ??= this.endpoint.getInstitutions().pipe(shareReplay(1)));
  }

  public getUniversities(): Observable<University[]> {
    return (this.universitiesCache$ ??= this.endpoint.getUniversities().pipe(shareReplay(1)));
  }

  public getScholarshipProducts(): Observable<ScholarshipProduct[]> {
    return (this.scholarshipProductsCache$ ??= this.endpoint
      .getScholarshipProducts()
      .pipe(shareReplay(1)));
  }

  public getScholarshipFunds(): Observable<ScholarshipFund[]> {
    return (this.scholarshipFundsCache$ ??= this.endpoint
      .getScholarshipFunds()
      .pipe(shareReplay(1)));
  }

  /** Invalida todos los caches. La próxima llamada hará fetch fresco. */
  public clearCache(): void {
    this.countriesCache$ = undefined;
    this.reasonsCache$ = undefined;
    this.advertisingCache$ = undefined;
    this.baccalaureatesCache$ = undefined;
    this.baccalaureateYearsCache$ = undefined;
    this.institutionsCache$ = undefined;
    this.universitiesCache$ = undefined;
    this.scholarshipProductsCache$ = undefined;
    this.scholarshipFundsCache$ = undefined;
  }
}
