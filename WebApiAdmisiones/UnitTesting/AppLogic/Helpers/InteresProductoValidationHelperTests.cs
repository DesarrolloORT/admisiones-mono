using AppLogic.Helpers.ValidationHelpers;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Helpers
{
    public class InteresProductoValidationHelperTests
    {
        private const string Method = "RegistrarInteresProducto";

        [Fact]
        public void ValidarRegistroInteresProducto_PersonaInexistente_DevuelveNotFound()
        {
            var uow = new Mock<IUnitOfWork>();
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(false);
            uow.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = InteresProductoValidationHelper.ValidarRegistroInteresProducto(uow.Object, 123, 10, 20, Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_ProductoInvalido_DevuelveBadRequest()
        {
            var uow = CrearUowConPersonaValida();
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(false);
            uow.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = InteresProductoValidationHelper.ValidarRegistroInteresProducto(uow.Object, 123, 10, 20, Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_ProcesoNoHabilitado_DevuelveBadRequest()
        {
            var uow = CrearUowConProductoValido();
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(false);
            uow.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var result = InteresProductoValidationHelper.ValidarRegistroInteresProducto(uow.Object, 123, 10, 20, Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_InscripcionPrevia_DevuelveConflict()
        {
            var uow = CrearUowConProcesoHabilitado();
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(true);
            uow.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = InteresProductoValidationHelper.ValidarRegistroInteresProducto(uow.Object, 123, 10, 20, Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_04", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_InscripcionPendiente_DevuelveConflict()
        {
            var uow = CrearUowConInscripcionPreviaNegativa();
            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(true);
            uow.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var result = InteresProductoValidationHelper.ValidarRegistroInteresProducto(uow.Object, 123, 10, 20, Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        private static Mock<IUnitOfWork> CrearUowConPersonaValida()
        {
            var uow = new Mock<IUnitOfWork>();
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            uow.Setup(u => u.Personas).Returns(personaRepo.Object);
            return uow;
        }

        private static Mock<IUnitOfWork> CrearUowConProductoValido()
        {
            var uow = CrearUowConPersonaValida();
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            uow.Setup(u => u.Productos).Returns(productoRepo.Object);
            return uow;
        }

        private static Mock<IUnitOfWork> CrearUowConProcesoHabilitado()
        {
            var uow = CrearUowConProductoValido();
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            uow.Setup(u => u.Procesos).Returns(procesoRepo.Object);
            return uow;
        }

        private static Mock<IUnitOfWork> CrearUowConInscripcionPreviaNegativa()
        {
            var uow = CrearUowConProcesoHabilitado();
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            uow.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            return uow;
        }
    }
}
