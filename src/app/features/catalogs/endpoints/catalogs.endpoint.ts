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
import type { DtoAnioBachilleratoCatalogo } from 'src/app/shared/api/generated/models/dtoAnioBachilleratoCatalogo';
import type { DtoBachilleratoCatalogo } from 'src/app/shared/api/generated/models/dtoBachilleratoCatalogo';
import type { DtoCiudadResponse } from 'src/app/shared/api/generated/models/dtoCiudadResponse';
import type { DtoEstadoCiudadResponse } from 'src/app/shared/api/generated/models/dtoEstadoCiudadResponse';
import type { DtoPaisEstadoCiudadResponse } from 'src/app/shared/api/generated/models/dtoPaisEstadoCiudadResponse';

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
        this.fromData(data, nivel =>
          (nivel.escuelas ?? []).flatMap(escuela =>
            (escuela.productos ?? []).map(producto => ({
              idProducto: producto.idProducto ?? 0,
              idNivelProducto: nivel.idNivelProducto ?? 0,
              nombreProducto: producto.nombreProducto ?? '',
              nombreNivelProducto: nivel.nombreNivelProducto ?? '',
              nombreEscuela: escuela.nombreEscuela ?? '',
            }))
          )
        ).flat()
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
        educacion: {
          ubicacionesUltimoAnioSecundaria: this.toCatalogItems(
            data?.educacion?.ubicacionesUltimoAnioSecundaria
          ),
          aniosBachillerato: (data?.educacion?.aniosBachillerato ?? []).map(year =>
            this.toBaccalaureateYear(year)
          ),
          estadosEducacionSuperiorPrevia: this.toCatalogItems(
            data?.educacion?.estadosEducacionSuperiorPrevia
          ),
          universidades: this.toCatalogItems(data?.educacion?.universidades),
          nivelesFormacionTutores: this.toCatalogItems(data?.educacion?.nivelesFormacionTutores),
        },
        decisionAcademica: {
          aniosEducacionMediaSuperior: this.toCatalogItems(
            data?.decisionAcademica?.aniosEducacionMediaSuperior
          ),
          apoyosDecision: this.toCatalogItems(data?.decisionAcademica?.apoyosDecision),
          nivelesDecision: this.toCatalogItems(data?.decisionAcademica?.nivelesDecision),
          universidades: this.toCatalogItems(data?.decisionAcademica?.universidades),
          motivosEleccionOrt: this.toCatalogItems(data?.decisionAcademica?.motivosEleccionOrt),
        },
        experienciaOrt: {
          valoraciones: this.toCatalogItems(data?.experienciaOrt?.valoraciones),
          publicidadesOrt: this.toCatalogItems(data?.experienciaOrt?.publicidadesOrt),
        },
        situacionLaboral: {
          tiposJornada: this.toCatalogItems(data?.situacionLaboral?.tiposJornada),
        },
      }))
    );
  }

  public getBancos(): Observable<Bank[]> {
    return this.api.request(getCatalogosBancosEndpoint).pipe(
      map(data =>
        this.fromData(data, banco => ({
          id: banco.idBanco ?? 0,
          label: banco.nombreBanco ?? '',
          code: banco.idBancoSistarbanc ?? banco.codigoBanco?.toString() ?? null,
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

  private toBaccalaureateYear(year: DtoAnioBachilleratoCatalogo): BaccalaureateYearGroup {
    return {
      id: year.value ?? 0,
      label: year.label ?? '',
      baccalaureates: (year.orientaciones ?? []).map(item => this.toBaccalaureate(item)),
    };
  }

  private toBaccalaureate(item: DtoBachilleratoCatalogo): BaccalaureateOption {
    return {
      id: item.value ?? 0,
      label: item.label ?? '',
      orientation: item.orientacion ?? item.orientacionNueva ?? null,
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
