using AppLogic.Interfaces;
using AppLogic.DTOs;
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

