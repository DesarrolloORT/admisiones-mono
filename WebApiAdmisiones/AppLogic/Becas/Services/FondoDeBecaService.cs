using AppLogic.Becas.Dtos;
using AppLogic.DevartDTOs;
using AppLogic.Becas.Validators;
using AppLogic.Becas.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Becas.Services
{
    public class FondoDeBecaService : IFondoDeBecaServices
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public FondoDeBecaService(IUnitOfWorkFactory uowFactory)
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
            var entidades = uow.TipoEgresoDjs.GetActivosOrdenados().ToList();
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

        #region MÉTODOS PRIVADOS

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

        #endregion MÉTODOS PRIVADOS

    }
}
