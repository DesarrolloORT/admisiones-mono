using AppLogic.Becas.Interfaces;
using AppLogic.DevartDTOs;
using AppLogic.Inscripciones.Constants;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Becas.Services
{
    public class BecasService : IBecasService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public BecasService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripcionesConfirmadas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var dtos = uow.VdInscripcionesFresco1y2s
                .GetInscripcionesFrescoHabilitadas(codigoPersona)
                .Where(i => i.EstadoInscripcion == InscripcionesConstants.EstadoInscripcion.Confirmada)
                .ToDtos();

            return OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>.Ok(dtos, nameof(ObtenerMisInscripcionesConfirmadas));
        }
    }
}
