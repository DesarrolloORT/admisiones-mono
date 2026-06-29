using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.IServices.Becas;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services.Becas
{
    public class BecasService : IBecasService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public BecasService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<DtoAceptacionReglamentoEstDevart> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidad = uow.AceptacionReglamentoEsts.GetByPersona(codigoPersona);
            if (entidad == null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "GEN_ARE_01",
                    nameof(ObtenerAceptacionReglamentoEstudiantil),
                    "No se encontró aceptación del reglamento para la persona.",
                    204);
            }

            return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(entidad.ToDto(), nameof(ObtenerAceptacionReglamentoEstudiantil));
        }

        public OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(long idProducto, long idProceso, long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var producto = uow.Productos.GetByKey(idProducto);
            if (producto == null)
            {
                return OperationResult<IEnumerable<DtoPruebaDevart>>.IsFailed(
                    "GEN_FBV_01",
                    nameof(ObtenerFondosDeBecaVigentes),
                    "El producto indicado es inválido.",
                    400);
            }

            long idNivelProducto = producto.IdNivelProducto;
            var pruebas = uow.Pruebas.GetFondosBecaVigentes(idNivelProducto, 0, codigoPersona, idProducto, idProceso);

            var fechaActual = DateTime.Now;
            var dtos = pruebas
                .Where(p => EstaDisponibleEnFechaActual(p, fechaActual))
                .Select(p => p.ToDtoWithRelated(1));

            return OperationResult<IEnumerable<DtoPruebaDevart>>.Ok(dtos, nameof(ObtenerFondosDeBecaVigentes));
        }

        public OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripcionesConfirmadas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var dtos = uow.VdInscripcionesFresco1y2s
                .GetInscripcionesFrescoHabilitadas(codigoPersona)
                .Where(i => i.EstadoInscripcion == "Confirmada")
                .ToDtos();

            return OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>.Ok(dtos, nameof(ObtenerMisInscripcionesConfirmadas));
        }

        private static bool EstaDisponibleEnFechaActual(Prueba prueba, DateTime fechaActual)
        {
            if (prueba.FechaEntregaDjPrueba?.Date != fechaActual.Date
                || string.IsNullOrWhiteSpace(prueba.HoraEntregaDjPrueba))
            {
                return true;
            }

            var partes = prueba.HoraEntregaDjPrueba.Split(':');
            if (partes.Length < 2
                || !int.TryParse(partes[0], out var hora)
                || !int.TryParse(partes[1], out var minuto))
            {
                return true;
            }

            var fechaEntrega = prueba.FechaEntregaDjPrueba.Value;
            var limite = new DateTime(
                fechaEntrega.Year,
                fechaEntrega.Month,
                fechaEntrega.Day,
                hora,
                minuto,
                0,
                DateTimeKind.Local).AddHours(2);

            return fechaActual <= limite;
        }
    }
}
