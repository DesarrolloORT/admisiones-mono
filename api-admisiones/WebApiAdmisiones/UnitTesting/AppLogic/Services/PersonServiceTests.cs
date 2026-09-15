using AppLogic.Contracts.Constants;
using AppLogic.Contracts.Dtos;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.People.Dtos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Xunit;
using AppLogic.People.Contracts;
using AppLogic.People.UseCases;

namespace UnitTesting.AppLogic.Services
{
    public class PersonServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IPersonaRepository> _personaRepositoryMock;
        private readonly Mock<IInscriptoRepository> _inscriptoRepositoryMock;
        private readonly Mock<IVdInscripcionesFresco1y2Repository> _vdInscripcionesFresco1y2RepositoryMock;
        private readonly Mock<IVdInscripcionesFresco3y4Repository> _vdInscripcionesFresco3y4RepositoryMock;
        private readonly Mock<BusinessLogic.IDevartRepositories.ICiudadRepository> _ciudadRepositoryMock;
        private readonly Mock<IImagenRepository> _imagenRepositoryMock;
        private readonly Mock<IImagenTemporalRepository> _imagenTemporalRepositoryMock;
        private readonly Mock<ICaracteristicaPaiRepository> _caracteristicaPaisRepositoryMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly PeopleUseCases _service;

        /// <summary>Composition root de los casos de uso de People para los tests.</summary>
        private sealed record PeopleUseCases(
            IGetPersonDetails Details,
            IUpdatePersonDetails Update,
            IGetMyEnrollments Enrollments,
            IChangePassword Password,
            IGetPersonIdentityDocument Document,
            IUploadPersonIdentityDocument UploadDocument);

