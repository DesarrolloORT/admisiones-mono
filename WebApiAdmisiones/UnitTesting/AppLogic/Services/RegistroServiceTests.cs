using System;
using System.Collections.Generic;
using System.Text.Json;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RegistroServiceTests
    {
        private readonly Mock<ICatalogosService> _catalogosServiceMock;
        private readonly Mock<IPreinscripcionService> _preinscripcionServiceMock;
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly RegistroService _service;

        public RegistroServiceTests()
        {
            _catalogosServiceMock = new Mock<ICatalogosService>();
            _preinscripcionServiceMock = new Mock<IPreinscripcionService>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new RegistroService(
                _catalogosServiceMock.Object,
                _preinscripcionServiceMock.Object,
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _ldapMock.Object);
        }

        [Fact]
        public async Task EvaluarDocumento_NonCiWithoutPersonaOrSolicitud_ReturnsAltaSolicitud()
        {
            var solicitudAltaRepo = new Mock<ISolicitudAltaRepository>();
            solicitudAltaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("PS", "A123")).Returns(default(SolicitudAlta)!);
            _uowMock.Setup(u => u.SolicitudAltas).Returns(solicitudAltaRepo.Object);

            var request = new RegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "PS",
                Documento = "A123"
            };

            var result = await _service.EvaluarDocumentoAsync(request);

            Assert.True(result.Success);
            Assert.Equal("No existe solicitud de alta para el documento indicado. Se puede continuar con la solicitud de alta.", result.Message);
            Assert.True(result.Data!.RequiereAltaSolicitud);
            Assert.False(result.Data.RequiereAltaPersona);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public async Task EvaluarDocumento_CiExistingLdapUser_ReturnsUsuarioExistente()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(new Persona
            {
                CodigoPersona = 123,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                PrimerNombreMay = "ANA",
                PrimerApellidoMay = "PEREZ",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
                Documento = "1234567-2",
                TipoDocumento = "CI"
            });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(true);

            var result = await _service.EvaluarDocumentoAsync(new RegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            });

            Assert.True(result.Success);
            Assert.Equal("Ya estás registrado. Para acceder, ingresá con tu número de usuario y tu contraseña.", result.Message);
            Assert.True(result.Data!.UsuarioExistente);
            Assert.False(result.Data.RequiereVerificacion);
            Assert.False(result.Data.RequiereAltaPersona);
        }

        [Fact]
        public async Task EvaluarDocumento_CiWithoutPersona_ReturnsAltaPersonaPermitida()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(default(Persona)!);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = await _service.EvaluarDocumentoAsync(new RegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            });

            Assert.True(result.Success);
            Assert.Equal("La persona no existe. Se puede continuar con el alta.", result.Message);
            Assert.True(result.Data!.RequiereAltaPersona);
            Assert.False(result.Data.UsuarioExistente);
            Assert.False(result.Data.RequiereVerificacion);
            Assert.False(result.Data.RequiereAltaSolicitud);
            _uowMock.Verify(u => u.SolicitudAltas, Times.Never);
        }

        [Fact]
        public async Task EvaluarDocumento_WithoutPersonaButWithSolicitudAlta_ReturnsExistingSolicitudMessage()
        {
            var solicitudAltaRepo = new Mock<ISolicitudAltaRepository>();
            solicitudAltaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("PS", "A123")).Returns(new SolicitudAlta
            {
                IdSolicitudAlta = 10,
                TipoDocumentoSolicitudAlta = "PS",
                DocumentoSolicitudAlta = "A123",
                PrimerApellidoSolicitudAlta = "Perez",
                PrimerNombreSolicitudAlta = "Ana"
            });
            _uowMock.Setup(u => u.SolicitudAltas).Returns(solicitudAltaRepo.Object);

            var result = await _service.EvaluarDocumentoAsync(new RegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "PS",
                Documento = "A123"
            });

            Assert.True(result.Success);
            Assert.Equal("Ya existe una solicitud de alta para el documento indicado.", result.Message);
            Assert.True(result.Data!.SolicitudAltaExistente);
            Assert.False(result.Data.RequiereAltaSolicitud);
            Assert.False(result.Data.RequiereAltaPersona);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public void RegistroEvaluacionResponse_SerializesOnlyTrueFlags()
        {
            var response = new RegistroEvaluacionResponse
            {
                RequiereAltaPersona = true
            };

            var json = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Contains("requiereAltaPersona", json);
            Assert.DoesNotContain("usuarioExistente", json);
            Assert.DoesNotContain("requiereVerificacion", json);
            Assert.DoesNotContain("requiereAltaSolicitud", json);
            Assert.DoesNotContain("solicitudAltaExistente", json);
        }

        [Fact]
        public async Task EvaluarDocumento_InvalidCi_ReturnsFailure()
        {
            var result = await _service.EvaluarDocumentoAsync(new RegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-1"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_DOC_02", result.ErrorCode);
        }

        [Fact]
        public async Task VerificarIdentidad_ExistingWithoutLdapAndMatchingData_ReturnsSuccess()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);

            var result = await _service.VerificarIdentidadAsync(new RegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                Mail = "ana@example.com",
                VerificacionMail = "ana@example.com"
            });

            Assert.True(result.Success);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task VerificarIdentidad_MismatchedData_ReturnsFailure()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);

            var result = await _service.VerificarIdentidadAsync(new RegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Gomez",
                Mail = "ana@example.com",
                VerificacionMail = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_PERSONA_VERIF_01", result.ErrorCode);
        }

        [Fact]
        public async Task ConfirmarNuevaPersona_InvalidDocument_ReturnsFailure()
        {
            var result = await _service.ConfirmarNuevaPersonaAsync(new RegistroConfirmarNuevaPersonaRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-1"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_DOC_02", result.ErrorCode);
        }

        [Fact]
        public async Task ConfirmarPersonaExistente_InvalidProduct_ReturnsFailure()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(99)).Returns(false);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = await _service.ConfirmarPersonaExistenteAsync(new RegistroConfirmarPersonaExistenteRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                IdProducto = 99,
                IdProceso = 20
            });

            Assert.False(result.Success);
            Assert.Equal("REG_PRODUCTO_01", result.ErrorCode);
        }

        [Fact]
        public async Task ConfirmarPersonaExistente_ExistingWithoutLdap_OnlyRequiresProductAndProcess()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var productoRepo = new Mock<IProductoRepository>();
            var procesoRepo = new Mock<IProcesoRepository>();
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            var interesRepo = new Mock<IIntereRepository>();
            var interesProductoRepo = new Mock<IInteresProductoRepository>();
            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            var actividadRepo = new Mock<IActividadRepository>();
            var accionRepo = new Mock<IAccionRepository>();

            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            inscriptoRepo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns(default(Inscripto)!);
            interesRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns([]);
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns(default(PersonaAdmite)!);

            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            _uowMock.Setup(u => u.Interes).Returns(interesRepo.Object);
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(1000);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_ACTIVIDAD))
                .Returns(2000);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_ACCION))
                .Returns(3000);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CrearUsuarioAsync)));
            _ldapMock
                .Setup(l => l.EnviarContrasenia("123", "CI", "1234567-2", "Perez", "ADMISIONES", "REGISTRO"))
                .ReturnsAsync(OperationResult<string>.Ok("ok", nameof(ILdap.EnviarContrasenia)));

            var result = await _service.ConfirmarPersonaExistenteAsync(new RegistroConfirmarPersonaExistenteRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                IdProducto = 10,
                IdProceso = 20
            });

            Assert.True(result.Success);
            Assert.Equal("Registro realizado correctamente.", result.Message);
            interesRepo.Verify(r => r.Add(It.IsAny<Intere>()), Times.Once);
            interesProductoRepo.Verify(r => r.Add(It.IsAny<InteresProducto>()), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        private static Persona CrearPersonaExistente()
        {
            return new Persona
            {
                CodigoPersona = 123,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                PrimerApellidoMay = "PEREZ",
                Email = "ana@example.com",
                Documento = "1234567-2",
                TipoDocumento = "CI",
                CodigoVigencia = "SI"
            };
        }
    }
}
