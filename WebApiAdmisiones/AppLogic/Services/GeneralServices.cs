using AppLogic.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using Utilities;
using System.Security.AccessControl;

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

       
        #endregion CONSULTAS GENERALES

    }
}
