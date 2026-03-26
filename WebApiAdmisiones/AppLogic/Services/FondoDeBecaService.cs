using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.Interfaces;
using AppLogic.Utilities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using System.Text.Json;
using Utilities;

namespace AppLogic.Services
{
    public class FondoDeBecaService : IFondoDeBecaServices
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;

        public FondoDeBecaService(IUnitOfWorkFactory uowFactory, IDbConnectionContext dbConnectionContext)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
        }

        #region TIPOS DECLARACIÓN JURADA

        public OperationResult<IEnumerable<DtoTipoParentescoDevart>> ObtenerTiposParentesco()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoParentescos.GetAll().ToList();
            return OperationResult<IEnumerable<DtoTipoParentescoDevart>>.Ok(
                entidades.ToDtos(),
                nameof(ObtenerTiposParentesco));
        }

        public OperationResult<IEnumerable<DtoTipoEgresoDjDevart>> ObtenerTiposEgreso()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoEgresoDjs.GetAll()
                .Where(e => e.Activo == CommonConstants.Booleanos.Si)
                .OrderBy(e => e.Orden)
                .ToList();
            return OperationResult<IEnumerable<DtoTipoEgresoDjDevart>>.Ok(
                entidades.ToDtos(),
                nameof(ObtenerTiposEgreso));
        }

        public OperationResult<IEnumerable<DtoTipoViviendaDevart>> ObtenerTiposVivienda()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoViviendas.GetAll().ToList();
            return OperationResult<IEnumerable<DtoTipoViviendaDevart>>.Ok(
                entidades.ToDtos(),
                nameof(ObtenerTiposVivienda));
        }

        #endregion

        #region UNIVERSIDADES

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades(long codigoPais)
        {
            using var uow = _uowFactory.Create();

            var pais = uow.Paises.GetPaisConEstadosYCiudades(codigoPais);
            if (pais == null)
            {
                return OperationResult<IEnumerable<DtoEmpresaDevart>>.IsFailed(
                    "FDB_UV_01",
                    nameof(ObtenerUniversidades),
                    "El país indicado es inválido.",
                    400);
            }

            var entidades = uow.Empresas.GetUniversidades(codigoPais).ToList();
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(
                entidades.ToDtos(),
                nameof(ObtenerUniversidades));
        }

        #endregion

        #region DECLARACIÓN JURADA

        public OperationResult<IEnumerable<DtoDeclaracionJuradaAdmisiones>> ObtenerFormulariosDeclaracionJuradaWeb(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var declaraciones = uow.DeclaracionJuradaWebs
                .GetFormulariosAdmisionesVigentes(codigoPersona)
                .Select(MapDeclaracionBase)
                .Select(declaracion => declaracion.ToAdmisionesDto(ObtenerPrueba(uow, (long)declaracion.IdInscriptoPrueba)))
                .ToList();

            if (declaraciones.Count == 0)
            {
                return OperationResult<IEnumerable<DtoDeclaracionJuradaAdmisiones>>.IsFailed(
                    "FDB_FDJ_01",
                    nameof(ObtenerFormulariosDeclaracionJuradaWeb),
                    "No se encontraron formularios de declaración jurada web vigentes para la persona.",
                    404);
            }

            return OperationResult<IEnumerable<DtoDeclaracionJuradaAdmisiones>>.Ok(
                declaraciones,
                nameof(ObtenerFormulariosDeclaracionJuradaWeb));
        }

        public OperationResult<DtoDeclaracionJuradaWebDevart> ObtenerFormularioDeclaracionJuradaWebDetalle(long codigoPersona, long idInscriptoPrueba)
        {
            using var uow = _uowFactory.Create();

            var entidad = uow.DeclaracionJuradaWebs.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba);
            if (entidad is null)
            {
                return OperationResult<DtoDeclaracionJuradaWebDevart>.IsFailed(
                    "FDB_FDJ_02",
                    nameof(ObtenerFormularioDeclaracionJuradaWebDetalle),
                    "No se encontró el formulario de declaración jurada web para la inscripción indicada.",
                    404);
            }

            var detalle = MapDeclaracionDetalle(uow, entidad);

            return OperationResult<DtoDeclaracionJuradaWebDevart>.Ok(
                detalle,
                nameof(ObtenerFormularioDeclaracionJuradaWebDetalle));
        }

        public OperationResult<bool> GuardarFormularioDeclaracionJuradaWeb(
            long codigoPersona,
            DtoDeclaracionJuradaWebDevart declaracionModificada,
            bool confirmar)
        {
            var validacionEntrada = FondoDeBecaValidation.ValidarSolicitudGuardadoDeclaracion(
                declaracionModificada,
                nameof(GuardarFormularioDeclaracionJuradaWeb));
            if (!validacionEntrada.Success)
            {
                return validacionEntrada;
            }

            using var uow = _uowFactory.Create();

            var entidad = uow.DeclaracionJuradaWebs.GetFormularioAdmisionesCompleto(
                codigoPersona,
                (long)declaracionModificada.IdInscriptoPrueba);
            return entidad is null
                ? CrearFormularioDeclaracionJuradaWeb(uow, codigoPersona, declaracionModificada, confirmar)
                : ModificarFormularioDeclaracionJuradaWeb(uow, entidad, declaracionModificada, confirmar);
        }

        private OperationResult<bool> CrearFormularioDeclaracionJuradaWeb(
            IUnitOfWork uow,
            long codigoPersona,
            DtoDeclaracionJuradaWebDevart declaracionModificada,
            bool confirmar)
        {
            var inscriptoPrueba = uow.InscriptoPruebas.GetByKey((long)declaracionModificada.IdInscriptoPrueba);
            if (inscriptoPrueba is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_16",
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    "No se encontró la inscripción a prueba indicada.",
                    404);
            }

            if (inscriptoPrueba.CodigoPersona != codigoPersona)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_17",
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    "La inscripción a prueba indicada no pertenece al usuario autenticado.",
                    403);
            }

            if (!inscriptoPrueba.IdProducto.HasValue)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_18",
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    "No fue posible determinar el producto asociado a la inscripción a prueba.",
                    404);
            }

            var prueba = uow.Pruebas.GetByKey(inscriptoPrueba.IdPrueba);
            if (prueba is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_19",
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    "No se encontró la prueba asociada a la inscripción indicada.",
                    404);
            }

            var fechaActual = DateTime.Now;
            var entidad = new BusinessLogic.Entities.DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_DECLARACION_JURADA_WEB),
                CodigoPersona = codigoPersona,
                IdProducto = inscriptoPrueba.IdProducto.Value,
                IdTipoDescuento = prueba.IdTipoBeca,
                IdInscriptoPrueba = inscriptoPrueba.IdInscriptoPrueba,
                FechaIngreso = fechaActual,
                HoraIngreso = fechaActual.ToString("HH:mm:ss"),
                UsuarioIngreso = FondoDeBecaConstants.Declaracion.UsuarioIngresoWebAdmisiones,
                TienevehiculoNfDj = declaracionModificada.TienevehiculoNfDj ?? CommonConstants.Booleanos.No,
                TienecasaveraneoNfDj = declaracionModificada.TienecasaveraneoNfDj ?? CommonConstants.Booleanos.No
            };

            var validacionPrueba = ValidarPlazoDeclaracion(uow, entidad, fechaActual, nameof(GuardarFormularioDeclaracionJuradaWeb));
            if (!validacionPrueba.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacionPrueba.ErrorCode,
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    validacionPrueba.Message,
                    validacionPrueba.HttpCode);
            }

            uow.DeclaracionJuradaWebs.Add(entidad);
            AplicarCambiosDeclaracion(entidad, declaracionModificada, fechaActual, confirmar);
            SincronizarEgresos(uow, entidad, declaracionModificada.EgresoMensualNfDjs);
            SincronizarIntegrantes(uow, entidad, declaracionModificada.IntegranteNfDjs);

            if (confirmar)
            {
                var validacionConfirmacion = FondoDeBecaValidation.ValidarConfirmacionDeclaracion(entidad, nameof(GuardarFormularioDeclaracionJuradaWeb));
                if (!validacionConfirmacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        validacionConfirmacion.ErrorCode,
                        nameof(GuardarFormularioDeclaracionJuradaWeb),
                        validacionConfirmacion.Message,
                        validacionConfirmacion.HttpCode);
                }

                ActualizarInscriptoPrueba(uow, entidad, prueba, fechaActual);
            }

            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(GuardarFormularioDeclaracionJuradaWeb));
        }

        private OperationResult<bool> ModificarFormularioDeclaracionJuradaWeb(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb entidad,
            DtoDeclaracionJuradaWebDevart declaracionModificada,
            bool confirmar)
        {

            if (entidad.Subestado is FondoDeBecaConstants.Declaracion.ConfirmadoWeb or FondoDeBecaConstants.Declaracion.ConfirmadoWebSinInscripcion)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_02",
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    "La declaración jurada ya fue completada y confirmada.",
                    409);
            }

            var snapshotBaseActual = CrearSnapshot(MapDeclaracionDetalle(uow, entidad));
            var snapshotModificado = CrearSnapshot(declaracionModificada);
            if (!confirmar && string.Equals(snapshotBaseActual, snapshotModificado, StringComparison.Ordinal))
            {
                return OperationResult<bool>.Ok(true, nameof(GuardarFormularioDeclaracionJuradaWeb));
            }

            var fechaActual = DateTime.Now;
            var validacionPrueba = ValidarPlazoDeclaracion(uow, entidad, fechaActual, nameof(GuardarFormularioDeclaracionJuradaWeb));
            if (!validacionPrueba.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacionPrueba.ErrorCode,
                    nameof(GuardarFormularioDeclaracionJuradaWeb),
                    validacionPrueba.Message,
                    validacionPrueba.HttpCode);
            }

            AplicarCambiosDeclaracion(entidad, declaracionModificada, fechaActual, confirmar);
            SincronizarEgresos(uow, entidad, declaracionModificada.EgresoMensualNfDjs);
            SincronizarIntegrantes(uow, entidad, declaracionModificada.IntegranteNfDjs);

            if (confirmar)
            {
                var validacionConfirmacion = FondoDeBecaValidation.ValidarConfirmacionDeclaracion(entidad, nameof(GuardarFormularioDeclaracionJuradaWeb));
                if (!validacionConfirmacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        validacionConfirmacion.ErrorCode,
                        nameof(GuardarFormularioDeclaracionJuradaWeb),
                        validacionConfirmacion.Message,
                        validacionConfirmacion.HttpCode);
                }

                if (validacionPrueba.Data is null)
                {
                    return OperationResult<bool>.IsFailed(
                        "FDB_GDJ_20",
                        nameof(GuardarFormularioDeclaracionJuradaWeb),
                        "No fue posible cargar la prueba asociada a la declaración jurada.",
                        404);
                }

                ActualizarInscriptoPrueba(uow, entidad, validacionPrueba.Data, fechaActual);
            }

            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(GuardarFormularioDeclaracionJuradaWeb));
        }

        public OperationResult<bool> SubirArchivoIngreso(long codigoPersona, long idIngresoMensualNF, byte[] fileContent, string fileName)
        {
            var archivoValidado = FondoDeBecaValidation.ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoIngreso));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoIngreso),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, idIngresoMensualNF, nameof(SubirArchivoIngreso), "FDB_SAI");
            if (!ingresoResult.Success || ingresoResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    ingresoResult.ErrorCode,
                    nameof(SubirArchivoIngreso),
                    ingresoResult.Message,
                    ingresoResult.HttpCode);
            }

            var ingreso = ingresoResult.Data;

            ingreso.NombreArchivoIngreso = Path.GetFileNameWithoutExtension(archivoValidado.Data) ?? string.Empty;
            ingreso.ExtensionArchivoIngreso = Path.GetExtension(archivoValidado.Data) ?? string.Empty;
            ingreso.ArchivoIngresoNfDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoIngreso));
        }

        public OperationResult<DtoArchivoDescarga> DescargarArchivoIngreso(long codigoPersona, long idIngresoMensualNF)
        {
            using var uow = _uowFactory.Create();
            var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, idIngresoMensualNF, nameof(DescargarArchivoIngreso), "FDB_DAI");
            if (!ingresoResult.Success || ingresoResult.Data is null)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    ingresoResult.ErrorCode,
                    nameof(DescargarArchivoIngreso),
                    ingresoResult.Message,
                    ingresoResult.HttpCode);
            }

            var ingreso = ingresoResult.Data;
            var archivo = ingreso.ArchivoIngresoNfDj;
            if (archivo is null || archivo.Length == 0)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    "FDB_DAI_05",
                    nameof(DescargarArchivoIngreso),
                    "El ingreso mensual indicado no tiene archivo adjunto.",
                    404);
            }

            var extension = NormalizarExtension(ingreso.ExtensionArchivoIngreso);
            var nombreArchivo = ConstruirNombreArchivo(ingreso.NombreArchivoIngreso, extension, $"ingreso_{idIngresoMensualNF}");

            return OperationResult<DtoArchivoDescarga>.Ok(
                new DtoArchivoDescarga
                {
                    Archivo = archivo,
                    NombreArchivo = nombreArchivo,
                    ContentType = ObtenerContentType(extension)
                },
                nameof(DescargarArchivoIngreso));
        }

        public OperationResult<bool> EliminarArchivoIngreso(long codigoPersona, long idIngresoMensualNF)
        {
            using var uow = _uowFactory.Create();
            var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, idIngresoMensualNF, nameof(EliminarArchivoIngreso), "FDB_EAI");
            if (!ingresoResult.Success || ingresoResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    ingresoResult.ErrorCode,
                    nameof(EliminarArchivoIngreso),
                    ingresoResult.Message,
                    ingresoResult.HttpCode);
            }

            var ingreso = ingresoResult.Data;
            ingreso.NombreArchivoIngreso = string.Empty;
            ingreso.ExtensionArchivoIngreso = string.Empty;
            ingreso.ArchivoIngresoNfDj = null;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(EliminarArchivoIngreso));
        }

        public OperationResult<bool> SubirArchivoEgreso(long codigoPersona, long idEgresoMensualNF, byte[] fileContent, string fileName)
        {
            var archivoValidado = FondoDeBecaValidation.ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoEgreso));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoEgreso),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var egresoResult = ObtenerEgresoAutorizado(uow, codigoPersona, idEgresoMensualNF, nameof(SubirArchivoEgreso), "FDB_SAE");
            if (!egresoResult.Success || egresoResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    egresoResult.ErrorCode,
                    nameof(SubirArchivoEgreso),
                    egresoResult.Message,
                    egresoResult.HttpCode);
            }

            var egreso = egresoResult.Data;

            egreso.NombreArchivoEgreso = Path.GetFileNameWithoutExtension(archivoValidado.Data) ?? string.Empty;
            egreso.ExtensionArchivoEgreso = Path.GetExtension(archivoValidado.Data) ?? string.Empty;
            egreso.ArchivoEgresoMensualNfDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoEgreso));
        }

        public OperationResult<DtoArchivoDescarga> DescargarArchivoEgreso(long codigoPersona, long idEgresoMensualNF)
        {
            using var uow = _uowFactory.Create();
            var egresoResult = ObtenerEgresoAutorizado(uow, codigoPersona, idEgresoMensualNF, nameof(DescargarArchivoEgreso), "FDB_DAE");
            if (!egresoResult.Success || egresoResult.Data is null)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    egresoResult.ErrorCode,
                    nameof(DescargarArchivoEgreso),
                    egresoResult.Message,
                    egresoResult.HttpCode);
            }

            var egreso = egresoResult.Data;
            var archivo = egreso.ArchivoEgresoMensualNfDj;
            if (archivo is null || archivo.Length == 0)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    "FDB_DAE_03",
                    nameof(DescargarArchivoEgreso),
                    "El egreso mensual indicado no tiene archivo adjunto.",
                    404);
            }

            var extension = NormalizarExtension(egreso.ExtensionArchivoEgreso);
            var nombreArchivo = ConstruirNombreArchivo(egreso.NombreArchivoEgreso, extension, $"egreso_{idEgresoMensualNF}");

            return OperationResult<DtoArchivoDescarga>.Ok(
                new DtoArchivoDescarga
                {
                    Archivo = archivo,
                    NombreArchivo = nombreArchivo,
                    ContentType = ObtenerContentType(extension)
                },
                nameof(DescargarArchivoEgreso));
        }

        public OperationResult<bool> EliminarArchivoEgreso(long codigoPersona, long idEgresoMensualNF)
        {
            using var uow = _uowFactory.Create();
            var egresoResult = ObtenerEgresoAutorizado(uow, codigoPersona, idEgresoMensualNF, nameof(EliminarArchivoEgreso), "FDB_EAE");
            if (!egresoResult.Success || egresoResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    egresoResult.ErrorCode,
                    nameof(EliminarArchivoEgreso),
                    egresoResult.Message,
                    egresoResult.HttpCode);
            }

            var egreso = egresoResult.Data;
            egreso.NombreArchivoEgreso = string.Empty;
            egreso.ExtensionArchivoEgreso = string.Empty;
            egreso.ArchivoEgresoMensualNfDj = null;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(EliminarArchivoEgreso));
        }

        public OperationResult<bool> SubirArchivoRevalidaDJ(long codigoPersona, long idDeclaracionJuradaWeb, byte[] fileContent, string fileName)
        {
            var archivoValidado = FondoDeBecaValidation.ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoRevalidaDJ));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoRevalidaDJ),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var declaracionResult = ObtenerDeclaracionAutorizada(
                uow,
                codigoPersona,
                idDeclaracionJuradaWeb,
                nameof(SubirArchivoRevalidaDJ),
                "FDB_SAR");
            if (!declaracionResult.Success || declaracionResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    declaracionResult.ErrorCode,
                    nameof(SubirArchivoRevalidaDJ),
                    declaracionResult.Message,
                    declaracionResult.HttpCode);
            }

            var declaracion = declaracionResult.Data;
            declaracion.NombrePdfRevalidasDj = Path.GetFileNameWithoutExtension(archivoValidado.Data);
            declaracion.ExtensionPdfRevalidasDj = Path.GetExtension(archivoValidado.Data);
            declaracion.PdfFormRevalidasDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoRevalidaDJ));
        }

        public OperationResult<DtoArchivoDescarga> DescargarArchivoRevalidaDJ(long codigoPersona, long idDeclaracionJuradaWeb)
        {
            using var uow = _uowFactory.Create();
            var declaracionResult = ObtenerDeclaracionAutorizada(
                uow,
                codigoPersona,
                idDeclaracionJuradaWeb,
                nameof(DescargarArchivoRevalidaDJ),
                "FDB_DAR");
            if (!declaracionResult.Success || declaracionResult.Data is null)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    declaracionResult.ErrorCode,
                    nameof(DescargarArchivoRevalidaDJ),
                    declaracionResult.Message,
                    declaracionResult.HttpCode);
            }

            var declaracion = declaracionResult.Data;
            if (declaracion.PdfFormRevalidasDj is null || declaracion.PdfFormRevalidasDj.Length == 0)
            {
                return OperationResult<DtoArchivoDescarga>.IsFailed(
                    "FDB_DAR_03",
                    nameof(DescargarArchivoRevalidaDJ),
                    "La declaración jurada indicada no tiene archivo de reválida adjunto.",
                    404);
            }

            var extension = NormalizarExtension(declaracion.ExtensionPdfRevalidasDj);
            var nombreArchivo = ConstruirNombreArchivo(
                declaracion.NombrePdfRevalidasDj,
                extension,
                $"revalida_{idDeclaracionJuradaWeb}");

            return OperationResult<DtoArchivoDescarga>.Ok(
                new DtoArchivoDescarga
                {
                    Archivo = declaracion.PdfFormRevalidasDj,
                    NombreArchivo = nombreArchivo,
                    ContentType = ObtenerContentType(extension)
                },
                nameof(DescargarArchivoRevalidaDJ));
        }

        public OperationResult<bool> EliminarArchivoRevalidaDJ(long codigoPersona, long idDeclaracionJuradaWeb)
        {
            using var uow = _uowFactory.Create();
            var declaracionResult = ObtenerDeclaracionAutorizada(
                uow,
                codigoPersona,
                idDeclaracionJuradaWeb,
                nameof(EliminarArchivoRevalidaDJ),
                "FDB_EAR");
            if (!declaracionResult.Success || declaracionResult.Data is null)
            {
                return OperationResult<bool>.IsFailed(
                    declaracionResult.ErrorCode,
                    nameof(EliminarArchivoRevalidaDJ),
                    declaracionResult.Message,
                    declaracionResult.HttpCode);
            }

            var declaracion = declaracionResult.Data;
            declaracion.NombrePdfRevalidasDj = string.Empty;
            declaracion.ExtensionPdfRevalidasDj = string.Empty;
            declaracion.PdfFormRevalidasDj = null;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(EliminarArchivoRevalidaDJ));
        }

        #endregion DECLARACIÓN JURADA

        #region METODOS PRIVADOS

        private static DtoDeclaracionJuradaWebDevart MapDeclaracionBase(BusinessLogic.Entities.DeclaracionJuradaWeb entity)
        {
            ArgumentNullException.ThrowIfNull(entity.Persona);
            ArgumentNullException.ThrowIfNull(entity.Producto);
            ArgumentNullException.ThrowIfNull(entity.TipoDescuento);

            var dto = entity.ToDto();
            dto.Persona = entity.Persona.ToDto();
            dto.Producto = entity.Producto.ToDto();
            dto.TipoDescuento = entity.TipoDescuento.ToDto();
            return dto;
        }
        private static DtoDeclaracionJuradaWebDevart MapDeclaracionDetalle(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb entity)
        {
            var detalle = MapDeclaracionBase(entity);
            detalle.ObjPrueba = ObtenerPrueba(uow, (long)entity.IdInscriptoPrueba);

            if (entity.CodigoInstitucionBac is decimal codigoInstitucionBac)
            {
                detalle.NombreBachillerato = uow.Empresas.GetByKey((long)codigoInstitucionBac)?.Nombre;
            }

            if (entity.IdTipoVivienda is decimal idTipoVivienda)
            {
                var tipoVivienda = uow.TipoViviendas.GetByKey(idTipoVivienda);
                if (tipoVivienda is not null)
                {
                    detalle.ObjTipoVivienda = tipoVivienda.ToDto();
                }
            }

            detalle.EgresoMensualNfDjs = entity.EgresoMensualNfDjs?
                .OrderBy(e => e.IdEgresoMensualNfDj)
                .Select(egreso =>
                {
                    var dto = egreso.ToDto();
                    if (egreso.TipoEgresoDj is not null)
                    {
                        dto.TipoEgresoDj = egreso.TipoEgresoDj.ToDto();
                    }
                    return dto;
                })
                .ToList() ?? [];

            detalle.IntegranteNfDjs = entity.IntegranteNfDjs?
                .OrderBy(i => i.IdIntegranteNfDj)
                .Select(integrante =>
                {
                    var dto = integrante.ToDto();
                    if (integrante.TipoParentesco is not null)
                    {
                        dto.TipoParentesco = integrante.TipoParentesco.ToDto();
                    }
                    dto.IngresoMensualNfDjs = integrante.IngresoMensualNfDjs?
                        .OrderBy(i => i.IdIngresoMensualNfDj)
                        .Select(ingreso => ingreso.ToDto())
                        .ToList() ?? [];
                    return dto;
                })
                .ToList() ?? [];

            return detalle;
        }

        private static DtoPruebaDevart? ObtenerPrueba(IUnitOfWork uow, long idInscriptoPrueba)
        {
            var inscriptoPrueba = uow.InscriptoPruebas.GetByKey(idInscriptoPrueba);
            if (inscriptoPrueba is null)
            {
                return null;
            }

            var prueba = uow.Pruebas.GetByKey(inscriptoPrueba.IdPrueba);
            if (prueba is null)
            {
                return null;
            }

            var dto = prueba.ToDto();
            if( prueba.TipoDescuento is not null){
                dto.TipoDescuento = prueba.TipoDescuento.ToDto();
            }
            if( prueba.Comienzo is not null){
                dto.Comienzo = prueba.Comienzo.ToDto();
            }
            return dto;
        }

        private static OperationResult<BusinessLogic.Entities.Prueba> ValidarPlazoDeclaracion(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb declaracion,
            DateTime fechaActual,
            string methodName)
        {
            var inscriptoPrueba = uow.InscriptoPruebas.GetByKey((long)declaracion.IdInscriptoPrueba);
            if (inscriptoPrueba is null)
            {
                return OperationResult<BusinessLogic.Entities.Prueba>.IsFailed(
                    "FDB_GDJ_04",
                    methodName,
                    "No fue posible cargar la inscripción a prueba de la declaración jurada.",
                    404);
            }

            var prueba = uow.Pruebas.GetByKey(inscriptoPrueba.IdPrueba);
            if (prueba?.FechaEntregaDjPrueba is null)
            {
                return OperationResult<BusinessLogic.Entities.Prueba>.IsFailed(
                    "FDB_GDJ_05",
                    methodName,
                    "No fue posible determinar la fecha límite de la declaración jurada.",
                    404);
            }

            if (prueba.FechaEntregaDjPrueba.Value.Date < fechaActual.Date)
            {
                return OperationResult<BusinessLogic.Entities.Prueba>.IsFailed(
                    "FDB_GDJ_06",
                    methodName,
                    "Ya pasó la fecha límite de entrega de la declaración jurada.",
                    400);
            }

            if (prueba.FechaEntregaDjPrueba.Value.Date == fechaActual.Date
                && !string.IsNullOrWhiteSpace(prueba.HoraEntregaDjPrueba))
            {
                var partes = prueba.HoraEntregaDjPrueba.Split(':');
                if (partes.Length >= 2
                    && int.TryParse(partes[0], out var hora)
                    && int.TryParse(partes[1], out var minutos))
                {
                    var limite = new DateTime(
                        prueba.FechaEntregaDjPrueba.Value.Year,
                        prueba.FechaEntregaDjPrueba.Value.Month,
                        prueba.FechaEntregaDjPrueba.Value.Day,
                        hora,
                        minutos,
                        0,
                        DateTimeKind.Local).AddHours(2);

                    if (fechaActual > limite)
                    {
                        return OperationResult<BusinessLogic.Entities.Prueba>.IsFailed(
                            "FDB_GDJ_06",
                            methodName,
                            "Ya pasó la fecha límite de entrega de la declaración jurada.",
                            400);
                    }
                }
            }

            return OperationResult<BusinessLogic.Entities.Prueba>.Ok(prueba, methodName);
        }

        private static void AplicarCambiosDeclaracion(
            BusinessLogic.Entities.DeclaracionJuradaWeb target,
            DtoDeclaracionJuradaWebDevart source,
            DateTime fechaActual,
            bool confirmar)
        {
            target.IdTipoVivienda = source.IdTipoVivienda;
            target.MontoViviendaDj = source.MontoViviendaDj;
            target.ObsOtroViviendaDj = source.ObsOtroViviendaDj;
            target.ObservacionesNfDj = source.ObservacionesNfDj;
            target.OtroActivosNfDj = source.OtroActivosNfDj;
            target.SolventoEstudiosNfDj = source.SolventoEstudiosNfDj;
            target.TienevehiculoNfDj = source.TienevehiculoNfDj;
            target.MarcavehiculoNfDj = source.MarcavehiculoNfDj;
            target.AnioVehiculoNfDj = source.AnioVehiculoNfDj;
            target.TienecasaveraneoNfDj = source.TienecasaveraneoNfDj;
            target.BalnearioNfDj = source.BalnearioNfDj;
            target.ObservacionesIngresosNfDj = source.ObservacionesIngresosNfDj;
            target.ResidenciaDj = source.ResidenciaDj;
            target.ResidiraEnmontevideoDj = source.ResidiraEnmontevideoDj;
            target.IdTipoResidencia = source.IdTipoResidencia;
            target.DetalleResideEnmontevideoDj = source.DetalleResideEnmontevideoDj;
            target.CoberturasaludResideMontDj = source.CoberturasaludResideMontDj;
            target.GastosTrasladoResideInteDj = source.GastosTrasladoResideInteDj;
            target.NominalOtrosingresosNfDj = source.NominalOtrosingresosNfDj;
            target.LiquidoOtrosingresosNfDj = source.LiquidoOtrosingresosNfDj;
            target.ObservacionesEgresosNfDj = source.ObservacionesEgresosNfDj;
            target.CodigoInstitucionBac = source.CodigoInstitucionBac;
            target.FacultadUniversidadDj = source.FacultadUniversidadDj;
            target.CarreraUniversitariaDj = source.CarreraUniversitariaDj;
            target.CantMateriasAprobadasDj = source.CantMateriasAprobadasDj;
            target.CantMatExaReprobadosDj = source.CantMatExaReprobadosDj;
            target.PromTotalCalificacionesDj = source.PromTotalCalificacionesDj;
            target.PromAprlCalificacionesDj = source.PromAprlCalificacionesDj;
            target.UltimoanioSecundariaAprDj = source.UltimoanioSecundariaAprDj;
            target.OtrosEstudiosDj = source.OtrosEstudiosDj;
            target.DetalleOtrosEstudiosDj = source.DetalleOtrosEstudiosDj;
            target.ExperienciaLaboralDj = source.ExperienciaLaboralDj;
            target.FechaComienzoTrabajo = source.FechaComienzoTrabajo;
            target.LugarTrabajoActualDj = source.LugarTrabajoActualDj;
            target.CargoDj = source.CargoDj;
            target.FamiliaresCursandoEnort = source.FamiliaresCursandoEnort;
            target.CodigoInstitucionUniv = source.CodigoInstitucionUniv;
            target.ModalidadPostulacionDj = source.ModalidadPostulacionDj;
            target.TipoBachillerato = source.TipoBachillerato;
            target.PromedioCalificaiones5to = source.PromedioCalificaiones5to;
            target.TrabajaActualmente = source.TrabajaActualmente;
            target.PromedioCalificaiones6to = source.PromedioCalificaiones6to;
            target.CodigoCiudadDj = source.CodigoCiudadDj;
            target.CodigoEstadoDj = source.CodigoEstadoDj;
            target.UltactualizacionDecjurada = fechaActual;

            if (target.ResidenciaDj != 2)
            {
                target.CodigoCiudadDj = null;
                target.CodigoEstadoDj = null;
            }

            if (confirmar)
            {
                target.ConfirmacionDecjurada = fechaActual;
                target.Subestado = FondoDeBecaConstants.Declaracion.ConfirmadoWeb;
            }
            else
            {
                target.Subestado = FondoDeBecaConstants.Declaracion.GuardadoWebSinConfirmar;
            }
        }

        private void SincronizarEgresos(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb declaracion,
            IEnumerable<DtoEgresoMensualNfDjDevart>? egresosDto)
        {
            if (egresosDto is null)
            {
                return;
            }

            var incoming = (egresosDto).ToList();
            var existentes = declaracion.EgresoMensualNfDjs.ToList();

            foreach (var egresoExistente in existentes)
            {
                var match = incoming.FirstOrDefault(e =>
                    e.IdEgresoMensualNfDj == egresoExistente.IdEgresoMensualNfDj
                    || (!string.IsNullOrWhiteSpace(e.IdEgresoFront)
                        && string.Equals(e.IdEgresoFront, egresoExistente.IdEgresoFront, StringComparison.Ordinal)));

                if (match is null)
                {
                    declaracion.EgresoMensualNfDjs.Remove(egresoExistente);
                    uow.EgresoMensualNfDjs.Remove(egresoExistente);
                    continue;
                }

                egresoExistente.IdTipoEgresoDj = match.IdTipoEgresoDj;
                egresoExistente.MontoEgresoMensualNfDj = match.MontoEgresoMensualNfDj;
                egresoExistente.DetalleOtroEgreso = match.DetalleOtroEgreso;
                egresoExistente.IdEgresoFront = match.IdEgresoFront;
            }

            foreach (var egresoNuevo in incoming.Where(e =>
                         !existentes.Any(existing =>
                             existing.IdEgresoMensualNfDj == e.IdEgresoMensualNfDj
                             || (!string.IsNullOrWhiteSpace(e.IdEgresoFront)
                                 && string.Equals(existing.IdEgresoFront, e.IdEgresoFront, StringComparison.Ordinal)))))
            {
                var nuevo = new BusinessLogic.Entities.EgresoMensualNfDj
                {
                    IdEgresoMensualNfDj = egresoNuevo.IdEgresoMensualNfDj > 0
                        ? egresoNuevo.IdEgresoMensualNfDj
                        : _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EGRESO_MENSUAL_NF),
                    IdDeclaracionjuradaWeb = declaracion.IdDeclaracionjuradaWeb,
                    IdTipoEgresoDj = egresoNuevo.IdTipoEgresoDj,
                    MontoEgresoMensualNfDj = egresoNuevo.MontoEgresoMensualNfDj,
                    DetalleOtroEgreso = egresoNuevo.DetalleOtroEgreso,
                    IdEgresoFront = egresoNuevo.IdEgresoFront,
                    ArchivoEgresoMensualNfDj = egresoNuevo.ArchivoEgresoMensualNfDj,
                    NombreArchivoEgreso = egresoNuevo.NombreArchivoEgreso,
                    ExtensionArchivoEgreso = egresoNuevo.ExtensionArchivoEgreso
                };

                declaracion.EgresoMensualNfDjs.Add(nuevo);
                uow.EgresoMensualNfDjs.Add(nuevo);
            }
        }

        private void SincronizarIntegrantes(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb declaracion,
            IEnumerable<DtoIntegranteNfDjDevart>? integrantesDto)
        {
            if (integrantesDto is null)
            {
                return;
            }

            var incoming = (integrantesDto).ToList();
            var existentes = declaracion.IntegranteNfDjs.ToList();

            foreach (var integranteExistente in existentes)
            {
                var match = incoming.FirstOrDefault(i => i.IdIntegranteNfDj == integranteExistente.IdIntegranteNfDj);
                if (match is null)
                {
                    foreach (var ingreso in integranteExistente.IngresoMensualNfDjs.ToList())
                    {
                        integranteExistente.IngresoMensualNfDjs.Remove(ingreso);
                        uow.IngresoMensualNfDjs.Remove(ingreso);
                    }

                    declaracion.IntegranteNfDjs.Remove(integranteExistente);
                    uow.IntegranteNfDjs.Remove(integranteExistente);
                    continue;
                }

                integranteExistente.IdTipoParentesco = match.IdTipoParentesco;
                integranteExistente.NombreIntegranteNfDj = match.NombreIntegranteNfDj;
                integranteExistente.EdadIntegranteNfDj = match.EdadIntegranteNfDj;
                integranteExistente.ActividadIntegranteNfDj = match.ActividadIntegranteNfDj;
                integranteExistente.DetalleOtroParentesco = match.DetalleOtroParentesco;

                SincronizarIngresos(uow, integranteExistente, match.IngresoMensualNfDjs);
            }

            foreach (var integranteNuevo in incoming.Where(i =>
                         !existentes.Any(existing => existing.IdIntegranteNfDj == i.IdIntegranteNfDj)))
            {
                var nuevo = new BusinessLogic.Entities.IntegranteNfDj
                {
                    IdIntegranteNfDj = integranteNuevo.IdIntegranteNfDj > 0
                        ? integranteNuevo.IdIntegranteNfDj
                        : _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTEGRANTE_NF),
                    IdDeclaracionjuradaWeb = declaracion.IdDeclaracionjuradaWeb,
                    IdTipoParentesco = integranteNuevo.IdTipoParentesco,
                    NombreIntegranteNfDj = integranteNuevo.NombreIntegranteNfDj,
                    EdadIntegranteNfDj = integranteNuevo.EdadIntegranteNfDj,
                    ActividadIntegranteNfDj = integranteNuevo.ActividadIntegranteNfDj,
                    DetalleOtroParentesco = integranteNuevo.DetalleOtroParentesco,
                    IngresoMensualNfDjs = []
                };

                declaracion.IntegranteNfDjs.Add(nuevo);
                uow.IntegranteNfDjs.Add(nuevo);
                SincronizarIngresos(uow, nuevo, integranteNuevo.IngresoMensualNfDjs);
            }
        }

        private void SincronizarIngresos(
            IUnitOfWork uow,
            BusinessLogic.Entities.IntegranteNfDj integrante,
            IEnumerable<DtoIngresoMensualNfDjDevart>? ingresosDto)
        {
            if (ingresosDto is null)
            {
                return;
            }

            var incoming = (ingresosDto).ToList();
            var existentes = integrante.IngresoMensualNfDjs.ToList();

            foreach (var ingresoExistente in existentes)
            {
                var match = incoming.FirstOrDefault(i =>
                    i.IdIngresoMensualNfDj == ingresoExistente.IdIngresoMensualNfDj
                    || (!string.IsNullOrWhiteSpace(i.IdIngresoFront)
                        && string.Equals(i.IdIngresoFront, ingresoExistente.IdIngresoFront, StringComparison.Ordinal)));

                if (match is null)
                {
                    integrante.IngresoMensualNfDjs.Remove(ingresoExistente);
                    uow.IngresoMensualNfDjs.Remove(ingresoExistente);
                    continue;
                }

                ingresoExistente.NominalIngresoNfDj = match.NominalIngresoNfDj;
                ingresoExistente.DescuentoslegalesIngresoNf = match.DescuentoslegalesIngresoNf;
                ingresoExistente.LiquidoIngresoNfDj = match.LiquidoIngresoNfDj;
                ingresoExistente.IdIngresoFront = match.IdIngresoFront;
            }

            foreach (var ingresoNuevo in incoming.Where(i =>
                         !existentes.Any(existing =>
                             existing.IdIngresoMensualNfDj == i.IdIngresoMensualNfDj
                             || (!string.IsNullOrWhiteSpace(i.IdIngresoFront)
                                 && string.Equals(existing.IdIngresoFront, i.IdIngresoFront, StringComparison.Ordinal)))))
            {
                var nuevo = new BusinessLogic.Entities.IngresoMensualNfDj
                {
                    IdIngresoMensualNfDj = ingresoNuevo.IdIngresoMensualNfDj > 0
                        ? ingresoNuevo.IdIngresoMensualNfDj
                        : _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INGRESO_MENSUAL_NF),
                    IdIntegranteNfDj = integrante.IdIntegranteNfDj,
                    NominalIngresoNfDj = ingresoNuevo.NominalIngresoNfDj,
                    DescuentoslegalesIngresoNf = ingresoNuevo.DescuentoslegalesIngresoNf,
                    LiquidoIngresoNfDj = ingresoNuevo.LiquidoIngresoNfDj,
                    IdIngresoFront = ingresoNuevo.IdIngresoFront,
                    ArchivoIngresoNfDj = ingresoNuevo.ArchivoIngresoNfDj,
                    NombreArchivoIngreso = ingresoNuevo.NombreArchivoIngreso,
                    ExtensionArchivoIngreso = ingresoNuevo.ExtensionArchivoIngreso
                };

                integrante.IngresoMensualNfDjs.Add(nuevo);
                uow.IngresoMensualNfDjs.Add(nuevo);
            }
        }

        private static void ActualizarInscriptoPrueba(
            IUnitOfWork uow,
            BusinessLogic.Entities.DeclaracionJuradaWeb declaracion,
            BusinessLogic.Entities.Prueba prueba,
            DateTime fechaActual)
        {
            var inscriptoPrueba = uow.InscriptoPruebas.GetByKey((long)declaracion.IdInscriptoPrueba);
            if (inscriptoPrueba is null)
            {
                return;
            }

            var ingresosNominales = declaracion.IntegranteNfDjs
                .SelectMany(i => i.IngresoMensualNfDjs)
                .Where(i => i.NominalIngresoNfDj.HasValue)
                .Sum(i => i.NominalIngresoNfDj!.Value);

            var descuentosLegales = declaracion.IntegranteNfDjs
                .SelectMany(i => i.IngresoMensualNfDjs)
                .Where(i => i.DescuentoslegalesIngresoNf.HasValue)
                .Sum(i => i.DescuentoslegalesIngresoNf!.Value);

            var egresos = declaracion.EgresoMensualNfDjs
                .Where(e => e.MontoEgresoMensualNfDj.HasValue)
                .Sum(e => e.MontoEgresoMensualNfDj!.Value);

            inscriptoPrueba.EstadoInscriptoPrueba = FondoDeBecaConstants.Declaracion.EstadoInscriptoPruebaEnviado;
            inscriptoPrueba.FechaEstadoInscriptoPrueba = fechaActual;
            inscriptoPrueba.DjFechaInscriptoPrueba = fechaActual;
            inscriptoPrueba.DjIntegranInscriptoPrueba = declaracion.IntegranteNfDjs.Count;
            inscriptoPrueba.DjIngresoInscriptoPrueba = ingresosNominales == 0 ? null : ingresosNominales;
            inscriptoPrueba.DjDtoLegalInscriptoPrueba = descuentosLegales == 0 ? null : (long?)descuentosLegales;
            inscriptoPrueba.DjEgresosInscriptoPrueba = egresos == 0 ? null : egresos;

            if (!string.IsNullOrWhiteSpace(declaracion.ModalidadPostulacionDj))
            {
                inscriptoPrueba.RangoInscriptoPrueba = declaracion.ModalidadPostulacionDj;
            }

            if (prueba.IdTipoBeca == FondoDeBecaConstants.Declaracion.TipoDescuentoAntecedentesAcademicos && declaracion.CodigoInstitucionUniv.HasValue)
            {
                inscriptoPrueba.CodigoUniversidad = (long)declaracion.CodigoInstitucionUniv.Value;
            }

            declaracion.WarningDeclaracionJurada = descuentosLegales < egresos ? CommonConstants.Booleanos.Si : CommonConstants.Booleanos.No;
        }

        private static string CrearSnapshot(DtoDeclaracionJuradaWebDevart dto)
        {
            var snapshot = new
            {
                dto.IdTipoVivienda,
                dto.MontoViviendaDj,
                dto.ObsOtroViviendaDj,
                dto.ObservacionesNfDj,
                dto.OtroActivosNfDj,
                dto.SolventoEstudiosNfDj,
                dto.TienevehiculoNfDj,
                dto.MarcavehiculoNfDj,
                dto.AnioVehiculoNfDj,
                dto.TienecasaveraneoNfDj,
                dto.BalnearioNfDj,
                dto.ObservacionesIngresosNfDj,
                dto.ResidenciaDj,
                dto.ResidiraEnmontevideoDj,
                dto.IdTipoResidencia,
                dto.DetalleResideEnmontevideoDj,
                dto.CoberturasaludResideMontDj,
                dto.GastosTrasladoResideInteDj,
                dto.NominalOtrosingresosNfDj,
                dto.LiquidoOtrosingresosNfDj,
                dto.ObservacionesEgresosNfDj,
                dto.CodigoInstitucionBac,
                dto.FacultadUniversidadDj,
                dto.CarreraUniversitariaDj,
                dto.CantMateriasAprobadasDj,
                dto.CantMatExaReprobadosDj,
                dto.PromTotalCalificacionesDj,
                dto.PromAprlCalificacionesDj,
                dto.UltimoanioSecundariaAprDj,
                dto.OtrosEstudiosDj,
                dto.DetalleOtrosEstudiosDj,
                dto.ExperienciaLaboralDj,
                dto.FechaComienzoTrabajo,
                dto.LugarTrabajoActualDj,
                dto.CargoDj,
                dto.FamiliaresCursandoEnort,
                dto.CodigoInstitucionUniv,
                dto.ModalidadPostulacionDj,
                dto.TipoBachillerato,
                dto.PromedioCalificaiones5to,
                dto.TrabajaActualmente,
                dto.PromedioCalificaiones6to,
                dto.CodigoCiudadDj,
                dto.CodigoEstadoDj,
                dto.NombrePdfRevalidasDj,
                dto.ExtensionPdfRevalidasDj,
                TieneArchivoRevalida = dto.PdfFormRevalidasDj is { Length: > 0 },
                Egresos = (dto.EgresoMensualNfDjs ?? [])
                    .OrderBy(e => e.IdEgresoMensualNfDj)
                    .ThenBy(e => e.IdEgresoFront)
                    .Select(e => new
                    {
                        e.IdEgresoMensualNfDj,
                        e.IdTipoEgresoDj,
                        e.MontoEgresoMensualNfDj,
                        e.DetalleOtroEgreso,
                        e.IdEgresoFront,
                        e.NombreArchivoEgreso,
                        e.ExtensionArchivoEgreso,
                        TieneArchivo = e.ArchivoEgresoMensualNfDj is { Length: > 0 }
                    })
                    .ToList(),
                Integrantes = (dto.IntegranteNfDjs ?? [])
                    .OrderBy(i => i.IdIntegranteNfDj)
                    .Select(i => new
                    {
                        i.IdIntegranteNfDj,
                        i.IdTipoParentesco,
                        i.NombreIntegranteNfDj,
                        i.EdadIntegranteNfDj,
                        i.ActividadIntegranteNfDj,
                        i.DetalleOtroParentesco,
                        Ingresos = (i.IngresoMensualNfDjs ?? [])
                            .OrderBy(ing => ing.IdIngresoMensualNfDj)
                            .ThenBy(ing => ing.IdIngresoFront)
                            .Select(ing => new
                            {
                                ing.IdIngresoMensualNfDj,
                                ing.NominalIngresoNfDj,
                                ing.DescuentoslegalesIngresoNf,
                                ing.LiquidoIngresoNfDj,
                                ing.IdIngresoFront,
                                ing.NombreArchivoIngreso,
                                ing.ExtensionArchivoIngreso,
                                TieneArchivo = ing.ArchivoIngresoNfDj is { Length: > 0 }
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return JsonSerializer.Serialize(snapshot);
        }

        private static bool PerteneceAPersona(IUnitOfWork uow, decimal idDeclaracionJuradaWeb, long codigoPersona)
        {
            var declaracion = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);
            return declaracion?.CodigoPersona == codigoPersona;
        }

        private static OperationResult<BusinessLogic.Entities.IngresoMensualNfDj> ObtenerIngresoAutorizado(
            IUnitOfWork uow,
            long codigoPersona,
            long idIngresoMensualNF,
            string methodName,
            string errorPrefix)
        {
            var ingreso = uow.IngresoMensualNfDjs.GetWithIntegranteYDeclaracion(idIngresoMensualNF);
            if (ingreso is null)
            {
                return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_01",
                    methodName,
                    "No se encontró el ingreso mensual indicado.",
                    404);
            }

            var integrante = ingreso.IntegranteNfDj;
            if (integrante is null)
            {
                return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_02",
                    methodName,
                    "No se encontró el integrante asociado al ingreso mensual indicado.",
                    404);
            }

            var declaracion = integrante.DeclaracionJuradaWeb;
            if (declaracion is null)
            {
                return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_03",
                    methodName,
                    "No se encontró la declaración jurada asociada al ingreso mensual indicado.",
                    404);
            }

            if (declaracion.CodigoPersona != codigoPersona)
            {
                return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_04",
                    methodName,
                    "El ingreso mensual indicado no pertenece a la persona autenticada.",
                    403);
            }

            return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.Ok(ingreso, methodName);
        }

        private static OperationResult<BusinessLogic.Entities.EgresoMensualNfDj> ObtenerEgresoAutorizado(
            IUnitOfWork uow,
            long codigoPersona,
            long idEgresoMensualNF,
            string methodName,
            string errorPrefix)
        {
            var egreso = uow.EgresoMensualNfDjs.GetByKey(idEgresoMensualNF);
            if (egreso is null)
            {
                return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_01",
                    methodName,
                    "No se encontró el egreso mensual indicado.",
                    404);
            }

            if (!PerteneceAPersona(uow, egreso.IdDeclaracionjuradaWeb, codigoPersona))
            {
                return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.IsFailed(
                    $"{errorPrefix}_02",
                    methodName,
                    "El egreso mensual indicado no pertenece a la persona autenticada.",
                    403);
            }

            return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.Ok(egreso, methodName);
        }

        private static OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb> ObtenerDeclaracionAutorizada(
            IUnitOfWork uow,
            long codigoPersona,
            long idDeclaracionJuradaWeb,
            string methodName,
            string errorPrefix)
        {
            var declaracion = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);
            if (declaracion is null)
            {
                return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.IsFailed(
                    $"{errorPrefix}_01",
                    methodName,
                    "No se encontró la declaración jurada indicada.",
                    404);
            }

            if (declaracion.CodigoPersona != codigoPersona)
            {
                return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.IsFailed(
                    $"{errorPrefix}_02",
                    methodName,
                    "La declaración jurada indicada no pertenece a la persona autenticada.",
                    403);
            }

            return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.Ok(declaracion, methodName);
        }

        private static string ConstruirNombreArchivo(string? nombreBase, string extension, string fallback)
        {
            var nombre = string.IsNullOrWhiteSpace(nombreBase) ? fallback : nombreBase.Trim();
            return $"{nombre}{extension}";
        }

        private static string NormalizarExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            var ext = extension.Trim();
            return ext.StartsWith('.') ? ext : $".{ext}";
        }

        private static string ObtenerContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };

        #endregion METODOS PRIVADOS

    }
}
