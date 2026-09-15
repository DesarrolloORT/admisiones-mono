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
  CatalogItem,
  Country,
  DegreeProgram,
  EducationalInstitution,
  InitialSurveyCatalogs,
  Intake,
  LocationCity,
  LocationCountry,
  LocationState,
  Seminar,
  Shift,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class CatalogsApi {
  private readonly api = inject(ApiHttpClient);

  public getCountries(): Observable<Country[]> {
    return this.api.list(getCatalogsCountriesStatesCitiesEndpoint, item => ({
      id: item.countryId ?? 0,
      label: item.name ?? '',
    }));
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api.list(getCatalogsCountriesStatesCitiesEndpoint, item =>
      this.toLocationCountry(item)
    );
  }

  public getDegreePrograms(academicProposal: AcademicProposalTypeId): Observable<DegreeProgram[]> {
    return this.api
      .list(getCatalogsDegreeProgramsEndpoint, level => this.toDegreePrograms(level), {
        queryParams: { academicOffer: academicProposal },
      })
      .pipe(map(levels => levels.flat()));
  }

  public getIntakes(degreeProgramId: number): Observable<Intake[]> {
    return this.api.list(
      getCatalogsIntakesEndpoint,
      item => ({
        admissionProcessId: item.admissionProcessId ?? 0,
        admissionProcessName: item.admissionProcessName ?? '',
      }),
      { queryParams: { degreeProgramId: degreeProgramId } }
    );
  }

  public getInitialSurveyCatalogs(): Observable<InitialSurveyCatalogs> {
    return this.api.request(getCatalogsInitialSurveyEndpoint).pipe(
      map(data => ({
        education: {
          lastSecondaryYearLocations: this.toCatalogItems(
            data?.education?.lastSecondaryYearLocations
          ),
          highSchoolYears: (data?.education?.highSchoolYears ?? []).map(year =>
            this.toBaccalaureateYear(year)
          ),
          previousHigherEducationOptions: this.toCatalogItems(
            data?.education?.previousHigherEducationOptions
          ),
          universities: this.toCatalogItems(data?.education?.universities),
          guardianEducationLevels: this.toCatalogItems(data?.education?.educationLevels),
        },
        academicDecision: {
          upperSecondaryYears: this.toCatalogItems(data?.academicDecision?.upperSecondaryYears),
          decisionSupports: this.toCatalogItems(data?.academicDecision?.decisionSupports),
          decisionLevels: this.toCatalogItems(data?.academicDecision?.decisionLevels),
          universities: this.toCatalogItems(data?.academicDecision?.universities),
          ortChoiceReasons: this.toCatalogItems(data?.academicDecision?.ortChoiceReasons),
        },
        ortExperience: {
          ratings: this.toCatalogItems(data?.ortExperience?.ratings),
          ortAdvertisements: this.toCatalogItems(data?.ortExperience?.ortAdvertisements),
        },
      }))
    );
  }

  public getBanks(): Observable<Bank[]> {
    return this.api.list(getCatalogsBanksEndpoint, bank => ({
      id: bank.id ?? 0,
      label: bank.name ?? '',
      code: bank.sistarbancBankId ?? bank.code?.toString() ?? null,
    }));
  }

  public getInstitutions(
    countryCode: number,
    stateCode: number
  ): Observable<EducationalInstitution[]> {
    return this.api.list(
      getCatalogsInstitutionsEndpoint,
      item => ({
        id: item.id ?? 0,
        label: item.name ?? '',
        countryCode,
        stateCode,
      }),
      { queryParams: { countryId: countryCode, stateId: stateCode } }
    );
  }

  public getShifts(degreeProgramId: number, admissionProcessId: number): Observable<Shift[]> {
    return this.api.list(
      getCatalogsShiftsEndpoint,
      item => ({
        offeringId: item.offeringId ?? 0,
        shiftId: item.shift?.shiftId ?? 0,
        shiftName: item.shift?.shiftName ?? '',
        referenceSchedule: item.referenceSchedule ?? '',
        offeringDescription: item.offeringDescription ?? '',
        referenceDate: item.referenceDate ?? null,
      }),
      {
        queryParams: { degreeProgramId: degreeProgramId, admissionProcessId: admissionProcessId },
      }
    );
  }

  public getSeminars(degreeProgramId: number, admissionProcessId: number): Observable<Seminar[]> {
    return this.getShifts(degreeProgramId, admissionProcessId).pipe(
      map(shifts =>
        shifts.map(shift => ({
          offeringId: shift.offeringId,
          admissionProcessId: admissionProcessId,
          name: shift.offeringDescription,
          startDate: shift.referenceDate,
        }))
      )
    );
  }

  private toDegreePrograms(level: DegreeProgramsByLevelResponse): DegreeProgram[] {
    return (level.schools ?? []).flatMap(school => {
      const groups = school.seminars?.length
        ? school.seminars
        : [{ hasSeminar: false, products: school.products }];

      return groups.flatMap(group =>
        (group.products ?? []).map(product => ({
          productId: product.productId ?? 0,
          admissionProcessId: product.admissionProcessId ?? null,
          productLevelId: level.productLevelId ?? 0,
          productName: product.productName ?? '',
          productLevelName: level.productLevelName ?? '',
          schoolName: school.schoolName ?? '',
          hasSeminar: group.hasSeminar ?? false,
        }))
      );
    });
  }

  private toLocationCountry(item: CountryStateCityResponse): LocationCountry {
    return {
      countryCode: item.countryId ?? 0,
      name: item.name ?? '',
      states: item.states?.map(s => this.toLocationState(s)) ?? null,
    };
  }

  private toLocationState(s: StateWithCitiesResponse): LocationState {
    return {
      countryCode: s.countryId ?? 0,
      stateCode: s.stateId ?? 0,
      name: s.name ?? '',
      cities: s.cities?.map(c => this.toLocationCity(c)) ?? null,
    };
  }

  private toLocationCity(c: CityResponse): LocationCity {
    return {
      countryCode: c.countryId ?? 0,
      stateCode: c.stateId ?? 0,
      cityCode: c.cityId ?? 0,
      name: c.name ?? '',
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
      orientation: item.label ?? item.newTrack ?? null,
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
}
