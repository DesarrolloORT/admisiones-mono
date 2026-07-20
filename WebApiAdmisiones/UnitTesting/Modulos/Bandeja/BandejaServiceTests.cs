// File: UnitTesting/ModBandejaAppLogic/BandejaServiceTests.cs
using System;
using Moq;
using Xunit;
using ModBandejaAppLogic.Services;
using ModBandejaAppLogic.DevartDTOs;
using ModBandejaAppLogic.Interfaces;
using ModBandejaBusinessLogic;
using ModBandejaBusinessLogic.Entities;
using ModBandejaBusinessLogic.IDevartRepositories;

namespace UnitTesting.Modulos
{
    public class BandejaServiceTests
    {
        private readonly Mock<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory> _mockUowFactory;
        private readonly Mock<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWork> _mockUow;

        public BandejaServiceTests()
        {
            _mockUowFactory = new Mock<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory>();
            _mockUow = new Mock<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWork>();
            _mockUowFactory.Setup(f => f.Create()).Returns(_mockUow.Object);
        }

        [Fact]
        public void GetInstanciaById_ReturnsDto()
        {
            var instancia = new InstanciaWorkflow { IdInstanciaWorkflow = 1, DescripcionInstanciaWorkflow = "desc" };
            var dto = new DtoInstanciaWorkflowDevartModBandeja { IdInstanciaWorkflow = 1, DescripcionInstanciaWorkflow = "desc" };

            var repo = new Mock<IInstanciaWorkflowRepository>();
            repo.Setup(r => r.GetByKey(1)).Returns(instancia);
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(repo.Object);

            // Static converter, so use Moq.Protected or just let it run if it is simple
            // Here, we assume ToDto is a simple mapping, so we can test the result directly

            var service = new BandejaService(_mockUowFactory.Object);

            // Patch: InstanciaWorkflowConverterModBandeja.ToDto is static, so can't mock, but we can check the result
            var result = service.GetInstanciaById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Data.IdInstanciaWorkflow);
        }

