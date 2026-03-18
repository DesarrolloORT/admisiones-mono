using AppLogic.Interfaces;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using ConnectionContext;
using Utilities;
using AppLogic.Utilities;

namespace AppLogic.Services
{
    public class GeneralServices : IGeneralServices
    {
        private static readonly List<string> AllowedDocumentExtensions =
        [
            ".pdf", ".jpg", ".jpeg", ".png"
        ];

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;

        public GeneralServices(
             IUnitOfWorkFactory uowFactory,
             IDbConnectionContext dbConnectionContext)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
        }

        #region CONSULTAS GENERALES

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises()
        {
            using var uow = _uowFactory.Create();
            var dtoList = uow.Paises.GetPaisesOrdenados().ToDtos().ToList();
            return OperationResult<IEnumerable<DtoPaisDevart>>.Ok(dtoList, nameof(ObtenerPaises));
        }

        public OperationResult<DtoPaisDevart> ObtenerPais(long idPais)
        {
            using var uow = _uowFactory.Create();
            var pais = uow.Paises.GetPaisConEstadosYCiudades(idPais);
            if (pais == null)
                return OperationResult<DtoPaisDevart>.IsFailed("FDP_GPAC_01", nameof(ObtenerPais), "País no encontrado.", 404);

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

        public OperationResult<DTOUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcionActiva(codigoPersona);
            if (inscripto == null)
                return OperationResult<DTOUltimaInscripcion>.IsFailed("GEN_UI_01", nameof(ObtenerUltimaInscripcionActiva), "No se encontró inscripción para la persona.", 204);

            var dto = new DTOUltimaInscripcion
            {
                IdInscripto    = inscripto.IdInscripto,
                IdProducto     = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreProducto,
                NombreExtensoProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo     = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
            };
            return OperationResult<DTOUltimaInscripcion>.Ok(dto, nameof(ObtenerUltimaInscripcionActiva));
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

        public OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaIniAdmisionDevart>.IsFailed("GEN_DPI_01", nameof(ObtenerEncuestaInicialAdmision), "No se encontraron datos de pre-inscripción para la persona.", 204);

            return OperationResult<DtoEncuestaIniAdmisionDevart>.Ok(encuesta.ToDtoWithRelated(1), nameof(ObtenerEncuestaInicialAdmision));
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

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorProducto));
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

        public OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosConInteresActivo(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DTOProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosConInteresActivo));
        }

        public OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentesConInteres(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DTOProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosVigentesConInteres));
        }

        private static DTOProductoAdmisiones MapProductoAdmisiones(BusinessLogic.Entities.Producto p)
        {
            var proceso = p.ProcesoProductos?.FirstOrDefault()?.Proceso;
            return new DTOProductoAdmisiones
            {
                IdProducto            = p.IdProducto,
                NombreProducto        = p.NombreProducto,
                NombreExtensoProducto = p.NombreExtensoProducto,
                IdNivelProducto       = p.IdNivelProducto,
                NombreNivelProducto   = p.NivelProducto?.NombreNivelProducto,
                AliasProducto         = p.AliasProducto,
                InscribibleProducto   = p.InscribibleProducto,
                IntermedioProducto    = p.IntermedioProducto,
                VisibleAdmisionesProducto = p.VisibleAdmisionesProducto,
                IdProceso             = proceso?.IdProceso ?? 0,
                NombreProceso         = proceso?.NombreProceso,
            };
        }

        public OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.VdEsFrescoAdmisions.TieneInscripcionActivaParaProceso(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionActivaParaProceso));
        }

        public OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesPendientes(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var dtos = instancias.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesDict.TryGetValue(iw.IdInstanciaWorkflow, out var iwi))
                    dto.InstWorkflowInscripcion = iwi.ToDto();
                return dto;
            });

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesPendientes));
        }

        public OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var instancias = uow.InstanciaWorkflows.GetInscripcionesCanceladas(codigoPersona);
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var dtos = instancias.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesDict.TryGetValue(iw.IdInstanciaWorkflow, out var iwi))
                    dto.InstWorkflowInscripcion = iwi.ToDto();
                return dto;
            });

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesCanceladas));
        }

        public OperationResult<IEnumerable<DtoOfertaDevart>> ObtenerOfertasParaInscripcionConProceso(long idProducto, long idProceso, long idTurno)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Ofertas.GetOfertasParaInscripcionConProceso(idProducto, idProceso, idTurno);
            return OperationResult<IEnumerable<DtoOfertaDevart>>.Ok(entidades.ToDtosWithRelated(2), nameof(ObtenerOfertasParaInscripcionConProceso));
        }

        public OperationResult<IEnumerable<DTOInscripcionRealizada>> ObtenerInscripcionesRealizadas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscriptos = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona);
            var dtos = inscriptos
                .Select(i => new DTOInscripcionRealizada
                {
                    FechaInscripcion = i.FechaInscr ?? DateTime.MinValue,
                    IdProducto       = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                    NombreProducto   = i.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                    NombreComienzo   = i.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
                    NombreTurno      = i.Oferta?.Turno?.NombreTurno,
                })
                .GroupBy(d => d.IdProducto)
                .Select(g => g.OrderBy(d => d.FechaInscripcion).First())
                .ToList();
            return OperationResult<IEnumerable<DTOInscripcionRealizada>>.Ok(dtos, nameof(ObtenerInscripcionesRealizadas));
        }

        public OperationResult<IEnumerable<DTOProductoBeca>> ObtenerProductosBeca(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            // 1. Inscripciones realizadas (T_INSCRIPTO)
            var realizadas = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona)
                .Select(i => new DTOProductoBeca
                {
                    FechaInscripcion = i.FechaInscr ?? DateTime.MinValue,
                    IdProducto       = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                    IdNivelProducto  = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdNivelProducto ?? 0,
                    NombreProducto   = i.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                    NombreComienzo   = i.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
                    NombreTurno      = i.Oferta?.Turno?.NombreTurno,
                    IdProceso        = i.Oferta?.Supraoferta?.Comienzo?.ProcesoComienzos?
                                           .FirstOrDefault()?.IdProceso ?? 0,
                })
                .ToList();

            // 2. Inscripciones pendientes en workflow (T_INSTANCIA_WORKFLOW)
            var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var pendientes = instancias
                .Where(iw => inscripcionesDict.ContainsKey(iw.IdInstanciaWorkflow)
                          && inscripcionesDict[iw.IdInstanciaWorkflow].IdProducto.HasValue)
                .Select(iw =>
                {
                    var iwi        = inscripcionesDict[iw.IdInstanciaWorkflow];
                    var idProducto = (long)iwi.IdProducto!.Value;
                    var producto   = uow.Productos.GetByKey(idProducto);
                    var comienzo   = iwi.IdComienzo.HasValue
                        ? uow.Comienzos.GetByKey((long)iwi.IdComienzo.Value) : null;
                    var turno      = iwi.IdTurno.HasValue
                        ? uow.Turnos.GetByKey((long)iwi.IdTurno.Value) : null;

                    return new DTOProductoBeca
                    {
                        FechaInscripcion = iw.FechaInicialInstanciaWf ?? DateTime.MinValue,
                        IdProducto       = idProducto,
                        IdNivelProducto  = producto?.IdNivelProducto ?? 0,
                        NombreProducto   = producto?.NombreExtensoProducto,
                        NombreComienzo   = comienzo?.NombreComienzo,
                        NombreTurno      = turno?.NombreTurno,
                        IdProceso        = (long)iw.IdProceso,
                    };
                })
                .ToList();

            // 3. Productos con interés activo (sin inscripción pendiente en workflow)
            var intereses = uow.Productos.GetProductosConInteresActivo(codigoPersona)
                .Select(p => new DTOProductoBeca
                {
                    FechaInscripcion = DateTime.MinValue,
                    IdProducto       = p.IdProducto,
                    IdNivelProducto  = p.IdNivelProducto,
                    NombreProducto   = p.NombreExtensoProducto,
                    NombreComienzo   = p.ProcesoProductos?.FirstOrDefault()?.Proceso?.NombreProceso,
                    NombreTurno      = null,
                    IdProceso        = p.ProcesoProductos?.FirstOrDefault()?.IdProceso ?? 0,
                })
                .ToList();

            var todos = realizadas.Concat(pendientes).Concat(intereses)
                .GroupBy(b => b.IdProducto)
                .Select(g => g.OrderBy(b => b.FechaInscripcion).First())
                .ToList();

            return OperationResult<IEnumerable<DTOProductoBeca>>.Ok(todos, nameof(ObtenerProductosBeca));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
        }

        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        #region IMAGEN / DOCUMENTOS

        public OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo)
        {
            if (tipo != 1 && tipo != 2)
                return OperationResult<byte[]>.IsFailed("GEN_DA_01", nameof(ObtenerDocumentoAlumno), "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).", 400);

            using var uow = _uowFactory.Create();
            var imagenTemporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (imagenTemporal == null)
                return OperationResult<byte[]>.IsFailed("GEN_DA_02", nameof(ObtenerDocumentoAlumno), "Documento no encontrado.", 404);

            if (imagenTemporal.FechaVtoDocumentoPersona.HasValue && imagenTemporal.FechaVtoDocumentoPersona.Value < DateTime.Now)
                return OperationResult<byte[]>.IsFailed("GEN_DA_03", nameof(ObtenerDocumentoAlumno), "El documento se encuentra vencido.", 204);

            if (imagenTemporal.BlobImagen == null || imagenTemporal.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_DA_04", nameof(ObtenerDocumentoAlumno), "El documento no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagenTemporal.BlobImagen, nameof(ObtenerDocumentoAlumno));
        }

        public OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var imagen = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagen == null)
                return OperationResult<byte[]>.IsFailed("GEN_FA_01", nameof(ObtenerFotoAlumno), "Foto no encontrada.", 404);

            if (imagen.BlobImagen == null || imagen.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_FA_02", nameof(ObtenerFotoAlumno), "La foto no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagen.BlobImagen, nameof(ObtenerFotoAlumno));
        }

        public OperationResult<bool> SubirFotoAlumno(long codigoPersona, byte[] fileContent, string fileName)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SFA_01", nameof(SubirFotoAlumno), "Persona no encontrada.", 404);
            }

            var imagenExistente = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagenExistente is null)
            {
                var resultadoGuardado = GuardarFotoAlumno(
                    persona,
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirFotoAlumno),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.Imagens.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarFotoAlumno(imagenExistente, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirFotoAlumno),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.Imagens.Update(imagenExistente);
            }

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirFotoAlumno));
        }

        public OperationResult<bool> SubirDocumentoAlumno(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            if (tipo != 1 && tipo != 2)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SDA_01",
                    nameof(SubirDocumentoAlumno),
                    "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                    400);
            }

            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SDA_02", nameof(SubirDocumentoAlumno), "Persona no encontrada.", 404);
            }

            var documentoExistente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (documentoExistente is null)
            {
                var resultadoGuardado = GuardarDocumentoAlumno(
                    codigoPersona,
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                    tipo,
                    fecha,
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirDocumentoAlumno),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.ImagenTemporals.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarDocumentoAlumno(documentoExistente, tipo, fecha, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirDocumentoAlumno),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.ImagenTemporals.Update(documentoExistente);
            }

            persona.FechaVtoDocumentoPersona = fecha;
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoAlumno));
        }

        private static OperationResult<Imagen> GuardarFotoAlumno(Persona persona, int idImagen, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<Imagen>.IsFailed("GEN_SFA_03", nameof(GuardarFotoAlumno), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(GuardarFotoAlumno));

            if (!imageValidation.Success)
            {
                return OperationResult<Imagen>.IsFailed(
                    "GEN_SFA_02",
                    nameof(GuardarFotoAlumno),
                    $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                    400);
            }

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            return OperationResult<Imagen>.Ok(
                new Imagen
                {
                    IdImagen = idImagen,
                    CodigoPersona = persona.CodigoPersona,
                    NombreImagen = persona.CodigoPersona + "_3" + extension,
                    TipoImagen = "3",
                    BlobImagen = fileContent
                },
                nameof(GuardarFotoAlumno));
        }

        private static OperationResult<bool> ModificarFotoAlumno(Imagen existing, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<bool>.IsFailed("GEN_SFA_04", nameof(ModificarFotoAlumno), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(ModificarFotoAlumno));

            if (!imageValidation.Success)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SFA_05",
                    nameof(ModificarFotoAlumno),
                    $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                    400);
            }

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            existing.NombreImagen = (existing.CodigoPersona ?? 0) + "_3" + extension;
            existing.TipoImagen = "3";
            existing.BlobImagen = fileContent;
            return OperationResult<bool>.Ok(true, nameof(ModificarFotoAlumno));
        }

        private static OperationResult<ImagenTemporal> GuardarDocumentoAlumno(long codigoPersona, int idImagenTemporal, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = ValidarArchivoDocumentoAlumno(fileContent, fileName, nameof(GuardarDocumentoAlumno));
            if (!validacion.Success)
            {
                return OperationResult<ImagenTemporal>.IsFailed(validacion.ErrorCode, nameof(GuardarDocumentoAlumno), validacion.Message, validacion.HttpCode);
            }

            var extension = Path.GetExtension(validacion.Data) ?? ".jpg";
            var nombrePersistencia = $"{codigoPersona}_{tipo}{extension}";

            return OperationResult<ImagenTemporal>.Ok(
                new ImagenTemporal
                {
                    IdImagenTemporal = idImagenTemporal,
                    CodigoPersona = codigoPersona,
                    NombreImagen = nombrePersistencia,
                    TipoImagen = tipo.ToString(),
                    BlobImagen = fileContent,
                    FechaVtoDocumentoPersona = fecha
                },
                nameof(GuardarDocumentoAlumno));
        }

        private static OperationResult<bool> ModificarDocumentoAlumno(ImagenTemporal existing, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = ValidarArchivoDocumentoAlumno(fileContent, fileName, nameof(ModificarDocumentoAlumno));
            if (!validacion.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacion.ErrorCode,
                    nameof(ModificarDocumentoAlumno),
                    validacion.Message,
                    validacion.HttpCode);
            }

            var extension = Path.GetExtension(validacion.Data) ?? ".jpg";
            existing.NombreImagen = $"{existing.CodigoPersona}_{tipo}{extension}";
            existing.TipoImagen = tipo.ToString();
            existing.BlobImagen = fileContent;
            existing.FechaVtoDocumentoPersona = fecha;
            return OperationResult<bool>.Ok(true, nameof(ModificarDocumentoAlumno));
        }

        private static OperationResult<string> ValidarArchivoDocumentoAlumno(byte[] fileContent, string fileName, string originMethod)
        {
            var validacion = FileValidationHelper.ValidateFile(fileContent, fileName, AllowedDocumentExtensions, originMethod);
            if (!validacion.Success)
            {
                return OperationResult<string>.IsFailed(validacion.ErrorCode, originMethod, validacion.Message, validacion.HttpCode);
            }

            var nombreArchivo = FileValidationHelper.SanitizeFileName(fileName, AllowedDocumentExtensions, originMethod);
            if (!nombreArchivo.Success)
            {
                return OperationResult<string>.IsFailed(nombreArchivo.ErrorCode, originMethod, nombreArchivo.Message, nombreArchivo.HttpCode);
            }

            return OperationResult<string>.Ok(nombreArchivo.Data!, originMethod);
        }

        #endregion IMAGEN / DOCUMENTOS


        #region ADMISIONES

        public OperationResult<DateTime> ObtenerFechaVencimientoAdmisiones(long codigoPersona, long idProceso)
        {
            using var uow = _uowFactory.Create();

            var proceso = uow.Procesos.GetByKey(idProceso);
            if (proceso?.ComienzoSemestre1Proceso == null)
                return OperationResult<DateTime>.IsFailed("GEN_FVA_01", nameof(ObtenerFechaVencimientoAdmisiones),
                    "Problema con la carga de fecha del comienzo del proceso.", 400);

            var fechaComienzoSemestre = proceso.ComienzoSemestre1Proceso.Value;
            var fechaActual = DateTime.Now;
            const int cantDiasHabiles = 5;

            DateTime fechaVencimiento;
            if (fechaActual >= fechaComienzoSemestre)
            {
                fechaVencimiento = AddDiasHabilesaFecha(uow, fechaActual, 1, false);
            }
            else if (fechaActual > AddDiasHabilesaFecha(uow, fechaComienzoSemestre, cantDiasHabiles, true))
            {
                fechaVencimiento = fechaComienzoSemestre;
            }
            else
            {
                fechaVencimiento = AddDiasHabilesaFecha(uow, fechaActual, cantDiasHabiles, false);
            }

            var declaracion = uow.DeclaracionJuradaWebs.GetFechaEntregaDjAdmisiones(codigoPersona);
            if (declaracion.HasValue && declaracion.Value < fechaVencimiento)
                fechaVencimiento = declaracion.Value;

            return OperationResult<DateTime>.Ok(fechaVencimiento, nameof(ObtenerFechaVencimientoAdmisiones));
        }

        public OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(long idProducto, long idProceso, long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var producto = uow.Productos.GetByKey(idProducto);
            if (producto == null)
                return OperationResult<IEnumerable<DtoPruebaDevart>>.IsFailed("GEN_FBV_01", nameof(ObtenerFondosDeBecaVigentes),
                    "El producto indicado es inválido.", 400);

            long idNivelProducto = producto.IdNivelProducto;
            var pruebas = uow.Pruebas.GetFondosBecaVigentes(idNivelProducto, 0, codigoPersona, idProducto, idProceso);

            var fechaActual = DateTime.Now;
            var dtos = pruebas
                .Where(p =>
                {
                    if (p.FechaEntregaDjPrueba?.Date == fechaActual.Date && p.HoraEntregaDjPrueba != null)
                    {
                        var partes = p.HoraEntregaDjPrueba.Split(':');
                        if (partes.Length >= 2 && int.TryParse(partes[0], out int h) && int.TryParse(partes[1], out int m))
                        {
                            var limite = new DateTime(p.FechaEntregaDjPrueba.Value.Year,
                                p.FechaEntregaDjPrueba.Value.Month,
                                p.FechaEntregaDjPrueba.Value.Day, h, m, 0).AddHours(2);
                            return fechaActual <= limite;
                        }
                    }
                    return true;
                })
                .Select(p => p.ToDtoWithRelated(1));

            return OperationResult<IEnumerable<DtoPruebaDevart>>.Ok(dtos, nameof(ObtenerFondosDeBecaVigentes));
        }

        #endregion ADMISIONES

        #region HELPERS PRIVADOS

        private static DateTime AddDiasHabilesaFecha(
            IUnitOfWork uow, DateTime fecha, int cantDias, bool restar)
        {
            int signo = restar ? -1 : 1;
            int diasContados = 0;
            while (diasContados < cantDias)
            {
                fecha = fecha.AddDays(signo);
                if (fecha.DayOfWeek != DayOfWeek.Saturday
                    && fecha.DayOfWeek != DayOfWeek.Sunday
                    && !uow.Feriados.EsFeriado(fecha))
                    diasContados++;
            }
            return fecha;
        }

        #endregion HELPERS PRIVADOS
    }
}