        public PersonServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _personaRepositoryMock = new Mock<IPersonaRepository>();
            _inscriptoRepositoryMock = new Mock<IInscriptoRepository>();
            _vdInscripcionesFresco1y2RepositoryMock = new Mock<IVdInscripcionesFresco1y2Repository>();
            _vdInscripcionesFresco3y4RepositoryMock = new Mock<IVdInscripcionesFresco3y4Repository>();
            _ciudadRepositoryMock = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            _imagenRepositoryMock = new Mock<IImagenRepository>();
            _imagenTemporalRepositoryMock = new Mock<IImagenTemporalRepository>();
            _caracteristicaPaisRepositoryMock = new Mock<ICaracteristicaPaiRepository>();
            _ldapMock = new Mock<ILdap>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.Personas).Returns(_personaRepositoryMock.Object);
            _uowMock.Setup(u => u.Inscriptos).Returns(_inscriptoRepositoryMock.Object);
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(_vdInscripcionesFresco1y2RepositoryMock.Object);
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(_vdInscripcionesFresco3y4RepositoryMock.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(_ciudadRepositoryMock.Object);
            _uowMock.Setup(u => u.Imagens).Returns(_imagenRepositoryMock.Object);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(_imagenTemporalRepositoryMock.Object);
            _uowMock.Setup(u => u.CaracteristicaPais).Returns(_caracteristicaPaisRepositoryMock.Object);
            _caracteristicaPaisRepositoryMock
                .Setup(r => r.GetAll())
                .Returns(new List<CaracteristicaPai>
                {
                    new() { IdCaracteristicaPais = 7, Iso2 = "UY", NombrePais = "Uruguay", Caracteristica = 598 }
                });
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("ADMISIONES");
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(It.IsAny<long>())).Returns(false);

            _service = new PeopleUseCases(
                new GetPersonDetails(_uowFactoryMock.Object),
                new UpdatePersonDetails(_uowFactoryMock.Object),
                new GetMyEnrollments(_uowFactoryMock.Object),
                new ChangePassword(_ldapMock.Object, Mock.Of<ILogger<ChangePassword>>()),
                new GetPersonIdentityDocument(_uowFactoryMock.Object),
                new UploadPersonIdentityDocument(_uowFactoryMock.Object, _dbConnectionContextMock.Object));
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithBothSides_ReturnsFrenteAndDorso()
        {
            var fechaFrente = DateTime.Today.AddYears(1);
            var fechaDorso = DateTime.Today.AddYears(2);
            _imagenTemporalRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns(new ImagenTemporal
                {
                    NombreImagen = "123_1.pdf",
                    BlobImagen = ValidPdf(),
                    FechaVtoDocumentoPersona = fechaFrente
                });
            _imagenTemporalRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 2))
                .Returns(new ImagenTemporal
                {
                    NombreImagen = "123_2.pdf",
                    BlobImagen = ValidPdf(),
                    FechaVtoDocumentoPersona = fechaDorso
                });

            var result = _service.Document.Execute(123);

            Assert.True(result.Success);
            Assert.Equal("123_1.pdf", result.Data!.Front!.FileName);
            Assert.Equal("123_2.pdf", result.Data.Back!.FileName);
            Assert.Equal(ValidPdf(), result.Data.Front.Content);
            Assert.Equal(ValidPdf(), result.Data.Back.Content);
            Assert.Equal(fechaFrente, result.Data.ExpirationDate);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithOnlyFrente_ReturnsOnlyFrente()
        {
            _imagenTemporalRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns(new ImagenTemporal
                {
                    NombreImagen = "123_1.pdf",
                    BlobImagen = ValidPdf(),
                    FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
                });

            var result = _service.Document.Execute(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data!.Front);
            Assert.Null(result.Data.Back);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithOnlyDorso_ReturnsOnlyDorso()
        {
            var fechaDorso = DateTime.Today.AddYears(1);
            _imagenTemporalRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 2))
                .Returns(new ImagenTemporal
                {
                    NombreImagen = "123_2.pdf",
                    BlobImagen = ValidPdf(),
                    FechaVtoDocumentoPersona = fechaDorso
                });

            var result = _service.Document.Execute(123);

            Assert.True(result.Success);
            Assert.Null(result.Data!.Front);
            Assert.NotNull(result.Data.Back);
            Assert.Equal(fechaDorso, result.Data.ExpirationDate);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithDefinitiveDocument_UsesPersonaExpirationDate()
        {
            var fechaPersona = DateTime.Today.AddYears(3);
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona
                {
                    CodigoPersona = 123,
                    FechaVtoDocumentoPersona = fechaPersona
                });
            _imagenRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns(new Imagen
                {
                    NombreImagen = "123_1.pdf",
                    BlobImagen = ValidPdf()
                });

            var result = _service.Document.Execute(123);

            Assert.True(result.Success);
            Assert.Equal("123_1.pdf", result.Data!.Front!.FileName);
            Assert.Null(result.Data.Back);
            Assert.Equal(fechaPersona, result.Data.ExpirationDate);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithDocumentAndNoExpirationDate_ReturnsNullExpirationDate()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona
                {
                    CodigoPersona = 123,
                    FechaVtoDocumentoPersona = null
                });
            _imagenRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns(new Imagen
                {
                    NombreImagen = "123_1.pdf",
                    BlobImagen = ValidPdf()
                });

            var result = _service.Document.Execute(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data!.Front);
            Assert.Null(result.Data.ExpirationDate);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithNoSides_ReturnsNotFound()
        {
            var result = _service.Document.Execute(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithExpiredExistingSide_ReturnsConflict()
        {
            _imagenTemporalRepositoryMock
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns(new ImagenTemporal
                {
                    NombreImagen = "123_1.pdf",
                    BlobImagen = ValidPdf(),
                    FechaVtoDocumentoPersona = DateTime.Today.AddDays(-1)
                });

            var result = _service.Document.Execute(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_03", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void SubirDocumentoPersona_WithBothSides_CreatesBothAndSavesOnce()
        {
            var person = new Persona { CodigoPersona = 123 };
            var fecha = DateTime.Today.AddYears(1);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(10)
                .Returns(11);

            var result = _service.UploadDocument.Execute(
                123,
                fecha,
                new IdentityDocumentFile { FileName = "frente.png", Content = ValidPng() },
                new IdentityDocumentFile { FileName = "dorso.png", Content = ValidPng() });

            Assert.True(result.Success);
            Assert.Equal(fecha, person.FechaVtoDocumentoPersona);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i =>
                    i.IdImagenTemporal == 10 &&
                    i.CodigoPersona == 123 &&
                    i.NombreImagen == "123_1.jpg" &&
                    i.TipoImagen == "1")),
                Times.Once);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i =>
                    i.IdImagenTemporal == 11 &&
                    i.CodigoPersona == 123 &&
                    i.NombreImagen == "123_2.jpg" &&
                    i.TipoImagen == "1")),
                Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_WithPng_CreatesBothAndSavesOnce()
        {
            var person = new Persona { CodigoPersona = 123 };
            var fecha = DateTime.Today.AddYears(1);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(10)
                .Returns(11);

            var result = _service.UploadDocument.Execute(
                123,
                fecha,
                new IdentityDocumentFile { FileName = "frente.png", Content = ValidPng() },
                new IdentityDocumentFile { FileName = "dorso.png", Content = ValidPng() });

            Assert.True(result.Success);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i => i.NombreImagen == "123_1.jpg")),
                Times.Once);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i => i.NombreImagen == "123_2.jpg")),
                Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_WhenFechaIsExpired_ReturnsConflict()
        {
            var result = _service.UploadDocument.Execute(
                123,
                DateTime.Today.AddDays(-1),
                new IdentityDocumentFile { FileName = "frente.png", Content = ValidPng() },
                new IdentityDocumentFile { FileName = "dorso.png", Content = ValidPng() });

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_05", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            _uowFactoryMock.Verify(f => f.Create(), Times.Never);
            _personaRepositoryMock.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            _imagenTemporalRepositoryMock.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var result = _service.UploadDocument.Execute(
                123,
                DateTime.Today.AddYears(1),
                new IdentityDocumentFile { FileName = "frente.png", Content = ValidPng() },
                new IdentityDocumentFile { FileName = "dorso.png", Content = ValidPng() });

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_WhenDorsoIsMissing_ReturnsBadRequest()
        {
            var result = _service.UploadDocument.Execute(
                123,
                DateTime.Today.AddYears(1),
                new IdentityDocumentFile { FileName = "frente.png", Content = ValidPng() },
                new IdentityDocumentFile { FileName = "dorso.png", Content = Array.Empty<byte>() });

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _personaRepositoryMock.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void ObtenerMisInscripciones_DevuelveInscripcionesFresco1y2Y3y4()
        {
            _vdInscripcionesFresco1y2RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco1y2>
                {
                    new()
                    {
                        CodigoPersona = 123,
                        IdProducto = 10,
                        IdProceso = 1,
                        NombreExtensoProducto = "Carrera nivel 1",
                        IdNivelProducto = 1,
                        FechaReferencia = DateTime.Today,
                        FechaVtoInscr = new DateTime(2026, 7, 1),
                        VengoDe = "1y2"
                    }
                });

            _vdInscripcionesFresco3y4RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    new()
                    {
                        CodigoPersona = 123,
                        IdProducto = 30,
                        IdProceso = 2,
                        NombreExtensoProducto = "Curso nivel 3",
                        IdNivelProducto = 3,
                        FechaReferencia = DateTime.Today,
                        FechaVtoInscr = new DateTime(2026, 8, 15),
                        VengoDe = "3y4"
                    }
                });

            var result = _service.Enrollments.Execute(123);

            Assert.True(result.Success);
            var grupos = Assert.IsAssignableFrom<IEnumerable<MyEnrollmentsResponse>>(result.Data).ToList();
            Assert.Equal(2, grupos.Count);
            Assert.All(grupos, g => Assert.Single(g.Enrollments));
            Assert.Equal(new DateTime(2026, 7, 1), grupos[0].PaymentDueDate);
            Assert.Equal(new DateTime(2026, 8, 15), grupos[1].PaymentDueDate);
            _vdInscripcionesFresco1y2RepositoryMock.Verify(r => r.GetInscripcionesFrescoHabilitadas(123), Times.Once);
            _vdInscripcionesFresco3y4RepositoryMock.Verify(r => r.GetInscripcionesFrescoHabilitadas(123), Times.Once);
        }

        [Fact]
        public void ObtenerMisInscripciones_AgrupaOfertasDelMismoProductoYProceso()
        {
            _vdInscripcionesFresco1y2RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco1y2>());

            _vdInscripcionesFresco3y4RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    new()
                    {
                        CodigoPersona = 123,
                        IdProducto = 30,
                        IdProceso = 2,
                        IdOferta = 1,
                        NombreExtensoProducto = "Curso con seminarios",
                        IdNivelProducto = 3,
                        ProgConSeminariosProducto = "SI",
                        FechaReferencia = DateTime.Today,
                        VengoDe = "3y4"
                    },
                    new()
                    {
                        CodigoPersona = 123,
                        IdProducto = 30,
                        IdProceso = 2,
                        IdOferta = 2,
                        NombreExtensoProducto = "Curso con seminarios",
                        IdNivelProducto = 3,
                        ProgConSeminariosProducto = "SI",
                        FechaReferencia = DateTime.Today,
                        VengoDe = "3y4"
                    }
                });

            var result = _service.Enrollments.Execute(123);

            Assert.True(result.Success);
            var grupos = Assert.IsAssignableFrom<IEnumerable<MyEnrollmentsResponse>>(result.Data).ToList();
            var grupo = Assert.Single(grupos);
            Assert.Equal(2, grupo.Enrollments.Count);
            Assert.Equal("SI", grupo.HasSeminars);
            Assert.Equal(new long?[] { 1L, 2L }, grupo.Enrollments.Select(i => i.OfferingId));
        }

        /// <summary>
        /// Nivel 4 con seminarios: se pagaron dos y los otros dos quedaron con el pago pendiente.
        /// Son dos tarjetas, no una confirmada con los cuatro seminarios adentro.
        /// </summary>
        [Fact]
        public void ObtenerMisInscripciones_SeparaTarjetasPorEstado()
        {
            _vdInscripcionesFresco1y2RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco1y2>());

            _vdInscripcionesFresco3y4RepositoryMock
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    SeminarioNivel4(1, EnrollmentStatus.Confirmed, new DateTime(2026, 3, 1)),
                    SeminarioNivel4(2, EnrollmentStatus.Confirmed, new DateTime(2026, 3, 2)),
                    SeminarioNivel4(3, EnrollmentStatus.PaymentPending, new DateTime(2026, 3, 3)),
                    SeminarioNivel4(4, EnrollmentStatus.PaymentPending, new DateTime(2026, 3, 4))
                });

            var result = _service.Enrollments.Execute(123);

            Assert.True(result.Success);
            var grupos = Assert.IsAssignableFrom<IEnumerable<MyEnrollmentsResponse>>(result.Data).ToList();
            Assert.Equal(2, grupos.Count);
            Assert.All(grupos, g => Assert.Equal(30m, g.ProductId));
            Assert.All(grupos, g => Assert.Equal(2m, g.AdmissionProcessId));

            Assert.Equal(EnrollmentStatus.Confirmed, grupos[0].EnrollmentStatus);
            Assert.Equal(new long?[] { 1L, 2L }, grupos[0].Enrollments.Select(i => i.OfferingId));

            Assert.Equal(EnrollmentStatus.PaymentPending, grupos[1].EnrollmentStatus);
            Assert.Equal(new long?[] { 3L, 4L }, grupos[1].Enrollments.Select(i => i.OfferingId));
        }

        private static VdInscripcionesFresco3y4 SeminarioNivel4(long idOferta, string estado, DateTime fechaInicioComienzo) =>
            new()
            {
                CodigoPersona = 123,
                IdProducto = 30,
                IdProceso = 2,
                IdOferta = idOferta,
                NombreExtensoProducto = "Curso nivel 4 con seminarios",
                IdNivelProducto = 4,
                ProgConSeminariosProducto = "SI",
                EstadoInscripcion = estado,
                FechaInicioComienzo = fechaInicioComienzo,
                FechaReferencia = DateTime.Today,
                VengoDe = "3y4"
            };

        [Fact]
        public void ObtenerDatosPersona_NotFound_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns((Persona)null!);

            var result = _service.Details.Execute(123);

            Assert.False(result.Success);
            Assert.Equal("PER_DAT_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDatosPersona_Found_MapsOnlyRequiredData()
        {
            var fechaNacimiento = new DateTime(2000, 1, 2);
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona
                {
                    CodigoPersona = 123,
                    TipoDocumento = "CI",
                    Documento = "12345678",
                    PrimerNombre = "Ana",
                    SegundoNombre = "Maria",
                    PrimerApellido = "Perez",
                    SegundoApellido = "Gomez",
                    FechaNacimiento = fechaNacimiento,
                    Sexo = "F",
                    CodigoPais = 1,
                    CodigoEstado = 2,
                    CodigoCiudad = 3,
                    Direccion = "18 de julio 1234",
                    Telefono1 = "24001234",
                    Email = "ana@test.com"
                });

            var result = _service.Details.Execute(123);

            Assert.True(result.Success);
            Assert.IsType<PersonDetailsResponse>(result.Data);
            Assert.Equal("CI", result.Data.DocumentType);
            Assert.Equal("12345678", result.Data.DocumentNumber);
            Assert.Equal("Ana", result.Data.FirstName);
            Assert.Equal("Maria", result.Data.MiddleName);
            Assert.Equal("Perez", result.Data.FirstSurname);
            Assert.Equal("Gomez", result.Data.SecondSurname);
            Assert.Equal(fechaNacimiento, result.Data.BirthDate);
            Assert.Equal("F", result.Data.Sex);
            Assert.Equal(1, result.Data.CountryId);
            Assert.Equal(2, result.Data.StateId);
            Assert.Equal(3, result.Data.CityId);
            Assert.Equal("18 de julio 1234", result.Data.Address);
            Assert.Equal("ana@test.com", result.Data.Email);
            Assert.Equal("ana@test.com", result.Data.EmailConfirmation);
            Assert.False(result.Data.HasRestrictedIdentity);
        }

        [Fact]
        public void ObtenerDatosPersona_TelefonoEnE164_DevuelveElObjetoDesglosado()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona { CodigoPersona = 123, Telefono1 = "+59899333222" });

            var phone = _service.Details.Execute(123).Data!.PrimaryPhone;

            Assert.True(phone.IsValid);
            Assert.Equal("+59899333222", phone.E164);
            Assert.Equal("UY", phone.Iso2);
            Assert.Equal(598, phone.CountryCode);
            Assert.Equal("99333222", phone.NationalNumber);
        }

        /// <summary>
        /// Un teléfono legacy en formato local (acá además un fijo) no se puede desarmar sin país:
        /// vuelve crudo con IsValid en false para que el front pida el país antes del PUT.
        /// </summary>
        [Theory]
        [InlineData("24001234")]
        [InlineData("099333222")]
        public void ObtenerDatosPersona_TelefonoLegacy_VuelveCrudoYNoValido(string almacenado)
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona { CodigoPersona = 123, Telefono1 = almacenado });

            var phone = _service.Details.Execute(123).Data!.PrimaryPhone;

            Assert.False(phone.IsValid);
            Assert.Equal(almacenado, phone.NationalNumber);
            Assert.Null(phone.Iso2);
            Assert.Null(phone.E164);
        }

        [Theory]
        [InlineData("SI", "NO", "NO", false)]
        [InlineData("NO", "SI", "NO", false)]
        [InlineData("NO", "NO", "SI", false)]
        [InlineData("NO", "NO", "NO", true)]
        public void ObtenerDatosPersona_IdentidadRestringida_MapsLegacyRule(
            string activeStaffMember,
            string usoExclusivoDba,
            string alumnoExtranjero,
            bool tieneInscripcionActiva)
        {
            var person = new Persona
                {
                    CodigoPersona = 123,
                    FuncionarioActivoPersona = activeStaffMember,
                    UsoexclusivodbaPersona = usoExclusivoDba,
                    AlumnoExtranjeroPersona = alumnoExtranjero
                };
            _inscriptoRepositoryMock
                .Setup(r => r.TieneInscripcionActiva(123))
                .Returns(tieneInscripcionActiva);

            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(person);

            var result = _service.Details.Execute(123);

            Assert.True(result.Success);
            Assert.True(result.Data.HasRestrictedIdentity);
        }

        [Fact]
        public void ObtenerDatosPersona_WithoutRestrictiveConditions_ReturnsIdentidadRestringidaFalse()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona
                {
                    CodigoPersona = 123,
                    FuncionarioActivoPersona = "NO",
                    UsoexclusivodbaPersona = "NO",
                    AlumnoExtranjeroPersona = "NO"
                });

            var result = _service.Details.Execute(123);

            Assert.True(result.Success);
            Assert.False(result.Data.HasRestrictedIdentity);
        }

        [Fact]
        public void ActualizarDatosPersona_UpdatesOnlyAllowedFields()
        {
            var person = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "CI",
                Documento = "12345678",
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Gomez",
                FechaNacimiento = new DateTime(2000, 1, 2),
                Sexo = "F",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "Vieja",
                Telefono1 = "111",
                Email = "viejo@test.com"
            };
            var request = new UpdatePersonDetailsRequest
            {
                CountryId = 4,
                StateId = 5,
                CityId = 6,
                Address = "  nueva direccion  ",
                PrimaryPhone = new PhoneNumber { NationalNumber = " 099333222 ", Iso2 = "UY" },
                Email = "nuevo@test.com",
                EmailConfirmation = "nuevo@test.com"
            };

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.Update.Execute(123, request);

            Assert.True(result.Success);
            Assert.Equal("CI", person.TipoDocumento);
            Assert.Equal("12345678", person.Documento);
            Assert.Equal("Ana", person.PrimerNombre);
            Assert.Equal("Maria", person.SegundoNombre);
            Assert.Equal("Perez", person.PrimerApellido);
            Assert.Equal("Gomez", person.SegundoApellido);
            Assert.Equal(new DateTime(2000, 1, 2), person.FechaNacimiento);
            Assert.Equal("F", person.Sexo);
            Assert.Equal(4, person.CodigoPais);
            Assert.Equal(5, person.CodigoEstado);
            Assert.Equal(6, person.CodigoCiudad);
            Assert.Equal("Nueva Direccion", person.Direccion);
            // El teléfono se guarda en E.164, igual que FDP, con su característica de país.
            Assert.Equal("+59899333222", person.Telefono1);
            Assert.Equal(7, person.IdCaracteristicaPaisTel1);
            Assert.Equal("nuevo@test.com", person.Email);
            _personaRepositoryMock.Verify(r => r.Update(person), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_TelefonoNoCelular_ReturnsFailed()
        {
            var person = new Persona { CodigoPersona = 123, Telefono1 = "+59899333222" };
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 4,
                StateId = 5,
                CityId = 6,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "24001234", Iso2 = "UY" },
                Email = "nuevo@test.com",
                EmailConfirmation = "nuevo@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_07", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("+59899333222", person.Telefono1);
            _personaRepositoryMock.Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void ActualizarDatosPersona_IgnoraElE164QueMandaElCliente()
        {
            var person = new Persona { CodigoPersona = 123 };
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 4,
                StateId = 5,
                CityId = 6,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber
                {
                    NationalNumber = "099333222",
                    Iso2 = "UY",
                    // Basura a propósito: el servidor arma el E.164 él mismo, igual que FDP.
                    E164 = "+10000000000",
                    CountryCode = 1,
                    IsValid = true
                },
                Email = "nuevo@test.com",
                EmailConfirmation = "nuevo@test.com"
            });

            Assert.True(result.Success);
            Assert.Equal("+59899333222", person.Telefono1);
        }

        [Fact]
        public void ActualizarDatosPersona_PaisDelTelefonoSinCaracteristica_ReturnsFailed()
        {
            var person = new Persona { CodigoPersona = 123, Telefono1 = "+59899333222" };
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);

            // El mock de T_CARACTERISTICA_PAIS solo tiene Uruguay.
            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 4,
                StateId = 5,
                CityId = 6,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "91122223333", Iso2 = "AR" },
                Email = "nuevo@test.com",
                EmailConfirmation = "nuevo@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_08", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _personaRepositoryMock.Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void ActualizarDatosPersona_IdentidadRestringidaChangingIdentity_ReturnsFailed()
        {
            var person = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "CI",
                Documento = "12345678",
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Gomez",
                FechaNacimiento = new DateTime(2000, 1, 2),
                Sexo = "F"
            };

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(123)).Returns(true);

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                MiddleName = "Laura",
                SecondSurname = "Lopez",
                CountryId = 1,
                StateId = 2,
                CityId = 3,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099333222", Iso2 = "UY" },
                Email = "uno@test.com",
                EmailConfirmation = "uno@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_06", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _ciudadRepositoryMock.Verify(r => r.GetByKey(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            _personaRepositoryMock.Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void ActualizarDatosPersona_IdentidadRestringida_AllowsNonIdentityFields()
        {
            var person = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "CI",
                Documento = "12345678",
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Gomez",
                FechaNacimiento = new DateTime(2000, 1, 2),
                Sexo = "F",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "Vieja",
                Telefono1 = "111",
                Email = "viejo@test.com"
            };

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(123)).Returns(true);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 4,
                StateId = 5,
                CityId = 6,
                Address = "  nueva direccion  ",
                PrimaryPhone = new PhoneNumber { NationalNumber = " 099333222 ", Iso2 = "UY" },
                Email = "nuevo@test.com",
                EmailConfirmation = "nuevo@test.com"
            });

            Assert.True(result.Success);
            Assert.Equal("Ana", person.PrimerNombre);
            Assert.Equal("Maria", person.SegundoNombre);
            Assert.Equal("Perez", person.PrimerApellido);
            Assert.Equal("Gomez", person.SegundoApellido);
            Assert.Equal(new DateTime(2000, 1, 2), person.FechaNacimiento);
            Assert.Equal("F", person.Sexo);
            Assert.Equal("Nueva Direccion", person.Direccion);
            // El teléfono se guarda en E.164, igual que FDP, con su característica de país.
            Assert.Equal("+59899333222", person.Telefono1);
            Assert.Equal(7, person.IdCaracteristicaPaisTel1);
            Assert.Equal("nuevo@test.com", person.Email);
            _personaRepositoryMock.Verify(r => r.Update(person), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_SinIdentidadRestringida_UpdatesIdentityFieldsWhenProvided()
        {
            var person = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "CI",
                Documento = "12345678",
                PrimerNombre = "Ana",
                PrimerNombreMay = "ANA",
                SegundoNombre = "Maria",
                SegundoNombreMay = "MARIA",
                PrimerApellido = "Perez",
                PrimerApellidoMay = "PEREZ",
                SegundoApellido = "Gomez",
                SegundoApellidoMay = "GOMEZ",
                FechaNacimiento = new DateTime(2000, 1, 2),
                Sexo = "F"
            };

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(person);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad());

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                DocumentType = "pa",
                DocumentNumber = " A123 ",
                FirstName = "  laura  ",
                MiddleName = "  ines  ",
                FirstSurname = "  gomez  ",
                SecondSurname = "  rodriguez  ",
                BirthDate = new DateTime(2001, 2, 3),
                Sex = "m",
                CountryId = 1,
                StateId = 2,
                CityId = 3,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099333222", Iso2 = "UY" },
                Email = "uno@test.com",
                EmailConfirmation = "uno@test.com"
            });

            Assert.True(result.Success);
            Assert.Equal("PA", person.TipoDocumento);
            Assert.Equal("A123", person.Documento);
            Assert.Equal("Laura", person.PrimerNombre);
            Assert.Equal("LAURA", person.PrimerNombreMay);
            Assert.Equal("Ines", person.SegundoNombre);
            Assert.Equal("INES", person.SegundoNombreMay);
            Assert.Equal("Gomez", person.PrimerApellido);
            Assert.Equal("GOMEZ", person.PrimerApellidoMay);
            Assert.Equal("Rodriguez", person.SegundoApellido);
            Assert.Equal("RODRIGUEZ", person.SegundoApellidoMay);
            Assert.Equal(new DateTime(2001, 2, 3), person.FechaNacimiento);
            Assert.Equal("M", person.Sexo);
        }

        [Fact]
        public void ActualizarDatosPersona_MailMismatch_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona { CodigoPersona = 123 });

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 1,
                StateId = 2,
                CityId = 3,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099333222", Iso2 = "UY" },
                Email = "uno@test.com",
                EmailConfirmation = "dos@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_04", result.ErrorCode);
            _ciudadRepositoryMock.Verify(r => r.GetByKey(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void ActualizarDatosPersona_InvalidCity_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona { CodigoPersona = 123 });
            _ciudadRepositoryMock
                .Setup(r => r.GetByKey(1, 2, 3))
                .Returns((Ciudad)null!);

            var result = _service.Update.Execute(123, new UpdatePersonDetailsRequest
            {
                CountryId = 1,
                StateId = 2,
                CityId = 3,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099333222", Iso2 = "UY" },
                Email = "uno@test.com",
                EmailConfirmation = "uno@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_05", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_NullRequest_ReturnsFailed()
        {
            var result = await _service.Password.ExecuteAsync(12345, null!);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_InvalidPassword_ReturnsFailedWithoutCallingLdap()
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "short1A!"
            };

            var result = await _service.Password.ExecuteAsync(12345, request);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_02", result.ErrorCode);
            Assert.Equal("La contraseña debe tener entre 12 y 20 caracteres", result.Message);
            Assert.Equal(400, result.HttpCode);
            _ldapMock.Verify(
                x => x.CambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapSuccess_ReturnsOk()
        {
            long personId = 12345;
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    personId.ToString(),
                    request.CurrentPassword,
                    request.NewPassword))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CambiarPasswordAsync)));

            var result = await _service.Password.ExecuteAsync(personId, request);

            Assert.True(result.Success);
            Assert.Equal("Se actualizó tu contraseña", result.Data);
            Assert.Equal(200, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapFailure_PropagatesFailure()
        {
            long personId = 12345;
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    personId.ToString(),
                    request.CurrentPassword,
                    request.NewPassword))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_22",
                    nameof(ILdap.CambiarPasswordAsync),
                    "El servicio LDAP no pudo cambiar la password.",
                    400,
                    false));

            var result = await _service.Password.ExecuteAsync(personId, request);

            Assert.False(result.Success);
            Assert.Equal("AUTH_LDAP_22", result.ErrorCode);
            Assert.Equal("El servicio LDAP no pudo cambiar la password.", result.Message);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            long personId = 12345;
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    personId.ToString(),
                    request.CurrentPassword,
                    request.NewPassword))
                .ThrowsAsync(new InvalidOperationException("LDAP service unavailable"));

            var result = await _service.Password.ExecuteAsync(personId, request);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_99", result.ErrorCode);
            Assert.Contains("Error al cambiar contraseña", result.Message);
            // El detalle interno de la excepción NO debe filtrarse al cliente.
            Assert.DoesNotContain("LDAP service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }

        private static byte[] ValidPdf()
        {
            return new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        }

        private static byte[] ValidPng()
        {
            return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        }
    }
}
