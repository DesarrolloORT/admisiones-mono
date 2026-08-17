using AppLogic.Contracts.Dtos;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using System;
using System.Collections.Generic;
using System.Text.Json;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Xunit;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Services;
using AppLogic.Registration.UseCases;
using AppLogic.Authentication.Interfaces;

namespace UnitTesting.AppLogic.Services
{
    public class RegistrationServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly RegistrationUseCases _service;

        /// <summary>Composition root de los casos de uso del registro para los tests.</summary>
        private sealed record RegistrationUseCases(
            IEvaluateDocument EvaluateDocument,
            IVerifyIdentity VerifyIdentity,
            IValidateNewPerson ValidateNewPerson,
            ICompleteNewPerson CompleteNewPerson,
            IConfirmRegistrationRequest ConfirmRegistrationRequest);

        public RegistrationServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _ldapMock = new Mock<ILdap>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);

            var caracteristicaPaisRepo = new Mock<ICaracteristicaPaiRepository>();
            caracteristicaPaisRepo
                .Setup(r => r.GetAll())
                .Returns(new List<CaracteristicaPai>
                {
                    new() { IdCaracteristicaPais = 7, Iso2 = "UY", NombrePais = "Uruguay", Caracteristica = 598 }
                });
            _uowMock.Setup(u => u.CaracteristicaPais).Returns(caracteristicaPaisRepo.Object);

            var ldapDirectory = new LdapUserDirectory(_ldapMock.Object);
            _service = new RegistrationUseCases(
                new EvaluateDocument(_uowFactoryMock.Object, ldapDirectory),
                new VerifyIdentity(
                    _uowFactoryMock.Object,
                    _dbConnectionContextMock.Object,
                    ldapDirectory,
                    _passwordActivationServiceMock.Object,
                    Mock.Of<ILogger<VerifyIdentity>>()),
                new ValidateNewPerson(_uowFactoryMock.Object),
                new CompleteNewPerson(
                    _uowFactoryMock.Object,
                    _dbConnectionContextMock.Object,
                    ldapDirectory,
                    Mock.Of<ILogger<CompleteNewPerson>>()),
                new ConfirmRegistrationRequest(
                    _uowFactoryMock.Object,
                    _dbConnectionContextMock.Object,
                    Mock.Of<ILogger<ConfirmRegistrationRequest>>()));
        }

        [Fact]
        public async Task EvaluarDocumento_NonCiWithoutPersonaOrSolicitud_ReturnsAltaSolicitud()
        {
            var solicitudAltaRepo = new Mock<ISolicitudAltaRepository>();
            solicitudAltaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("PS", "A123")).Returns(default(SolicitudAlta)!);
            _uowMock.Setup(u => u.SolicitudAltas).Returns(solicitudAltaRepo.Object);

            var request = new EvaluateDocumentRequest
            {
                DocumentType = "PS",
                DocumentNumber = "A123"
            };

            var result = await _service.EvaluateDocument.ExecuteAsync(request);

            Assert.True(result.Success);
            Assert.Equal("No existe solicitud de alta para el documento indicado. Se puede continuar con la solicitud de alta.", result.Message);
            Assert.True(result.Data!.RequiresRegistrationRequest);
            Assert.False(result.Data.RequiresPersonRegistration);
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

            var result = await _service.EvaluateDocument.ExecuteAsync(new EvaluateDocumentRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2"
            });

            Assert.True(result.Success);
            Assert.Equal("La cedula ingresada ya está registrada.", result.Message);
            Assert.True(result.Data!.UserAlreadyRegistered);
            Assert.False(result.Data.RequiresIdentityVerification);
            Assert.False(result.Data.RequiresPersonRegistration);
        }

        [Fact]
        public async Task EvaluarDocumento_CiWithoutPersona_ReturnsAltaPersonaPermitida()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(default(Persona)!);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = await _service.EvaluateDocument.ExecuteAsync(new EvaluateDocumentRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2"
            });

            Assert.True(result.Success);
            Assert.Equal("La persona no existe. Se puede continuar con el alta.", result.Message);
            Assert.True(result.Data!.RequiresPersonRegistration);
            Assert.False(result.Data.UserAlreadyRegistered);
            Assert.False(result.Data.RequiresIdentityVerification);
            Assert.False(result.Data.RequiresRegistrationRequest);
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

            var result = await _service.EvaluateDocument.ExecuteAsync(new EvaluateDocumentRequest
            {
                DocumentType = "PS",
                DocumentNumber = "A123"
            });

            Assert.True(result.Success);
            Assert.Equal("El documento ingresado está en revisión.", result.Message);
            Assert.True(result.Data!.RegistrationRequestPending);
            Assert.False(result.Data.RequiresRegistrationRequest);
            Assert.False(result.Data.RequiresPersonRegistration);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public void RegistroEvaluacionResponse_SerializesOnlyTrueFlags()
        {
            var response = new DocumentEvaluationResponse
            {
                RequiresPersonRegistration = true
            };

            var json = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Contains("requiresPersonRegistration", json);
            Assert.DoesNotContain("userAlreadyRegistered", json);
            Assert.DoesNotContain("requiresIdentityVerification", json);
            Assert.DoesNotContain("requiresRegistrationRequest", json);
            Assert.DoesNotContain("registrationRequestPending", json);
        }

        [Fact]
        public async Task EvaluarDocumento_InvalidCi_ReturnsFailure()
        {
            var result = await _service.EvaluateDocument.ExecuteAsync(new EvaluateDocumentRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-1"
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
                .Setup(s => s.SendPasswordLinkMailAsync(
                    It.Is<Persona>(p => p.CodigoPersona == 123),
                    nameof(VerifyIdentity)))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(VerifyIdentity),
                    "Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña."));

            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                Email = "ana@example.com"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.MailSent);
            Assert.NotNull(registroAgregado);
            Assert.Equal(2000, registroAgregado!.IdRegistroAdmisiones);
            Assert.Equal(123, registroAgregado.CodigoPersona);
            Assert.Null(registroAgregado.IdSolicitudAlta);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Once);
            _passwordActivationServiceMock.Verify(
                s => s.SendPasswordLinkMailAsync(It.IsAny<Persona>(), nameof(VerifyIdentity)),
                Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task VerificarIdentidad_WhenMailFails_ReturnsSuccessWithMailEnviadoFalse()
        {
            // SRV-05: un fallo al enviar el mail no debe convertir un registro exitoso en error;
            // el estado parcial va estructurado en MailSent, no solo en el mensaje.
            var personaRepo = new Mock<IPersonaRepository>();
            var registroAdmisionesRepo = new Mock<IRegistroAdmisioneRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
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
                .Setup(s => s.SendPasswordLinkMailAsync(
                    It.Is<Persona>(p => p.CodigoPersona == 123),
                    nameof(VerifyIdentity)))
                .ReturnsAsync(OperationResult<object?>.IsFailed(
                    "ACT_PAS_99",
                    nameof(VerifyIdentity),
                    "No fue posible enviar el mail.",
                    500,
                    default));

            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                Email = "ana@example.com"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data!.MailSent);
        }

        [Fact]
        public async Task VerificarIdentidad_MismatchedData_ReturnsFailure()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(CrearPersonaExistente());
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _ldapMock.Setup(l => l.ExisteUsuarioLDAP("123")).ReturnsAsync(false);

            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Gomez",
                Email = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_PERSONA_VERIF_01", result.ErrorCode);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.SendPasswordLinkMailAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task VerificarIdentidad_WithNonCiDocument_ReturnsFailure()
        {
            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "PS",
                DocumentNumber = "A123",
                FirstSurname = "Perez",
                Email = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_DOC_03", result.ErrorCode);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public async Task ConfirmarSolicitudAlta_WithCiDocument_ReturnsFailure()
        {
            var result = await _service.ConfirmRegistrationRequest.ExecuteAsync(new RegisterPersonRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2"
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

            var result = await _service.ConfirmRegistrationRequest.ExecuteAsync(CrearRegistroPersonaRequest("PS", "A123"));

            Assert.True(result.Success);
            // El front distingue este final del alta de persona por estos dos flags, no por el mensaje.
            Assert.True(result.Data!.PendingReview);
            Assert.False(result.Data.MailSent);
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

            var result = await _service.CompleteNewPerson.ExecuteAsync(new PendingPerson
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                // En Redis el teléfono ya viaja en E.164.
                PrimaryPhone = "+59899123456",
                Email = "ana@example.com",
                CountryId = 1,
                StateId = 2,
                CityId = 3
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
            // SRV-01: la persona se commitea en una tx y el resto (admisión/metadata/imágenes)
            // en otra, con el LDAP en el medio y fuera de ambas.
            _uowMock.Verify(u => u.Commit(), Times.Exactly(2));
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

            var result = await _service.CompleteNewPerson.ExecuteAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new TemporaryDocumentImages
                {
                    ExpirationDate = new DateTime(2030, 1, 1),
                    DocumentFront = new TemporaryDocumentFile
                    {
                        Content = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        FileName = "documento.jpg",
                        ContentType = "image/jpeg"
                    },
                    PersonFace = new TemporaryDocumentFile
                    {
                        Content = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        FileName = "cara.jpg",
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
            _uowMock.Verify(u => u.Commit(), Times.Exactly(2));
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

            var result = await _service.CompleteNewPerson.ExecuteAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new TemporaryDocumentImages
                {
                    DocumentFront = new TemporaryDocumentFile
                    {
                        Content = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                        FileName = "documento.jpg",
                        ContentType = "image/jpeg"
                    }
                });

            Assert.True(result.Success);
            Assert.NotNull(documentoAgregado);
            Assert.Equal("123_1.jpg", documentoAgregado!.NombreImagen);
            Assert.Equal(DateTime.Today.AddYears(1), documentoAgregado.FechaVtoDocumentoPersona);
            _uowMock.Verify(u => u.Imagens, Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Exactly(2));
        }

        [Fact]
        public async Task ValidarNuevaPersona_TelefonoNoCelular_ReturnsFailed()
        {
            var request = CrearRegistroPersonaRequest("CI", "1234567-2");
            request.PrimaryPhone = new PhoneNumber { NationalNumber = "24001234", Iso2 = "UY" };

            var result = await _service.ValidateNewPerson.ExecuteAsync(request);

            Assert.False(result.Success);
            Assert.Equal("REG_TEL_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowMock.Verify(u => u.Personas, Times.Never);
        }

        [Fact]
        public async Task ValidarNuevaPersona_SinTelefono_ReturnsFailed()
        {
            var request = CrearRegistroPersonaRequest("CI", "1234567-2");
            request.PrimaryPhone = new PhoneNumber { Iso2 = "UY" };

            var result = await _service.ValidateNewPerson.ExecuteAsync(request);

            Assert.False(result.Success);
            Assert.Equal("REG_TEL_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ValidarNuevaPersona_PaisSinCaracteristica_ReturnsFailed()
        {
            var request = CrearRegistroPersonaRequest("CI", "1234567-2");
            request.PrimaryPhone = new PhoneNumber { NationalNumber = "91122223333", Iso2 = "AR" };

            var result = await _service.ValidateNewPerson.ExecuteAsync(request);

            Assert.False(result.Success);
            Assert.Equal("REG_TEL_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowMock.Verify(u => u.Personas, Times.Never);
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

            var result = await _service.CompleteNewPerson.ExecuteAsync(
                CrearPendingPersona(),
                "NuevaPassword1!",
                new TemporaryDocumentImages
                {
                    DocumentFront = new TemporaryDocumentFile
                    {
                        Content = [1, 2, 3],
                        FileName = "documento.jpg",
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

            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                Email = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("REG_USUARIO_01", result.ErrorCode);
            _ldapMock.Verify(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()), Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.SendPasswordLinkMailAsync(It.IsAny<Persona>(), It.IsAny<string>()),
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

            var result = await _service.VerifyIdentity.ExecuteAsync(new VerifyIdentityRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                Email = "ana@example.com"
            });

            Assert.False(result.Success);
            Assert.Equal("LDAP_01", result.ErrorCode);
            _uowMock.Verify(u => u.RegistroAdmisiones, Times.Never);
            _passwordActivationServiceMock.Verify(
                s => s.SendPasswordLinkMailAsync(It.IsAny<Persona>(), It.IsAny<string>()),
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

        private static RegisterPersonRequest CrearRegistroPersonaRequest(string documentType, string document)
            => new()
            {
                DocumentType = documentType,
                DocumentNumber = document,
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099123456", Iso2 = "UY" },
                Email = "ana@example.com",
                EmailConfirmation = "ana@example.com",
                CountryId = 1,
                StateId = 2,
                CityId = 3
            };

        [Fact]
        public async Task CompletarNuevaPersonaAsync_WhenCrearUsuarioLdapFails_PersonaStaysCommittedNoRollback()
        {
            // SRV-01: la persona ya está commiteada cuando se llama a LDAP; un fallo acá no
            // debe revertir la fila (no hay Rollback) — el reintento la encuentra vía GetByDocumento.
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA))
                .Returns(123);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "LDAP_CREATE_01",
                    nameof(ILdap.CrearUsuarioAsync),
                    "No se pudo crear el usuario LDAP.",
                    500,
                    false));

            var result = await _service.CompleteNewPerson.ExecuteAsync(CrearPendingPersona(), "NuevaPassword1!");

            Assert.False(result.Success);
            Assert.Equal("LDAP_CREATE_01", result.ErrorCode);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Rollback(), Times.Never);
            _ldapMock.Verify(l => l.ForzarCambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _uowMock.Verify(u => u.RegistroAdmisiones, Times.Never);
        }

        [Fact]
        public async Task CompletarNuevaPersonaAsync_WhenCambiarPasswordLdapFails_PersonaStaysCommittedNoRollback()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            personaRepo.Setup(r => r.GetByDocumento("1234567-2")).Returns(default(Persona)!);
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA))
                .Returns(123);
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CrearUsuarioAsync)));
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync("123", "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "LDAP_PWD_01",
                    nameof(ILdap.ForzarCambiarPasswordAsync),
                    "No se pudo establecer la password.",
                    500,
                    false));

            var result = await _service.CompleteNewPerson.ExecuteAsync(CrearPendingPersona(), "NuevaPassword1!");

            Assert.False(result.Success);
            Assert.Equal("LDAP_PWD_01", result.ErrorCode);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Rollback(), Times.Never);
            _uowMock.Verify(u => u.RegistroAdmisiones, Times.Never);
        }

        private static PendingPerson CrearPendingPersona()
            => new()
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                // En Redis el teléfono ya viaja en E.164.
                PrimaryPhone = "+59899123456",
                Email = "ana@example.com",
                CountryId = 1,
                StateId = 2,
                CityId = 3
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
