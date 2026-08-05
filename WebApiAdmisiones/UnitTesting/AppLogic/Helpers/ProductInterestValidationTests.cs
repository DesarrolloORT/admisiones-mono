using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace UnitTesting.AppLogic.Helpers
{
    public class ProductInterestValidationTests
    {
        private const string Method = "RegisterProductInterestRecord";
        private static readonly List<long> IdsOferta = new() { 55 };

        [Fact]
        public void ValidarRegistroInteresProducto_PersonaInexistente_DevuelveNotFound()
        {
            var uow = new Mock<IUnitOfWork>();
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(false);
            uow.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_ProductoInvalido_DevuelveBadRequest()
        {
            var uow = CrearUowConPersonaValida();
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(false);
            uow.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_ProcesoNoHabilitado_DevuelveBadRequest()
        {
            var uow = CrearUowConProductoValido(productLevelId: 1);
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(false);
            uow.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_InscripcionPrevia_DevuelveConflict()
        {
            var uow = CrearUowConProcesoHabilitado(productLevelId: 1);
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(true);
            uow.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

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

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_Nivel3y4OfertaNueva_PermiteAunqueOtraOfertaTengaInscripcion()
        {
            var uow = CrearUowConProcesoHabilitado(productLevelId: 3);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.TieneInteresRegistradoParaOferta(123, 20, 10, IdsOferta))
                .Returns(false);
            uow.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var rechazo = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta);

            Assert.Equal(ProductInterestRejection.None, rechazo);
            uow.VerifyGet(u => u.Inscriptos, Times.Never);
            uow.VerifyGet(u => u.InstanciaWorkflows, Times.Never);
        }

        [Fact]
        public void ValidarRegistroInteresProducto_Nivel3y4OfertaYaRegistrada_DevuelveConflict()
        {
            var uow = CrearUowConProcesoHabilitado(productLevelId: 4);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.TieneInteresRegistradoParaOferta(123, 20, 10, IdsOferta))
                .Returns(true);
            uow.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = ProductInterestValidation.ValidateProductInterestRegistration(uow.Object, 123, 10, 20, IdsOferta).ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal("GEN_IP_06", result.ErrorCode);
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

        private static Mock<IUnitOfWork> CrearUowConProductoValido(long productLevelId)
        {
            var uow = CrearUowConPersonaValida();
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = productLevelId });
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            uow.Setup(u => u.Productos).Returns(productoRepo.Object);
            return uow;
        }

        private static Mock<IUnitOfWork> CrearUowConProcesoHabilitado(long productLevelId)
        {
            var uow = CrearUowConProductoValido(productLevelId);
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            uow.Setup(u => u.Procesos).Returns(procesoRepo.Object);
            return uow;
        }

        private static Mock<IUnitOfWork> CrearUowConInscripcionPreviaNegativa()
        {
            var uow = CrearUowConProcesoHabilitado(productLevelId: 1);
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            uow.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            return uow;
        }
    }
}
