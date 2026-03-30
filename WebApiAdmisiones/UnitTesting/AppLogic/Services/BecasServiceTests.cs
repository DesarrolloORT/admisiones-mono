using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using System.Linq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class BecasServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly BecasService _service;

        public BecasServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new BecasService(_uowFactoryMock.Object, _dbConnectionContextMock.Object);
        }

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IAceptacionReglamentoEstRepository>();
            repo.Setup(r => r.GetByPersona(123)).Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(repo.Object);

            var result = _service.ObtenerAceptacionReglamentoEstudiantil(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_ARE_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_Found_ReturnsDto()
        {
            var repo = new Mock<IAceptacionReglamentoEstRepository>();
            repo.Setup(r => r.GetByPersona(123)).Returns(new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = 88,
                CodigoPersona = 123,
                IdProducto = 10,
                IdComienzo = 20,
                IdSistema = 25
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(repo.Object);

            var result = _service.ObtenerAceptacionReglamentoEstudiantil(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(88, result.Data.IdAceptacionReglamentoEst);
            Assert.Equal(123, result.Data.CodigoPersona);
        }

        [Fact]
        public void RegistrarAceptacionReglamentoEstudiantil_PersonaNotFound_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns((Persona)null);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _service.RegistrarAceptacionReglamentoEstudiantil(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_RARE_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void RegistrarAceptacionReglamentoEstudiantil_EncuestaNotFound_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona { CodigoPersona = 123 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.RegistrarAceptacionReglamentoEstudiantil(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_RARE_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void RegistrarAceptacionReglamentoEstudiantil_EncuestaSinProductoOComienzoValidos_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona { CodigoPersona = 123 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                CodigoPersona = 123,
                IdProducto = 0,
                IdComienzo = 20
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.RegistrarAceptacionReglamentoEstudiantil(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_RARE_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void RegistrarAceptacionReglamentoEstudiantil_Duplicated_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona { CodigoPersona = 123 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                CodigoPersona = 123,
                IdProducto = 10,
                IdComienzo = 20
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 10, 20)).Returns(new AceptacionReglamentoEst
            {
                CodigoPersona = 123,
                IdProducto = 10,
                IdComienzo = 20,
                IdSistema = 25
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = _service.RegistrarAceptacionReglamentoEstudiantil(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_RARE_04", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void RegistrarAceptacionReglamentoEstudiantil_ValidData_CreatesRecord()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona { CodigoPersona = 123 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                CodigoPersona = 123,
                IdProducto = 10,
                IdComienzo = 20
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST)).Returns(999);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 10, 20)).Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = _service.RegistrarAceptacionReglamentoEstudiantil(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(999, result.Data.IdAceptacionReglamentoEst);
            Assert.Equal(123, result.Data.CodigoPersona);
            Assert.Equal(10, result.Data.IdProducto);
            Assert.Equal(20, result.Data.IdComienzo);
            Assert.Equal(25, result.Data.IdSistema);
            aceptacionRepo.Verify(r => r.Add(It.Is<AceptacionReglamentoEst>(a =>
                a.IdAceptacionReglamentoEst == 999 &&
                a.CodigoPersona == 123 &&
                a.IdProducto == 10 &&
                a.IdComienzo == 20 &&
                a.IdSistema == 25)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ObtenerFondosDeBecaVigentes_ProductoInvalido_ReturnsFailed()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(2)).Returns((Producto)null);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerFondosDeBecaVigentes(2, 3, 4);

            Assert.False(result.Success);
            Assert.Equal("GEN_FBV_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ObtenerFondosDeBecaVigentes_ProductoValido_ReturnsMappedItems()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(2)).Returns(new Producto { IdProducto = 2, IdNivelProducto = 5 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetFondosBecaVigentes(5, 0, 4, 2, 3)).Returns(
            [
                new Prueba
                {
                    IdPrueba = 1,
                    IdTipoBeca = 11,
                    IdComienzo = 10,
                    IdComienzoPrueba = 10,
                    FechaEntregaDjPrueba = DateTime.Today,
                    HoraEntregaDjPrueba = DateTime.Now.AddHours(1).ToString("HH:mm"),
                    TipoDescuento = new TipoDescuento
                    {
                        IdTipoDescuento = 11,
                        NombreTipoDescuento = "Fondo vigente",
                        EsFijoTipoDescuento = "NO",
                        AliasTipoDescuento = "FV",
                        ReimputableTipoDescuento = "SI"
                    },
                    Comienzo = new Comienzo { IdComienzo = 10, NombreComienzo = "Marzo" }
                }
            ]);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var result = _service.ObtenerFondosDeBecaVigentes(2, 3, 4);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(1, item.IdPrueba);
            Assert.Equal(11, item.IdTipoBeca);
        }
    }
}
