using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class FondoDeBecaServices : IFondoDeBecaServices
    {
        private static readonly List<string> AllowedArchivoExtensions =
        [
            ".pdf", ".jpg", ".jpeg", ".png"
        ];

        private readonly IUnitOfWorkFactory _uowFactory;

        public FondoDeBecaServices(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
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
                .Where(e => e.Activo == "SI")
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

        public OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>> ObtenerFormulariosDeclaracionJuradaWeb(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var declaraciones = uow.DeclaracionJuradaWebs
                .GetFormulariosAdmisionesVigentes(codigoPersona)
                .Select(MapDeclaracionBase)
                .Select(declaracion => declaracion.ToAdmisionesDto(ObtenerPrueba(uow, (long)declaracion.IdInscriptoPrueba)))
                .ToList();

            if (!declaraciones.Any())
            {
                return OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>>.IsFailed(
                    "FDB_FDJ_01",
                    nameof(ObtenerFormulariosDeclaracionJuradaWeb),
                    "No se encontraron formularios de declaración jurada web vigentes para la persona.",
                    404);
            }

            return OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>>.Ok(
                declaraciones,
                nameof(ObtenerFormulariosDeclaracionJuradaWeb));
        }

        public OperationResult<DtoDeclaracionJuradaWebDevart> ObtenerFormularioDeclaracionJuradaWebDetalle(long codigoPersona, long idInscriptoPrueba)
        {
            using var uow = _uowFactory.Create();

            var entidad = uow.DeclaracionJuradaWebs.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba);
            if (entidad is null)
            {
                return OperationResult<DtoDeclaracionJuradaWebDevart>.IsFailed(
                    "FDB_FDJ_02",
                    nameof(ObtenerFormularioDeclaracionJuradaWebDetalle),
                    "No se encontró el formulario de declaración jurada web para la inscripción indicada.",
                    404);
            }

            var detalle = MapDeclaracionBase(entidad);
            detalle.ObjPrueba = ObtenerPrueba(uow, idInscriptoPrueba);

            if (entidad.CodigoInstitucionBac is decimal codigoInstitucionBac)
            {
                detalle.NombreBachillerato = uow.Empresas.GetByKey((long)codigoInstitucionBac)?.Nombre;
            }

            if (entidad.IdTipoVivienda is decimal idTipoVivienda)
            {
                var tipoVivienda = uow.TipoViviendas.GetByKey(idTipoVivienda);
                if (tipoVivienda is not null)
                {
                    detalle.ObjTipoVivienda = tipoVivienda.ToDto();
                }
            }

            return OperationResult<DtoDeclaracionJuradaWebDevart>.Ok(
                detalle,
                nameof(ObtenerFormularioDeclaracionJuradaWebDetalle));
        }

        public OperationResult<bool> SubirArchivoIngreso(long codigoPersona, long idIngresoMensualNF, byte[] fileContent, string fileName)
        {
            var archivoValidado = ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoIngreso));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoIngreso),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var ingreso = uow.IngresoMensualNfDjs.GetByKey(idIngresoMensualNF);

            if (ingreso is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_SAI_01",
                    nameof(SubirArchivoIngreso),
                    "No se encontró el ingreso mensual indicado.",
                    404);
            }

            ingreso.NombreArchivoIngreso = Path.GetFileNameWithoutExtension(archivoValidado.Data) ?? string.Empty;
            ingreso.ExtensionArchivoIngreso = Path.GetExtension(archivoValidado.Data) ?? string.Empty;
            ingreso.ArchivoIngresoNfDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoIngreso));
        }

        public OperationResult<bool> SubirArchivoEgreso(long codigoPersona, long idEgresoMensualNF, byte[] fileContent, string fileName)
        {
            var archivoValidado = ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoEgreso));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoEgreso),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var egreso = uow.EgresoMensualNfDjs.GetByKey(idEgresoMensualNF);

            if (egreso is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_SAE_01",
                    nameof(SubirArchivoEgreso),
                    "No se encontró el egreso mensual indicado.",
                    404);
            }

            if (!PerteneceAPersona(uow, egreso.IdDeclaracionjuradaWeb, codigoPersona))
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_SAE_02",
                    nameof(SubirArchivoEgreso),
                    "El egreso mensual indicado no pertenece a la persona autenticada.",
                    403);
            }

            egreso.NombreArchivoEgreso = Path.GetFileNameWithoutExtension(archivoValidado.Data) ?? string.Empty;
            egreso.ExtensionArchivoEgreso = Path.GetExtension(archivoValidado.Data) ?? string.Empty;
            egreso.ArchivoEgresoMensualNfDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoEgreso));
        }

        public OperationResult<bool> SubirArchivoRevalidaDJ(long codigoPersona, long idDeclaracionJuradaWeb, byte[] fileContent, string fileName)
        {
            var archivoValidado = ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoRevalidaDJ));
            if (!archivoValidado.Success)
            {
                return OperationResult<bool>.IsFailed(
                    archivoValidado.ErrorCode,
                    nameof(SubirArchivoRevalidaDJ),
                    archivoValidado.Message,
                    archivoValidado.HttpCode);
            }

            using var uow = _uowFactory.Create();
            var declaracion = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);

            if (declaracion is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_SAR_01",
                    nameof(SubirArchivoRevalidaDJ),
                    "No se encontró la declaración jurada indicada.",
                    404);
            }

            if (declaracion.CodigoPersona != codigoPersona)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_SAR_02",
                    nameof(SubirArchivoRevalidaDJ),
                    "La declaración jurada indicada no pertenece a la persona autenticada.",
                    403);
            }

            declaracion.NombrePdfRevalidasDj = Path.GetFileNameWithoutExtension(archivoValidado.Data);
            declaracion.ExtensionPdfRevalidasDj = Path.GetExtension(archivoValidado.Data);
            declaracion.PdfFormRevalidasDj = fileContent;
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(SubirArchivoRevalidaDJ));
        }

        #endregion

        private static DtoDeclaracionJuradaWebDevart MapDeclaracionBase(BusinessLogic.Entities.DeclaracionJuradaWeb entity)
        {
            var dto = entity.ToDto();
            dto.Persona = entity.Persona?.ToDto();
            dto.Producto = entity.Producto?.ToDto();
            dto.TipoDescuento = entity.TipoDescuento?.ToDto();
            return dto;
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
            dto.TipoDescuento = prueba.TipoDescuento?.ToDto();
            dto.Comienzo = prueba.Comienzo?.ToDto();
            return dto;
        }

        private static OperationResult<string> ValidarArchivoAdjunto(byte[] fileContent, string fileName, string methodName)
        {
            var validacion = FileValidationHelper.ValidateFile(fileContent, fileName, AllowedArchivoExtensions, methodName);
            if (!validacion.Success)
            {
                return OperationResult<string>.IsFailed(
                    validacion.ErrorCode,
                    methodName,
                    validacion.Message,
                    validacion.HttpCode);
            }

            return FileValidationHelper.SanitizeFileName(fileName, AllowedArchivoExtensions, methodName);
        }

        private static bool PerteneceAPersona(IUnitOfWork uow, decimal idDeclaracionJuradaWeb, long codigoPersona)
        {
            var declaracion = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);
            return declaracion?.CodigoPersona == codigoPersona;
        }
    }
}
