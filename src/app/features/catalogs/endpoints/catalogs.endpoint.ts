import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getCatalogosBancosEndpoint,
  getCatalogosCarrerasEndpoint,
  getCatalogosComienzosEndpoint,
  getCatalogosEncuestaInicialEndpoint,
  getCatalogosInstitucionesEndpoint,
  getCatalogosPaisesEstadosCiudadesEndpoint,
  getCatalogosTurnosEndpoint,
} from 'src/app/shared/api/generated/endpoints/catalogos.endpoints';
import type { DtoBachilleratoCatalogo } from 'src/app/shared/api/generated/models/dtoBachilleratoCatalogo';
import type { DtoCiudadResponse } from 'src/app/shared/api/generated/models/dtoCiudadResponse';
import type { DtoEstadoCiudadResponse } from 'src/app/shared/api/generated/models/dtoEstadoCiudadResponse';
import type { DtoPaisEstadoCiudadResponse } from 'src/app/shared/api/generated/models/dtoPaisEstadoCiudadResponse';

import {
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

  // TODO: getCatalogosTiposDocumentosEndpoint fue removido del API.
  // Reimplementar getDocumentTypes() cuando haya un endpoint de reemplazo.

  public getCountries(): Observable<Country[]> {
    return this.api.request(getCatalogosPaisesEstadosCiudadesEndpoint).pipe(
      map(data =>
        this.fromData(data, item => ({
          id: item.codigoPais ?? 0,
          label: item.nombre ?? '',
        }))
      )
    );
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api
      .request(getCatalogosPaisesEstadosCiudadesEndpoint)
      .pipe(map(data => this.fromData(data, item => this.toLocationCountry(item))));
  }

  public getCareers(): Observable<Career[]> {
    return this.api.request(getCatalogosCarrerasEndpoint).pipe(
      map(data =>
        this.fromData(data, item => ({
          idProducto: item.idProducto ?? 0,
          idNivelProducto: item.idNivelProducto ?? 0,
          nombreProducto: item.nombreProducto ?? '',
          nombreNivelProducto: item.nombreNivelProducto ?? '',
        }))
      )
    );
  }

  public getComienzos(idCarrera: number): Observable<Comienzo[]> {
    return this.api.request(getCatalogosComienzosEndpoint, { queryParams: { idCarrera } }).pipe(
      map(data =>
        this.fromData(data, item => ({
          idProceso: item.idProceso ?? 0,
          nombreProceso: item.nombreProceso ?? '',
        }))
      )
    );
  }

  public getInitialSurveyCatalogs(): Observable<InitialSurveyCatalogs> {
    return this.api.request(getCatalogosEncuestaInicialEndpoint).pipe(
      map(data => ({
        aniosAprobadosEducacionSuperior: this.toCatalogItems(data?.aniosAprobadosEducacionSuperior),
        compartidoCon: this.toCatalogItems(data?.compartidoCon),
        decisionCarrera: this.toCatalogItems(data?.decisionCarrera),
        decisionUniversidad: this.toCatalogItems(data?.decisionUniversidad),
        estadoEducacionSuperior: this.toCatalogItems(data?.estadoEducacionSuperior),
        formacionTutores: this.toCatalogItems(data?.formacionTutores),
        nivelConocimiento: this.toCatalogItems(data?.nivelConocimiento),
        motivosEleccion: (data?.motivosEleccion ?? []).map(item => ({
          id: item.idMotivo,
          label: item.nombreMotivo,
        })),
        publicidadesEleccion: (data?.publicidadesEleccion ?? []).map(item => ({
          id: item.idPublicidad,
          label: item.nombrePublicidad,
        })),
        universidades: (data?.universidades ?? []).map(item => ({
          id: item.codigoEmpresa ?? 0,
          label: item.nombre ?? '',
        })),
        aniosBachiller: (data?.aniosBachiller ?? []).map(year => this.toBaccalaureateYear(year)),
      }))
    );
  }

  public getBancos(): Observable<Bank[]> {
    return this.api.request(getCatalogosBancosEndpoint).pipe(
      map(data =>
        (data?.bancos ?? []).map(banco => ({
          id: banco.idBanco ?? 0,
          label: banco.nombreBanco ?? '',
          code: banco.codigo ?? null,
        }))
      )
    );
  }

  public getInstituciones(
    codigoPais: number,
    codigoEstado: number
  ): Observable<EducationalInstitution[]> {
    return this.api
      .request(getCatalogosInstitucionesEndpoint, { queryParams: { codigoPais, codigoEstado } })
      .pipe(
        map(data =>
          this.fromData(data, item => ({
            id: item.codigoEmpresa,
            label: item.nombre,
            codigoPais: item.codigoPais ?? null,
            codigoEstado: item.codigoEstado ?? null,
          }))
        )
      );
  }

  public getTurnos(idCarrera: number, idProceso: number): Observable<Turno[]> {
    return this.api
      .request(getCatalogosTurnosEndpoint, { queryParams: { idCarrera, idProceso } })
      .pipe(
        map(data =>
          this.fromData(data, item => ({
            idOferta: item.idOferta ?? 0,
            idTurno: item.turno?.idTurno ?? 0,
            nombreTurno: item.turno?.nombreTurno ?? '',
            horarioReferencia: item.horarioReferencia ?? '',
          }))
        )
      );
  }

  public clearCache(): void {
    this.api.clearCache();
  }

  private toLocationCountry(item: DtoPaisEstadoCiudadResponse): LocationCountry {
    return {
      codigoPais: item.codigoPais ?? 0,
      nombre: item.nombre ?? '',
      estado: item.estado?.map(s => this.toLocationState(s)) ?? null,
    };
  }

  private toLocationState(s: DtoEstadoCiudadResponse): LocationState {
    return {
      codigoPais: s.codigoPais ?? 0,
      codigoEstado: s.codigoEstado ?? 0,
      nombre: s.nombre ?? '',
      ciudad: s.ciudad?.map(c => this.toLocationCity(c)) ?? null,
    };
  }

  private toLocationCity(c: DtoCiudadResponse): LocationCity {
    return {
      codigoPais: c.codigoPais ?? 0,
      codigoEstado: c.codigoEstado ?? 0,
      codigoCiudad: c.codigoCiudad ?? 0,
      nombre: c.nombre ?? '',
    };
  }

  private toBaccalaureateYear(year: {
    idAnioBachiller?: number;
    nombreAnioBachiller?: string | null;
    bachilleratos?: DtoBachilleratoCatalogo[] | null;
  }): BaccalaureateYearGroup {
    return {
      id: year.idAnioBachiller ?? 0,
      label: year.nombreAnioBachiller ?? '',
      baccalaureates: (year.bachilleratos ?? []).map(item => ({
        id: item.codigoTitulo ?? 0,
        label: item.nombre ?? '',
        orientation: item.orientacionTitulo ?? null,
      })),
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
