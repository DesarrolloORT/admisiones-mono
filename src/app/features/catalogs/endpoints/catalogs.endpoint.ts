import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getCatalogsBanksEndpoint,
  getCatalogsCountriesStatesCitiesEndpoint,
  getCatalogsDegreeProgramsEndpoint,
  getCatalogsInitialSurveyEndpoint,
  getCatalogsInstitutionsEndpoint,
  getCatalogsIntakesEndpoint,
  getCatalogsShiftsEndpoint,
} from 'src/app/shared/api/generated/endpoints/catalogs.endpoints';
import type { CityResponse } from 'src/app/shared/api/generated/models/cityResponse';
import type { CountryStateCityResponse } from 'src/app/shared/api/generated/models/countryStateCityResponse';
import type { DegreeProgramsByLevelResponse } from 'src/app/shared/api/generated/models/degreeProgramsByLevelResponse';
import type { HighSchoolTrackCatalog } from 'src/app/shared/api/generated/models/highSchoolTrackCatalog';
import type { HighSchoolYearCatalog } from 'src/app/shared/api/generated/models/highSchoolYearCatalog';
import type { StateWithCitiesResponse } from 'src/app/shared/api/generated/models/stateWithCitiesResponse';

import type { AcademicProposalTypeId } from '../models/academic-proposal';
import {
  BaccalaureateOption,
  BaccalaureateYearGroup,
  Bank,
  Career,
  CatalogItem,
  Comienzo,
  Country,
  EducationalInstitution,
  InitialSurveyCatalogs,
  LocationCity,
  LocationCountry,
  LocationState,
  Turno,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class CatalogsEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getCountries(): Observable<Country[]> {
    return this.api.request(getCatalogsCountriesStatesCitiesEndpoint).pipe(
      map(data =>
        this.fromData(data, item => ({
          id: item.countryId ?? 0,
          label: item.name ?? '',
        }))
      )
    );
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api
      .request(getCatalogsCountriesStatesCitiesEndpoint)
      .pipe(map(data => this.fromData(data, item => this.toLocationCountry(item))));
  }

  public getCareers(propuestaAcademica: AcademicProposalTypeId): Observable<Career[]> {
    return this.api
      .request(getCatalogsDegreeProgramsEndpoint, {
        queryParams: { academicOffer: propuestaAcademica },
      })
      .pipe(map(data => this.fromData(data, nivel => this.toCareers(nivel)).flat()));
  }

  public getComienzos(idCarrera: number): Observable<Comienzo[]> {
    return this.api
      .request(getCatalogsIntakesEndpoint, {
        queryParams: { degreeProgramId: idCarrera },
      })
      .pipe(
        map(data =>
          this.fromData(data, item => ({
            idProceso: item.admissionProcessId ?? 0,
            nombreProceso: item.admissionProcessName ?? '',
          }))
        )
      );
  }

  public getInitialSurveyCatalogs(): Observable<InitialSurveyCatalogs> {
    return this.api.request(getCatalogsInitialSurveyEndpoint).pipe(
      map(data => ({
        educacion: {
          ubicacionesUltimoAnioSecundaria: this.toCatalogItems(
            data?.education?.lastSecondaryYearLocations
          ),
          aniosBachillerato: (data?.education?.highSchoolYears ?? []).map(year =>
            this.toBaccalaureateYear(year)
          ),
          estadosEducacionSuperiorPrevia: this.toCatalogItems(
            data?.education?.previousHigherEducationOptions
          ),
          universidades: this.toCatalogItems(data?.education?.universities),
          nivelesFormacionTutores: this.toCatalogItems(data?.education?.educationLevels),
        },
        decisionAcademica: {
          aniosEducacionMediaSuperior: this.toCatalogItems(
            data?.academicDecision?.upperSecondaryYears
          ),
          apoyosDecision: this.toCatalogItems(data?.academicDecision?.decisionSupports),
          nivelesDecision: this.toCatalogItems(data?.academicDecision?.decisionLevels),
          universidades: this.toCatalogItems(data?.academicDecision?.universities),
          motivosEleccionOrt: this.toCatalogItems(data?.academicDecision?.ortChoiceReasons),
        },
        experienciaOrt: {
          valoraciones: this.toCatalogItems(data?.ortExperience?.ratings),
          publicidadesOrt: this.toCatalogItems(data?.ortExperience?.ortAdvertisements),
        },
      }))
    );
  }

  public getBancos(): Observable<Bank[]> {
    return this.api.request(getCatalogsBanksEndpoint).pipe(
      map(data =>
        this.fromData(data, banco => ({
          id: banco.id ?? 0,
          label: banco.name ?? '',
          code: banco.sistarbancBankId ?? banco.code?.toString() ?? null,
        }))
      )
    );
  }

  public getInstituciones(
    codigoPais: number,
    codigoEstado: number
  ): Observable<EducationalInstitution[]> {
    return this.api
      .request(getCatalogsInstitutionsEndpoint, {
        queryParams: { countryId: codigoPais, stateId: codigoEstado },
      })
      .pipe(
        map(data =>
          this.fromData(data, item => ({
            id: item.id ?? 0,
            label: item.name ?? '',
            codigoPais,
            codigoEstado,
          }))
        )
      );
  }

  public getTurnos(idCarrera: number, idProceso: number): Observable<Turno[]> {
    return this.api
      .request(getCatalogsShiftsEndpoint, {
        queryParams: { degreeProgramId: idCarrera, admissionProcessId: idProceso },
      })
      .pipe(
        map(data =>
          this.fromData(data, item => ({
            idOferta: item.offeringId ?? 0,
            idTurno: item.shift?.shiftId ?? 0,
            nombreTurno: item.shift?.shiftName ?? '',
            horarioReferencia: item.referenceSchedule ?? '',
            descripcionOferta: item.offeringDescription ?? '',
            fechaReferencia: item.referenceDate ?? null,
          }))
        )
      );
  }

  public clearCache(): void {
    this.api.clearCache();
  }

  private toCareers(nivel: DegreeProgramsByLevelResponse): Career[] {
    return (nivel.schools ?? []).flatMap(escuela => {
      const groups = escuela.seminars?.length
        ? escuela.seminars
        : [{ hasSeminar: false, products: escuela.products }];

      return groups.flatMap(group =>
        (group.products ?? []).map(producto => ({
          idProducto: producto.productId ?? 0,
          idProceso: producto.admissionProcessId ?? null,
          idNivelProducto: nivel.productLevelId ?? 0,
          nombreProducto: producto.productName ?? '',
          nombreNivelProducto: nivel.productLevelName ?? '',
          nombreEscuela: escuela.schoolName ?? '',
          tieneSeminario: group.hasSeminar ?? false,
        }))
      );
    });
  }

  private toLocationCountry(item: CountryStateCityResponse): LocationCountry {
    return {
      codigoPais: item.countryId ?? 0,
      nombre: item.name ?? '',
      estado: item.states?.map(s => this.toLocationState(s)) ?? null,
    };
  }

  private toLocationState(s: StateWithCitiesResponse): LocationState {
    return {
      codigoPais: s.countryId ?? 0,
      codigoEstado: s.stateId ?? 0,
      nombre: s.name ?? '',
      ciudad: s.cities?.map(c => this.toLocationCity(c)) ?? null,
    };
  }

  private toLocationCity(c: CityResponse): LocationCity {
    return {
      codigoPais: c.countryId ?? 0,
      codigoEstado: c.stateId ?? 0,
      codigoCiudad: c.cityId ?? 0,
      nombre: c.name ?? '',
    };
  }

  private toBaccalaureateYear(year: HighSchoolYearCatalog): BaccalaureateYearGroup {
    return {
      id: year.value ?? 0,
      label: year.label ?? '',
      baccalaureates: (year.tracks ?? []).map(item => this.toBaccalaureate(item)),
    };
  }

  private toBaccalaureate(item: HighSchoolTrackCatalog): BaccalaureateOption {
    return {
      id: item.value ?? 0,
      label: item.label ?? '',
      orientation: item.track ?? item.newTrack ?? null,
    };
  }

  private toCatalogItems(
    data: Array<{ value?: number; label?: string | null }> | null | undefined
  ): CatalogItem[] {
    return (data ?? []).map(item => ({
      id: item.value ?? '',
      label: item.label ?? '',
    }));
  }

  private fromData<TItem, TResult>(
    data: TItem | TItem[] | null | undefined,
    mapper: (item: TItem) => TResult
  ): TResult[] {
    if (!data) {
      return [];
    }

    return (Array.isArray(data) ? data : [data]).map(mapper);
  }
}
