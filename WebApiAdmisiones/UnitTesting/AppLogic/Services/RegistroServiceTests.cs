using AppLogic.Registro.Requests;
using AppLogic.Registro.Responses;
using AppLogic.Registro.Dtos;
using System;
using System.Collections.Generic;
using System.Text.Json;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Moq;
using Utilities;
using Xunit;
using AppLogic.Registro.Services;
using AppLogic.Registro.Interfaces;
using AppLogic.Autenticacion.Interfaces;

namespace UnitTesting.AppLogic.Services
{
    public class RegistroServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly RegistroService _service;

        public RegistroServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _ldapMock = new Mock<ILdap>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new RegistroService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _ldapMock.Object,
                _passwordActivationServiceMock.Object);
        }

        [Fact]
        public async Task EvaluarDocumento_NonCiWithoutPersonaOrSolicitud_ReturnsAltaSolicitud()
        {
            var solicitudAltaRepo = new Mock<ISolicitudAltaRepository>();
            solicitudAltaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("PS", "A123")).Returns(default(SolicitudAlta)!);
            _uowMock.Setup(u => u.SolicitudAltas).Returns(solicitudAltaRepo.Object);

            var request = new DtoRegistroEvaluarDocumentoRequest
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

            var result = await _service.EvaluarDocumentoAsync(new DtoRegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            });

            Assert.True(result.Success);
            Assert.Equal("La cedula ingresada ya está registrada.", result.Message);
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

            var result = await _service.EvaluarDocumentoAsync(new DtoRegistroEvaluarDocumentoRequest
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

            var result = await _service.EvaluarDocumentoAsync(new DtoRegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "PS",
                Documento = "A123"
            });

            Assert.True(result.Success);
            Assert.Equal("El documento ingresado está en revisión.", result.Message);
            Assert.True(result.Data!.SolicitudAltaExistente);
            Assert.False(result.Data.RequiereAltaSolicitud);
            Assert.False(result.Data.RequiereAltaPersona);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public void RegistroEvaluacionResponse_SerializesOnlyTrueFlags()
        {
            var response = new DtoRegistroEvaluacionResponse
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
            var result = await _service.EvaluarDocumentoAsync(new DtoRegistroEvaluarDocumentoRequest
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
            RegistroAdmisione? registroAgregado = null;
            var personaRepo = new Mock<IPersonaRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            registroAdmisionesRepo
                .Setup(r => r.Add(It.IsAny<RegistroAdmisione>()))
                .Callback<RegistroAdmisione>(r => registroAgregado = r);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(registroAdmisionesRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CrearUsuarioAsync)));
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES))
                .Returns(2000);
            _passwordActivationServiceMock
                .Setup(s => s.EnviarMailLinkPasswordAsync(
                    It.Is<Persona>(p => p.CodigoPersona == 123),
                    nameof(IRegistroService.VerificarIdentidadAsync)))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.VerificarIdentidadAsync),
                    "Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña."));

            var result = await _service.VerificarIdentidadAsync(new DtoRegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                Mail = "ana@example.com"
            });

            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(registroAgregado);
            Assert.Equal(2000, registroAgregado!.IdRegistroAdmisiones);
            Assert.Equal(123, registroAgregado.CodigoPersona);
            Assert.Null(registroAgregado.IdSolicitudAlta);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Once);
            _passwordActivationServiceMock.Verify(
                s => s.EnviarMailLinkPasswordAsync(It.IsAny<Persona>(), nameof(IRegistroService.VerificarIdentidadAsync)),
                Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task VerificarIdentidad_MismatchedData_ReturnsFailure()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);

            var result = await _service.VerificarIdentidadAsync(new DtoRegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Gomez",
                Mail = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_PERSONA_VERIF_01", result.ErrorCode);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.EnviarMailLinkPasswordAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task VerificarIdentidad_WithNonCiDocument_ReturnsFailure()
        {
            var result = await _service.VerificarIdentidadAsync(new DtoRegistroVerificarIdentidadRequest
            {
                TipoDocumento = "PS",
                Documento = "A123",
                PrimerApellido = "Perez",
                Mail = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_DOC_03", result.ErrorCode);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public async Task ConfirmarSolicitudAlta_WithCiDocument_ReturnsFailure()
        {
            var result = await _service.ConfirmarSolicitudAltaAsync(new DtoRegistroPersonaRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_DOC_03", result.ErrorCode);
            _uowMock.Verify(u => u.Productos, Times.Never);
        }

        [Fact]
        public async Task ConfirmarSolicitudAlta_DoesNotRequireProductOrProcess()
        {
            SolicitudAlta? solicitudAgregada = null;
            RegistroAdmisione? registroAgregado = null;
            var solicitudAltaRepo = new Mock<ISolicitudAltaRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            solicitudAltaRepo
                .Setup(r => r.Add(It.IsAny<SolicitudAlta>()))
                .Callback<SolicitudAlta>(s => solicitudAgregada = s);
            registroAdmisionesRepo
                .Setup(r => r.Add(It.IsAny<RegistroAdmisione>()))
                .Callback<RegistroAdmisione>(r => registroAgregado = r);
            _uowMock.Setup(u => u.SolicitudAltas).Returns(solicitudAltaRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(registroAdmisionesRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_SOLICITUD_ALTA))
                .Returns(1000);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES))
                .Returns(2000);

            var result = await _service.ConfirmarSolicitudAltaAsync(CrearRegistroPersonaRequest("PS", "A123"));

            Assert.True(result.Success);
            Assert.NotNull(solicitudAgregada);
            Assert.Null(solicitudAgregada!.IdProducto);
            Assert.Null(solicitudAgregada.IdProceso);
            Assert.NotNull(registroAgregado);
            Assert.Equal(2000, registroAgregado!.IdRegistroAdmisiones);
            Assert.Null(registroAgregado.CodigoPersona);
            Assert.Equal(1000, registroAgregado.IdSolicitudAlta);
            _uowMock.Verify(u => u.Productos, Times.Never);
            _uowMock.Verify(u => u.Procesos, Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersonaAsync_DoesNotCreateInteresOrActividad()
        {
            RegistroAdmisione? registroAgregado = null;
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            registroAdmisionesRepo
                .Setup(r => r.Add(It.IsAny<RegistroAdmisione>()))
                .Callback<RegistroAdmisione>(r => registroAgregado = r);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(registroAdmisionesRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA))
                .Returns(123);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES))
                .Returns(2000);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CrearUsuarioAsync)));
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync("123", "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));

            var result = await _service.CompletarNuevaPersonaAsync(new DtoRegistroPendingPersona
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Sexo = "F",
                Direccion = "Calle 1",
                Telefono1 = "099123456",
                Email = "ana@example.com",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3
            }, "NuevaPassword1!");

            Assert.True(result.Success);
            Assert.Equal(123, result.Data);
            Assert.NotNull(registroAgregado);
            Assert.Equal(2000, registroAgregado!.IdRegistroAdmisiones);
            Assert.Equal(123, registroAgregado.CodigoPersona);
            Assert.Null(registroAgregado.IdSolicitudAlta);
            _uowMock.Verify(u => u.Interes, Times.Never);
            _uowMock.Verify(u => u.InteresProductos, Times.Never);
            _uowMock.Verify(u => u.PersonaAdmites, Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersonaAsync_WithTemporaryImages_PersistsPhotoAndDocument()
        {
            Imagen? fotoAgregada = null;
            ImagenTemporal? documentoAgregado = null;
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            var imagenRepo = new Mock<IImagenRepository>();
            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            imagenRepo
                .Setup(r => r.Add(It.IsAny<Imagen>()))
                .Callback<Imagen>(i => fotoAgregada = i);
            imagenTemporalRepo
                .Setup(r => r.Add(It.IsAny<ImagenTemporal>()))
                .Callback<ImagenTemporal>(i => documentoAgregado = i);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(registroAdmisionesRepo.Object);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);
            ConfigurarIdsAltaPersonaConImagenes();
            ConfigurarLdapAltaPersona();

            var result = await _service.CompletarNuevaPersonaAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new DtoRegistroDocumentoImagenesTemporales
                {
                    FechaVencimiento = new DateTime(2030, 1, 1),
                    DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                    {
                        Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        NombreArchivo = "documento.jpg",
                        ContentType = "image/jpeg"
                    },
                    CaraPersona = new DtoRegistroDocumentoArchivoTemporal
                    {
                        Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        NombreArchivo = "cara.jpg",
                        ContentType = "image/jpeg"
                    }
                });

            Assert.True(result.Success);
            Assert.NotNull(fotoAgregada);
            Assert.Equal(4000, fotoAgregada!.IdImagen);
            Assert.Equal(123, fotoAgregada.CodigoPersona);
            Assert.Equal("3", fotoAgregada.TipoImagen);
            Assert.Equal("123_3.jpg", fotoAgregada.NombreImagen);
            Assert.NotNull(documentoAgregado);
            Assert.Equal(3000, documentoAgregado!.IdImagenTemporal);
            Assert.Equal(123, documentoAgregado.CodigoPersona);
            Assert.Equal("1", documentoAgregado.TipoImagen);
            Assert.Equal("123_1.jpg", documentoAgregado.NombreImagen);
            Assert.Equal(new DateTime(2030, 1, 1), documentoAgregado.FechaVtoDocumentoPersona);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersonaAsync_WithTemporaryDocumentOnly_DoesNotPersistPhoto()
        {
            ImagenTemporal? documentoAgregado = null;
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            imagenTemporalRepo
                .Setup(r => r.Add(It.IsAny<ImagenTemporal>()))
                .Callback<ImagenTemporal>(i => documentoAgregado = i);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(registroAdmisionesRepo.Object);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);
            ConfigurarIdsAltaPersonaConImagenes();
            ConfigurarLdapAltaPersona();

            var result = await _service.CompletarNuevaPersonaAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new DtoRegistroDocumentoImagenesTemporales
                {
                    DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                    {
                        Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        NombreArchivo = "documento.jpg",
                        ContentType = "image/jpeg"
                    }
                });

            Assert.True(result.Success);
            Assert.NotNull(documentoAgregado);
            Assert.Equal("123_1.jpg", documentoAgregado!.NombreImagen);
            Assert.Equal(DateTime.Today.AddYears(1), documentoAgregado.FechaVtoDocumentoPersona);
            _uowMock.Verify(u => u.Imagens, Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersonaAsync_WithInvalidTemporaryDocument_ReturnsFailureWithoutCommit()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var result = await _service.CompletarNuevaPersonaAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new DtoRegistroDocumentoImagenesTemporales
                {
                    DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                    {
                        Archivo = [1, 2, 3],
                        NombreArchivo = "documento.jpg",
                        ContentType = "image/jpeg"
                    }
                });

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Never);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task VerificarIdentidad_ExistingLdapUser_ReturnsConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(true);

            var result = await _service.VerificarIdentidadAsync(new DtoRegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                Mail = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_USUARIO_01", result.ErrorCode);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.EnviarMailLinkPasswordAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task VerificarIdentidad_WhenLdapCreateFails_DoesNotRegisterAdmision()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "LDAP_01",
                    nameof(ILdap.CrearUsuarioAsync),
                    "No se pudo crear el usuario LDAP.",
                    500,
                    false));

            var result = await _service.VerificarIdentidadAsync(new DtoRegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                Mail = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("LDAP_01", result.ErrorCode);
            _uowMock.Verify(u => u.RegistroAdmisiones, Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.EnviarMailLinkPasswordAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
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

        private static DtoRegistroPersonaRequest CrearRegistroPersonaRequest(string tipoDocumento, string documento)
            => new()
            {
                TipoDocumento = tipoDocumento,
                Documento = documento,
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Sexo = "F",
                Direccion = "Calle 1",
                Telefono1 = "099123456",
                Mail = "ana@example.com",
                VerificacionMail = "ana@example.com",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3
            };

        private static DtoRegistroPendingPersona CrearPendingPersona()
            => new()
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Sexo = "F",
                Direccion = "Calle 1",
                Telefono1 = "099123456",
                Email = "ana@example.com",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3
            };

        private void ConfigurarIdsAltaPersonaConImagenes()
        {
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA))
                .Returns(123);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES))
                .Returns(2000);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(3000);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN))
                .Returns(4000);
        }

        private void ConfigurarLdapAltaPersona()
        {
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CrearUsuarioAsync)));
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync("123", "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
        }
    }
}