        [Fact]
        public void AltaEnInstanciaWF_AddsAndReturnsId()
        {
            var dto = new DtoInstanciaWorkflowDevartModBandeja
            {
                IdInstanciaWorkflow = 0,
                DescripcionInstanciaWorkflow = "desc",
                UsuarioIngreso = "user",
                HoraIngreso = "12:00",
                FechaIngreso = DateTime.Now,
                IdProceso = 1
            };
            var entidad = new InstanciaWorkflow
            {
                IdInstanciaWorkflow = 0,
                DescripcionInstanciaWorkflow = "desc",
                UsuarioIngreso = "user",
                HoraIngreso = "12:00",
                FechaIngreso = dto.FechaIngreso,
                IdProceso = 1
            };

            var repo = new Mock<IInstanciaWorkflowRepository>();
            repo.Setup(r => r.NextId(It.IsAny<string>())).Returns(123);
            repo.Setup(r => r.Add(It.IsAny<InstanciaWorkflow>()));
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(repo.Object);
            _mockUow.Setup(u => u.Save());

            // Patch: InstanciaWorkflowConverterModBandeja.ToEntity is static, so just use the same object
            // (Assume mapping is correct for this test)

            var service = new BandejaService(_mockUowFactory.Object);

            var result = service.AltaEnInstanciaWF(dto);

            Assert.Equal(123, result.Data);
            repo.Verify(r => r.Add(It.Is<InstanciaWorkflow>(i => i.IdInstanciaWorkflow == 123)), Times.Once);
            _mockUow.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void AltaEnBandejaWF_AddsAndReturnsId()
        {
            var dto = new DtoBandejaDevartModBandeja
            {
                IdBandeja = 0,
                IdInstanciaWorkflow = 1,
                IdEstadoProceso = 2,
                IdGrupoResponsable = 3,
                UsuarioIngreso = "user",
                HoraIngreso = "12:00",
                FechaIngreso = DateTime.Now
            };
            var entidad = new Bandeja
            {
                IdBandeja = 0,
                IdInstanciaWorkflow = 1,
                IdEstadoProceso = 2,
                IdGrupoResponsable = 3,
                UsuarioIngreso = "user",
                HoraIngreso = "12:00",
                FechaIngreso = dto.FechaIngreso
            };

            var repo = new Mock<IBandejaRepository>();
            repo.Setup(r => r.NextId(It.IsAny<string>())).Returns(456);
            repo.Setup(r => r.Add(It.IsAny<Bandeja>()));
            _mockUow.Setup(u => u.Bandejas).Returns(repo.Object);
            _mockUow.Setup(u => u.Save());

            // Patch: BandejaConverterModBandeja.ToEntity is static, so just use the same object

            var service = new BandejaService(_mockUowFactory.Object);

            var result = service.AltaEnBandejaWF(dto);

            Assert.Equal(456, result.Data);
            repo.Verify(r => r.Add(It.Is<Bandeja>(b => b.IdBandeja == 456)), Times.Once);
            _mockUow.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void AltaTramiteWorkflow_AddsTramiteInstanciaAndOneBandejaPerEstado_SavesOnce()
        {
            var dtoTramite = new DtoTramiteBandejaDevartModBandeja
            {
                CodigoPersona = 1,
                IdGrupoResponsable = 39,
                IdProceso = 89,
                TitularTramiteBandeja = "Inscripcion corporativa",
                UsuarioIngreso = "user",
                FechaIngreso = DateTime.Now,
                HoraIngreso = "12:00"
            };
            var dtoInstancia = new DtoInstanciaWorkflowDevartModBandeja
            {
                IdInstanciaWorkflow = 0,
                DescripcionInstanciaWorkflow = "desc",
                UsuarioIngreso = "user",
                HoraIngreso = "12:00",
                FechaIngreso = DateTime.Now,
                IdProceso = 89
            };

            var tramiteRepo = new Mock<ITramiteBandejaRepository>();
            tramiteRepo.Setup(r => r.NextId(It.IsAny<string>())).Returns(789);
            tramiteRepo.Setup(r => r.Add(It.IsAny<TramiteBandeja>()));
            _mockUow.Setup(u => u.TramiteBandejas).Returns(tramiteRepo.Object);

            var instanciaRepo = new Mock<IInstanciaWorkflowRepository>();
            instanciaRepo.Setup(r => r.NextId(It.IsAny<string>())).Returns(123);
            instanciaRepo.Setup(r => r.Add(It.IsAny<InstanciaWorkflow>()));
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(instanciaRepo.Object);

            var bandejaRepo = new Mock<IBandejaRepository>();
            bandejaRepo.SetupSequence(r => r.NextId(It.IsAny<string>()))
                .Returns(456)
                .Returns(457);
            bandejaRepo.Setup(r => r.Add(It.IsAny<Bandeja>()));
            _mockUow.Setup(u => u.Bandejas).Returns(bandejaRepo.Object);

            _mockUow.Setup(u => u.Save());

            var service = new BandejaService(_mockUowFactory.Object);

            var result = service.AltaTramiteWorkflow(dtoTramite, dtoInstancia, new long[] { 8104, 8105 }, 39);

            Assert.True(result.Success);
            Assert.Equal(123, result.Data);
            tramiteRepo.Verify(r => r.Add(It.Is<TramiteBandeja>(t => t.IdTramiteBandeja == 789)), Times.Once);
            instanciaRepo.Verify(r => r.Add(It.Is<InstanciaWorkflow>(i => i.IdInstanciaWorkflow == 123 && i.IdTramiteBandeja == 789)), Times.Once);
            bandejaRepo.Verify(r => r.Add(It.Is<Bandeja>(b => b.IdInstanciaWorkflow == 123 && b.IdEstadoProceso == 8104 && b.IdGrupoResponsable == 39)), Times.Once);
            bandejaRepo.Verify(r => r.Add(It.Is<Bandeja>(b => b.IdInstanciaWorkflow == 123 && b.IdEstadoProceso == 8105 && b.IdGrupoResponsable == 39)), Times.Once);
            _mockUow.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GetInstanciaById_InstanciaNotFound_ReturnsFailed()
        {
            var repo = new Mock<IInstanciaWorkflowRepository>();
            repo.Setup(r => r.GetByKey(1)).Returns((InstanciaWorkflow)null);
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(repo.Object);

            var service = new BandejaService(_mockUowFactory.Object);
            var result = service.GetInstanciaById(1);

            Assert.False(result.Success);
            Assert.Equal("404", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void Constructor_NullUowFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BandejaService(null));
        }

        [Fact]
        public void GetBandejaWFCompleta_ReturnsDtos()
        {
            List<InstanciaWorkflow> flatResult = new List<InstanciaWorkflow>
            {
                new InstanciaWorkflow { IdInstanciaWorkflow = 1, UsuarioIngreso = "user1", FechaIngreso = DateTime.Now, HoraIngreso = "10:00" },
                new InstanciaWorkflow { IdInstanciaWorkflow = 2, UsuarioIngreso = "user2", FechaIngreso = DateTime.Now, HoraIngreso = "11:00" }
            };
            var repo = new Mock<IInstanciaWorkflowRepository>();
            repo.Setup(r => r.GetBandejaWFCompleta("usuario")).Returns(flatResult.Cast<dynamic>().ToList());
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(repo.Object);

            var service = new BandejaService(_mockUowFactory.Object);
            var result = service.GetBandejaWFCompleta("usuario");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Contains(result.Data, d => d.IdInstanciaWorkflow == 1);
            Assert.Contains(result.Data, d => d.IdInstanciaWorkflow == 2);
        }

        [Fact]
        public void GetBandejaWFCompletaTipado_ReturnsInstancias()
        {
            var flatResult = new List<dynamic>
            {
                new InstanciaWorkflow { IdInstanciaWorkflow = 1, UsuarioIngreso = "user1", FechaIngreso = DateTime.Now, HoraIngreso = "10:00" },
                new InstanciaWorkflow { IdInstanciaWorkflow = 2, UsuarioIngreso = "user2", FechaIngreso = DateTime.Now, HoraIngreso = "11:00" }
            };
            var repo = new Mock<IInstanciaWorkflowRepository>();
            repo.Setup(r => r.GetBandejaWFCompleta("usuario")).Returns(flatResult);
            _mockUow.Setup(u => u.InstanciaWorkflows).Returns(repo.Object);

            var service = new BandejaService(_mockUowFactory.Object);
            var method = typeof(BandejaService).GetMethod("GetBandejaWFCompletaTipado", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var instancias = (List<InstanciaWorkflow>)method.Invoke(service, new object[] { "usuario" });

            Assert.Equal(2, instancias.Count);
            Assert.Equal("user1", instancias[0].UsuarioIngreso);
            Assert.Equal("user2", instancias[1].UsuarioIngreso);
        }
    }
}