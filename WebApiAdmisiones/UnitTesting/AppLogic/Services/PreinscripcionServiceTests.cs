using AppLogic.DevartDTOs;
using AppLogic.Interfaces;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PreinscripcionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly PreinscripcionService _service;

        public PreinscripcionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _generalServiceMock = new Mock<IGeneralService>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PreinscripcionService(_uowFactoryMock.Object, _generalServiceMock.Object);
        }

        [Fact]
        public void ObtenerFechaVencimientoAdmisiones_ProcesoSinFecha_ReturnsFailed()
        {
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 2))
                .Returns(OperationResult<DateTime>.IsFailed("GEN_FVA_01", "CalcularFechaVencimientoAdmisiones", "Problema con la carga de fecha del comienzo del proceso.", 400));

            var result = _service.ObtenerFechaVencimientoAdmisiones(1, 2);

            Assert.False(result.Success);
            Assert.Equal("GEN_FVA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ObtenerFechaVencimientoAdmisiones_HappyPath_DelegatesToGeneralService()
        {
            var fecha = new DateTime(2026, 3, 25);
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 2))
                .Returns(OperationResult<DateTime>.Ok(fecha, "CalcularFechaVencimientoAdmisiones"));

            var result = _service.ObtenerFechaVencimientoAdmisiones(1, 2);

            Assert.True(result.Success);
            Assert.Equal(fecha, result.Data);
        }

        [Fact]
        public void ObtenerDatosPreInscripcion_SinEncuesta_ReturnsNoContentFailure()
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(1)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.ObtenerDatosPreInscripcion(1);

            Assert.False(result.Success);
            Assert.Equal("PRE_DPI_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerDatosPreInscripcion_SinFechaGuardada_CalculaVencimientoYMapeaOferta()
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(1)).Returns(new EncuestaIniAdmision
            {
                CodigoPersona = 1,
                IdProducto = 10,
                IdProceso = 20,
                IdTurno = 30,
                IdComienzo = 40,
                TipoInscripcion = "ONLINE",
                Producto = new Producto { IdProducto = 10, NombreProducto = "ATI", NombreExtensoProducto = "Analista en TI" },
                Proceso = new Proceso { IdProceso = 20, NombreProceso = "Marzo 2026" },
                Comienzo = new Comienzo { IdComienzo = 40, NombreComienzo = "Abril 2026" },
                Turno = new Turno { IdTurno = 30, NombreTurno = "Nocturno" }
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetOfertasParaInscripcionConProceso(10, 20, 30))
                .Returns([
                    new Oferta
                    {
                        IdOferta = 50,
                        IdSupraoferta = 60,
                        IdTurno = 30,
                        IdLocalidad = 1,
                        InscripcionesAbiertasOferta = "SI",
                        AceptaCondicionalesOferta = "SI",
                        NombreCortoOferta = "A01",
                        MinimoCreditosOferta = 1,
                        Supraoferta = new Supraoferta
                        {
                            IdSupraoferta = 60,
                            IdComienzo = 40,
                            IdPaquete = 70,
                            IdMoneda = "UYU",
                            Paquete = new Paquete
                            {
                                IdPaquete = 70,
                                IdProducto = 10,
                                GeneraPlanAnclaPaquete = "SI",
                                MinimoCreditosPaquete = 1
                            }
                        },
                        Turno = new Turno { IdTurno = 30, NombreTurno = "Nocturno" },
                        Localidad = new Localidad { IdLocalidad = 1, NombreLocalidad = "Montevideo" }
                    }
                ]);
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 20))
                .Returns(OperationResult<DateTime>.Ok(new DateTime(2026, 4, 15), "CalcularFechaVencimientoAdmisiones"));

            var result = _service.ObtenerDatosPreInscripcion(1);

            Assert.True(result.Success);
            Assert.Equal(20, result.Data.IdProceso);
            Assert.Equal(40, result.Data.IdComienzo);
            Assert.Equal(10, result.Data.IdProducto);
            Assert.Equal("ONLINE", result.Data.TipoInscripcion);
            Assert.Equal(new DateTime(2026, 4, 15), result.Data.FechaVencimiento);
            Assert.NotNull(result.Data.ObjTurno);
            Assert.Equal(30, result.Data.ObjTurno!.IdTurno);
            Assert.NotNull(result.Data.ObjOferta);
            Assert.Equal(50, result.Data.ObjOferta!.IdOferta);
        }
    }
}
