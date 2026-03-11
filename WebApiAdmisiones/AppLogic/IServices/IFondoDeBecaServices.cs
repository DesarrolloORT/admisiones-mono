using AppLogic.DevartDTOs;
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

        #endregion
    }
}
