using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class ProcesoComienzoServices : IProcesoComienzoServices
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public ProcesoComienzoServices(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        #region PROCESO COMIENZO

        public OperationResult<IEnumerable<DtoProcesoComienzoDevart>> ObtenerProcesoComienzos()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.ProcesoComienzos.GetAllWithRelated().ToDtosWithRelated(1);
            return OperationResult<IEnumerable<DtoProcesoComienzoDevart>>.Ok(entidades, nameof(ObtenerProcesoComienzos));
        }

        public OperationResult<DtoProcesoComienzoDevart> ObtenerProcesoComienzo(long idProceso, long idComienzo)
        {
            using var uow = _uowFactory.Create();
            var entidad = uow.ProcesoComienzos.GetByKeyWithRelated(idProceso, idComienzo);
            if (entidad == null)
                return OperationResult<DtoProcesoComienzoDevart>.IsFailed("PC_GPC_01", nameof(ObtenerProcesoComienzo), "ProcesoComienzo no encontrado.", 404);

            return OperationResult<DtoProcesoComienzoDevart>.Ok(entidad.ToDtoWithRelated(1), nameof(ObtenerProcesoComienzo));
        }

        public OperationResult<DtoProcesoComienzoDevart> GuardarProcesoComienzo(ProcesoComienzoRequest dto)
        {
            using var uow = _uowFactory.Create();
            var existente = uow.ProcesoComienzos.GetByKey(dto.IdProceso, dto.IdComienzo);
            if (existente != null)
                return OperationResult<DtoProcesoComienzoDevart>.IsFailed("PC_SPC_01", nameof(GuardarProcesoComienzo), "ProcesoComienzo ya existe.", 400);

            var entidad = new ProcesoComienzo
            {
                IdProceso = dto.IdProceso,
                IdComienzo = dto.IdComienzo,
                HoraIngreso = dto.HoraIngreso,
                FechaIngreso = dto.FechaIngreso,
                UsuarioIngreso = dto.UsuarioIngreso
            };

            uow.ProcesoComienzos.Add(entidad);
            uow.Save();

            return OperationResult<DtoProcesoComienzoDevart>.Ok(entidad.ToDto(), nameof(GuardarProcesoComienzo));
        }

        public OperationResult<DtoProcesoComienzoDevart> ModificarProcesoComienzo(ProcesoComienzoRequest dto)
        {
            using var uow = _uowFactory.Create();
            var entidad = uow.ProcesoComienzos.GetByKey(dto.IdProceso, dto.IdComienzo);
            if (entidad == null)
                return OperationResult<DtoProcesoComienzoDevart>.IsFailed("PC_APC_01", nameof(ModificarProcesoComienzo), "ProcesoComienzo no encontrado.", 404);

            entidad.HoraIngreso = dto.HoraIngreso;
            entidad.FechaIngreso = dto.FechaIngreso;
            entidad.UsuarioIngreso = dto.UsuarioIngreso;

            uow.Save();

            return OperationResult<DtoProcesoComienzoDevart>.Ok(entidad.ToDto(), nameof(ModificarProcesoComienzo));
        }

        #endregion PROCESO COMIENZO
    }
}