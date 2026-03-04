using AppLogic.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using Utilities;

namespace AppLogic.Services
{
    public class GeneralServices : IGeneralServices
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public GeneralServices(
             IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        #region CONSULTAS GENERALES

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Paises.GetAll().ToList();
            var sorted = entidades
                .OrderBy(p => p.CodigoPais == 1 ? 0 : 1)
                .ThenBy(p => p.Nombre)
                .ToList();
            var dtoList = sorted.ToDtos().ToList();
            return OperationResult<IEnumerable<DtoPaisDevart>>.Ok(dtoList, nameof(ObtenerPaises));
        }

        public OperationResult<DtoPaisDevart> ObtenerPais(long idPais)
        {
            using var uow = _uowFactory.Create();
            var pais = uow.Paises.GetPaisAndCiudadesByKey(idPais);
            if (pais == null)
                return OperationResult<DtoPaisDevart>.IsFailed("FDP_GPAC_01", nameof(ObtenerPais), "País no encontrado.", 204);

            if (pais.Estado != null)
            {
                pais.Estado = pais.Estado.OrderBy(e => e.Nombre).ToList();
                foreach (var estado in pais.Estado)
                {
                    if (estado.Ciudad != null)
                    {
                        estado.Ciudad = estado.Ciudad.OrderBy(c => c.Nombre).ToList();
                    }
                }
            }

            return OperationResult<DtoPaisDevart>.Ok(pais.ToDtoWithRelated(2), nameof(ObtenerPais));
        }

        public OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.AcaTipoDocumentos.GetAll().ToList();
            return OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>.Ok(AcaTipoDocumentoConverter.ToDtos(entidades), nameof(ObtenerTipoDocumentos));
        }

        #endregion CONSULTAS GENERALES

        #region INTERES, PRODUCTOS, PROCESOS HABILITADOS

        public OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Procesos.GetProcesosHabilitadosPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoProcesoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerProcesosHabilitadosPorProducto));
        }

        public OperationResult<DtoInscriptoDevart> ObtenerUltimaInscripcion(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcion(codigoPersona);
            if (inscripto == null)
                return OperationResult<DtoInscriptoDevart>.IsFailed("GEN_UI_01", nameof(ObtenerUltimaInscripcion), "No se encontró inscripción para la persona.", 204);

            return OperationResult<DtoInscriptoDevart>.Ok(inscripto.ToDto(), nameof(ObtenerUltimaInscripcion));
        }

        #endregion INTERES, PRODUCTOS, PROCESOS HABILITADOS

        #region PERSONA

        public OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona == null)
                return OperationResult<DtoPersonaDevart>.IsFailed("GEN_PER_01", nameof(ObtenerPersona), "Persona no encontrada.", 404);

            return OperationResult<DtoPersonaDevart>.Ok(persona.ToDto(), nameof(ObtenerPersona));
        }

        #endregion PERSONA

        #region ENCUESTA

        public OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerDatosPreInscripcion(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaIniAdmisionDevart>.IsFailed("GEN_DPI_01", nameof(ObtenerDatosPreInscripcion), "No se encontraron datos de pre-inscripción para la persona.", 204);

            return OperationResult<DtoEncuestaIniAdmisionDevart>.Ok(encuesta.ToDto(), nameof(ObtenerDatosPreInscripcion));
        }

        public OperationResult<DateTime?> ObtenerFechaVtoAdmisiones(long codigoPersona, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var fechaVto = uow.EncuestaIniAdmisions.GetFechaVtoAdmisiones(codigoPersona, idProceso);
            return OperationResult<DateTime?>.Ok(fechaVto, nameof(ObtenerFechaVtoAdmisiones));
        }

        public OperationResult<IEnumerable<DtoTurnoDevart>> ObtenerTurnos(long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Turnos.GetTurnosParaAdmisiones(idProducto, idProceso);
            return OperationResult<IEnumerable<DtoTurnoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerTurnos));
        }

        public OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>> ObtenerMotivosEleccion()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.MotivoOpcionesAdmisions.GetAll().ToList();
            return OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerMotivosEleccion));
        }

        public OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>> ObtenerPublicidadesEleccion()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.PublicidadOpcionesAdmisions.GetAll().ToList();
            return OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerPublicidadesEleccion));
        }

        #endregion ENCUESTA

        #region BACHILLERATOS Y UNIVERSIDADES

        public OperationResult<IEnumerable<DtoTituloDevart>> ObtenerBachilleratos(long idAnioBachillerato)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Titulos.GetBachilleratosPorAnio(idAnioBachillerato);
            return OperationResult<IEnumerable<DtoTituloDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerBachilleratos));
        }

        public OperationResult<DtoAnioBachillerDevart> ObtenerAnioBachiller(long idAnioBachillerato)
        {
            using var uow = _uowFactory.Create();
            var anio = uow.AnioBachillers.GetWithRelated(idAnioBachillerato);
            if (anio == null)
                return OperationResult<DtoAnioBachillerDevart>.IsFailed("GEN_ANB_01", nameof(ObtenerAnioBachiller), "Año de bachillerato no encontrado.", 204);

            return OperationResult<DtoAnioBachillerDevart>.Ok(anio.ToDto(), nameof(ObtenerAnioBachiller));
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetInstituciones(codigoPais, codigoEstado);
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerInstituciones));
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetUniversidades();
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerUniversidades));
        }

        #endregion BACHILLERATOS Y UNIVERSIDADES

        #region POSTULACION A BECAS

        public OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(
            long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Pruebas.GetPruebasVigentesParaAdmisiones(idProducto, idProceso);
            return OperationResult<IEnumerable<DtoPruebaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaVigentes));
        }

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorNivel(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorNivel));
        }

        #endregion POSTULACION A BECAS

        #region INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        public OperationResult<DtoAceptacionReglamentoEstDevart> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidad = uow.AceptacionReglamentoEsts.GetByPersona(codigoPersona);
            if (entidad == null)
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed("GEN_ARE_01", nameof(ObtenerAceptacionReglamentoEstudiantil), "No se encontró aceptación del reglamento para la persona.", 204);

            return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(entidad.ToDto(), nameof(ObtenerAceptacionReglamentoEstudiantil));
        }

        public OperationResult<IEnumerable<DtoProductoDevart>> ObtenerProductoInteresPersona(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductoInteresPersona(codigoPersona);
            return OperationResult<IEnumerable<DtoProductoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerProductoInteresPersona));
        }

        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS
    }
}

