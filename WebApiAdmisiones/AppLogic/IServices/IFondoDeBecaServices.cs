using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IFondoDeBecaServices
    {
        #region TIPOS DECLARACIÓN JURADA

        /// <summary>
        /// Devuelve todos los tipos de parentesco disponibles.
        /// </summary>
        OperationResult<IEnumerable<DtoTipoParentescoDevart>> ObtenerTiposParentesco();

        /// <summary>
        /// Devuelve los tipos de egreso activos ordenados por campo Orden.
        /// </summary>
        OperationResult<IEnumerable<DtoTipoEgresoDjDevart>> ObtenerTiposEgreso();

        /// <summary>
        /// Devuelve todos los tipos de vivienda disponibles.
        /// </summary>
        OperationResult<IEnumerable<DtoTipoViviendaDevart>> ObtenerTiposVivienda();

        #endregion

        #region UNIVERSIDADES

        /// <summary>
        /// Devuelve las universidades disponibles. Valida que el país indicado exista.
        /// </summary>
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades(long codigoPais);

        OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>> ObtenerFormulariosDeclaracionJuradaWeb(long codigoPersona);

        OperationResult<DtoDeclaracionJuradaWebDevart> ObtenerFormularioDeclaracionJuradaWebDetalle(long codigoPersona, long idInscriptoPrueba);

        OperationResult<bool> SubirArchivoIngreso(long codigoPersona, long idIngresoMensualNF, byte[] fileContent, string fileName);

        OperationResult<ArchivoDescargaDto> DescargarArchivoIngreso(long codigoPersona, long idIngresoMensualNF);

        OperationResult<bool> EliminarArchivoIngreso(long codigoPersona, long idIngresoMensualNF);

        OperationResult<bool> SubirArchivoEgreso(long codigoPersona, long idEgresoMensualNF, byte[] fileContent, string fileName);

        OperationResult<ArchivoDescargaDto> DescargarArchivoEgreso(long codigoPersona, long idEgresoMensualNF);

        OperationResult<bool> EliminarArchivoEgreso(long codigoPersona, long idEgresoMensualNF);

        OperationResult<bool> SubirArchivoRevalidaDJ(long codigoPersona, long idDeclaracionJuradaWeb, byte[] fileContent, string fileName);

        #endregion
    }
}
