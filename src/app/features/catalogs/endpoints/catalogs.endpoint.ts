import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import { CatalogRequestError, CatalogType } from '../models/catalog-error';
import {
  AdvertisingChoice,
  Baccalaureate,
  BaccalaureateYear,
  Country,
  Institution,
  ReasonForChoice,
  ScholarshipFund,
  ScholarshipProduct,
  University,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class CatalogsEndpoint {
  private readonly http = inject(HttpClient);

  private readonly baseUrl = this.resolveUrl('/Catalogos');

  public readonly countryUrl = `${this.baseUrl}/Pais`;
  public readonly reasonsUrl = `${this.baseUrl}/MotivosEleccion`;
  public readonly advertisingUrl = `${this.baseUrl}/PublicidadesEleccion`;
  public readonly baccalaureateUrl = `${this.baseUrl}/Bachilleratos`;
  public readonly baccalaureateYearUrl = `${this.baseUrl}/AnioBachiller`;
  public readonly institutionUrl = `${this.baseUrl}/Instituciones`;
  public readonly universityUrl = `${this.baseUrl}/Universidades`;
  public readonly scholarshipProductUrl = `${this.baseUrl}/ProductosBeca`;
  public readonly scholarshipFundUrl = `${this.baseUrl}/FondosDeBecaPorProducto`;

  public getCountries(): Observable<Country[]> {
    return this.http
      .get<Country[]>(this.countryUrl)
      .pipe(catchError(err => this.toRequestError('country', err)));
  }

  public getReasonsForChoice(): Observable<ReasonForChoice[]> {
    return this.http
      .get<ReasonForChoice[]>(this.reasonsUrl)
      .pipe(catchError(err => this.toRequestError('reasonForChoice', err)));
  }

  public getAdvertisingChoices(): Observable<AdvertisingChoice[]> {
    return this.http
      .get<AdvertisingChoice[]>(this.advertisingUrl)
      .pipe(catchError(err => this.toRequestError('advertisingChoice', err)));
  }

  public getBaccalaureates(): Observable<Baccalaureate[]> {
    return this.http
      .get<Baccalaureate[]>(this.baccalaureateUrl)
      .pipe(catchError(err => this.toRequestError('baccalaureate', err)));
  }

  public getBaccalaureateYears(): Observable<BaccalaureateYear[]> {
    return this.http
      .get<BaccalaureateYear[]>(this.baccalaureateYearUrl)
      .pipe(catchError(err => this.toRequestError('baccalaureateYear', err)));
  }

  public getInstitutions(): Observable<Institution[]> {
    return this.http
      .get<Institution[]>(this.institutionUrl)
      .pipe(catchError(err => this.toRequestError('institution', err)));
  }

  public getUniversities(): Observable<University[]> {
    return this.http
      .get<University[]>(this.universityUrl)
      .pipe(catchError(err => this.toRequestError('university', err)));
  }

  public getScholarshipProducts(): Observable<ScholarshipProduct[]> {
    return this.http
      .get<ScholarshipProduct[]>(this.scholarshipProductUrl)
      .pipe(catchError(err => this.toRequestError('scholarshipProduct', err)));
  }

  public getScholarshipFunds(): Observable<ScholarshipFund[]> {
    return this.http
      .get<ScholarshipFund[]>(this.scholarshipFundUrl)
      .pipe(catchError(err => this.toRequestError('scholarshipFund', err)));
  }

  private toRequestError(catalog: CatalogType, error: unknown): Observable<never> {
    const status = error instanceof HttpErrorResponse ? error.status : null;
    return throwError(() => new CatalogRequestError(catalog, status));
  }

  private resolveUrl(path: string): string {
    try {
      return new URL(path, environment.API_URL).toString();
    } catch {
      return path;
    }
  }
}

