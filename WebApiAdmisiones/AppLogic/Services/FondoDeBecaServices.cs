using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class FondoDeBecaServices : IFondoDeBecaServices
    {
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
                return OperationResult<IEnumerable<DtoEmpresaDevart>>.IsFailed(
                    "FDB_UV_01",
                    nameof(ObtenerUniversidades),
                    "El país indicado es inválido.",
                    400);

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
    }
}