using AppLogic.DTOs;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Moq;
using Utilities;
using Xunit;
using AppLogic.Services.Personas;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaServiceTests
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
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly PersonaService _service;

        public PersonaServiceTests()
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
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("ADMISIONES");
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(It.IsAny<long>())).Returns(false);

            _service = new PersonaService(_uowFactoryMock.Object, _ldapMock.Object, _dbConnectionContextMock.Object);
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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.True(result.Success);
            Assert.Equal("123_1.pdf", result.Data!.Frente!.NombreArchivo);
            Assert.Equal("123_2.pdf", result.Data.Dorso!.NombreArchivo);
            Assert.Equal(ValidPdf(), result.Data.Frente.Archivo);
            Assert.Equal(ValidPdf(), result.Data.Dorso.Archivo);
            Assert.Equal(fechaFrente, result.Data.FechaVencimiento);
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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data!.Frente);
            Assert.Null(result.Data.Dorso);
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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.True(result.Success);
            Assert.Null(result.Data!.Frente);
            Assert.NotNull(result.Data.Dorso);
            Assert.Equal(fechaDorso, result.Data.FechaVencimiento);
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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.True(result.Success);
            Assert.Equal("123_1.pdf", result.Data!.Frente!.NombreArchivo);
            Assert.Null(result.Data.Dorso);
            Assert.Equal(fechaPersona, result.Data.FechaVencimiento);
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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data!.Frente);
            Assert.Null(result.Data.FechaVencimiento);
        }

        [Fact]
        public void ObtenerDocumentoPersona_WithNoSides_ReturnsNotFound()
        {
            var result = _service.ObtenerDocumentoPersona(123);

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

            var result = _service.ObtenerDocumentoPersona(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_03", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void SubirDocumentoPersona_WithBothSides_CreatesBothAndSavesOnce()
        {
            var persona = new Persona { CodigoPersona = 123 };
            var fecha = DateTime.Today.AddYears(1);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(10)
                .Returns(11);

            var result = _service.SubirDocumentoPersona(
                123,
                fecha,
                new DocumentoPersonaArchivoDto { NombreArchivo = "frente.pdf", Archivo = ValidPdf() },
                new DocumentoPersonaArchivoDto { NombreArchivo = "dorso.pdf", Archivo = ValidPdf() });

            Assert.True(result.Success);
            Assert.Equal(fecha, persona.FechaVtoDocumentoPersona);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i =>
                    i.IdImagenTemporal == 10 &&
                    i.CodigoPersona == 123 &&
                    i.NombreImagen == "123_1.pdf" &&
                    i.TipoImagen == "1")),
                Times.Once);
            _imagenTemporalRepositoryMock.Verify(
                r => r.Add(It.Is<ImagenTemporal>(i =>
                    i.IdImagenTemporal == 11 &&
                    i.CodigoPersona == 123 &&
                    i.NombreImagen == "123_2.pdf" &&
                    i.TipoImagen == "1")),
                Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var result = _service.SubirDocumentoPersona(
                123,
                DateTime.Today.AddYears(1),
                new DocumentoPersonaArchivoDto { NombreArchivo = "frente.pdf", Archivo = ValidPdf() },
                new DocumentoPersonaArchivoDto { NombreArchivo = "dorso.pdf", Archivo = ValidPdf() });

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_WhenDorsoIsMissing_ReturnsBadRequest()
        {
            var result = _service.SubirDocumentoPersona(
                123,
                DateTime.Today.AddYears(1),
                new DocumentoPersonaArchivoDto { NombreArchivo = "frente.pdf", Archivo = ValidPdf() },
                new DocumentoPersonaArchivoDto { NombreArchivo = "dorso.pdf", Archivo = Array.Empty<byte>() });

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
                        NombreExtensoProducto = "Carrera nivel 1",
                        IdNivelProducto = 1,
                        FechaReferencia = DateTime.Today,
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
                        NombreExtensoProducto = "Curso nivel 3",
                        IdNivelProducto = 3,
                        FechaReferencia = DateTime.Today,
                        VengoDe = "3y4"
                    }
                });

            var result = _service.ObtenerMisInscripciones(123);

            Assert.True(result.Success);
            var inscripciones = Assert.IsAssignableFrom<IEnumerable<global::AppLogic.DevartDTOs.DtoVdInscripcionesFresco1y2Devart>>(result.Data).ToList();
            Assert.Equal(2, inscripciones.Count);
            _vdInscripcionesFresco1y2RepositoryMock.Verify(r => r.GetInscripcionesFrescoHabilitadas(123), Times.Once);
            _vdInscripcionesFresco3y4RepositoryMock.Verify(r => r.GetInscripcionesFrescoHabilitadas(123), Times.Once);
        }

        [Fact]
        public void ObtenerDatosPersona_NotFound_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns((Persona)null!);

            var result = _service.ObtenerDatosPersona(123);

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

            var result = _service.ObtenerDatosPersona(123);

            Assert.True(result.Success);
            Assert.IsType<DtoDatosPersona>(result.Data);
            Assert.Equal("CI", result.Data.TipoDocumento);
            Assert.Equal("12345678", result.Data.Documento);
            Assert.Equal("Ana", result.Data.PrimerNombre);
            Assert.Equal("Maria", result.Data.SegundoNombre);
            Assert.Equal("Perez", result.Data.PrimerApellido);
            Assert.Equal("Gomez", result.Data.SegundoApellido);
            Assert.Equal(fechaNacimiento, result.Data.FechaNacimiento);
            Assert.Equal("F", result.Data.Sexo);
            Assert.Equal(1, result.Data.CodigoPais);
            Assert.Equal(2, result.Data.CodigoEstado);
            Assert.Equal(3, result.Data.CodigoCiudad);
            Assert.Equal("18 de julio 1234", result.Data.Direccion);
            Assert.Equal("24001234", result.Data.Telefono1);
            Assert.Equal("ana@test.com", result.Data.Mail);
            Assert.Equal("ana@test.com", result.Data.VerificacionMail);
            Assert.False(result.Data.IdentidadRestringida);
        }

        [Theory]
        [InlineData("SI", "NO", "NO", false)]
        [InlineData("NO", "SI", "NO", false)]
        [InlineData("NO", "NO", "SI", false)]
        [InlineData("NO", "NO", "NO", true)]
        public void ObtenerDatosPersona_IdentidadRestringida_MapsLegacyRule(
            string funcionarioActivo,
            string usoExclusivoDba,
            string alumnoExtranjero,
            bool tieneInscripcionActiva)
        {
            var persona = new Persona
                {
                    CodigoPersona = 123,
                    FuncionarioActivoPersona = funcionarioActivo,
                    UsoexclusivodbaPersona = usoExclusivoDba,
                    AlumnoExtranjeroPersona = alumnoExtranjero
                };
            _inscriptoRepositoryMock
                .Setup(r => r.TieneInscripcionActiva(123))
                .Returns(tieneInscripcionActiva);

            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(persona);

            var result = _service.ObtenerDatosPersona(123);

            Assert.True(result.Success);
            Assert.True(result.Data.IdentidadRestringida);
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

            var result = _service.ObtenerDatosPersona(123);

            Assert.True(result.Success);
            Assert.False(result.Data.IdentidadRestringida);
        }

        [Fact]
        public void ActualizarDatosPersona_UpdatesOnlyAllowedFields()
        {
            var persona = new Persona
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
            var request = new ActualizarDatosPersonaRequest
            {
                CodigoPais = 4,
                CodigoEstado = 5,
                CodigoCiudad = 6,
                Direccion = "  nueva direccion  ",
                Telefono1 = " 222 ",
                Mail = "nuevo@test.com",
                VerificacionMail = "nuevo@test.com"
            };

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.ActualizarDatosPersona(123, request);

            Assert.True(result.Success);
            Assert.Equal("CI", persona.TipoDocumento);
            Assert.Equal("12345678", persona.Documento);
            Assert.Equal("Ana", persona.PrimerNombre);
            Assert.Equal("Maria", persona.SegundoNombre);
            Assert.Equal("Perez", persona.PrimerApellido);
            Assert.Equal("Gomez", persona.SegundoApellido);
            Assert.Equal(new DateTime(2000, 1, 2), persona.FechaNacimiento);
            Assert.Equal("F", persona.Sexo);
            Assert.Equal(4, persona.CodigoPais);
            Assert.Equal(5, persona.CodigoEstado);
            Assert.Equal(6, persona.CodigoCiudad);
            Assert.Equal("Nueva Direccion", persona.Direccion);
            Assert.Equal("222", persona.Telefono1);
            Assert.Equal("nuevo@test.com", persona.Email);
            _personaRepositoryMock.Verify(r => r.Update(persona), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_IdentidadRestringidaChangingIdentity_ReturnsFailed()
        {
            var persona = new Persona
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

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(123)).Returns(true);

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                SegundoNombre = "Laura",
                SegundoApellido = "Lopez",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "uno@test.com"
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
            var persona = new Persona
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

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _inscriptoRepositoryMock.Setup(r => r.TieneInscripcionActiva(123)).Returns(true);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                CodigoPais = 4,
                CodigoEstado = 5,
                CodigoCiudad = 6,
                Direccion = "  nueva direccion  ",
                Telefono1 = " 222 ",
                Mail = "nuevo@test.com",
                VerificacionMail = "nuevo@test.com"
            });

            Assert.True(result.Success);
            Assert.Equal("Ana", persona.PrimerNombre);
            Assert.Equal("Maria", persona.SegundoNombre);
            Assert.Equal("Perez", persona.PrimerApellido);
            Assert.Equal("Gomez", persona.SegundoApellido);
            Assert.Equal(new DateTime(2000, 1, 2), persona.FechaNacimiento);
            Assert.Equal("F", persona.Sexo);
            Assert.Equal("Nueva Direccion", persona.Direccion);
            Assert.Equal("222", persona.Telefono1);
            Assert.Equal("nuevo@test.com", persona.Email);
            _personaRepositoryMock.Verify(r => r.Update(persona), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_SinIdentidadRestringida_UpdatesIdentityFieldsWhenProvided()
        {
            var persona = new Persona
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

            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad());

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                TipoDocumento = "pa",
                Documento = " A123 ",
                PrimerNombre = "  laura  ",
                SegundoNombre = "  ines  ",
                PrimerApellido = "  gomez  ",
                SegundoApellido = "  rodriguez  ",
                FechaNacimiento = new DateTime(2001, 2, 3),
                Sexo = "m",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "uno@test.com"
            });

            Assert.True(result.Success);
            Assert.Equal("PA", persona.TipoDocumento);
            Assert.Equal("A123", persona.Documento);
            Assert.Equal("Laura", persona.PrimerNombre);
            Assert.Equal("LAURA", persona.PrimerNombreMay);
            Assert.Equal("Ines", persona.SegundoNombre);
            Assert.Equal("INES", persona.SegundoNombreMay);
            Assert.Equal("Gomez", persona.PrimerApellido);
            Assert.Equal("GOMEZ", persona.PrimerApellidoMay);
            Assert.Equal("Rodriguez", persona.SegundoApellido);
            Assert.Equal("RODRIGUEZ", persona.SegundoApellidoMay);
            Assert.Equal(new DateTime(2001, 2, 3), persona.FechaNacimiento);
            Assert.Equal("M", persona.Sexo);
        }

        [Fact]
        public void ActualizarDatosPersona_MailMismatch_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetByKey(123))
                .Returns(new Persona { CodigoPersona = 123 });

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "dos@test.com"
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

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "uno@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_05", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_NullRequest_ReturnsFailed()
        {
            var result = await _service.CambiarPasswordAsync(12345, null!);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_InvalidPassword_ReturnsFailedWithoutCallingLdap()
        {
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "short1A!"
            };

            var result = await _service.CambiarPasswordAsync(12345, request);

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
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CambiarPasswordAsync)));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.True(result.Success);
            Assert.Equal("Se actualizó tu contraseña", result.Data);
            Assert.Equal(200, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapFailure_PropagatesFailure()
        {
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_22",
                    nameof(ILdap.CambiarPasswordAsync),
                    "El servicio LDAP no pudo cambiar la password.",
                    400,
                    false));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.False(result.Success);
            Assert.Equal("AUTH_LDAP_22", result.ErrorCode);
            Assert.Equal("El servicio LDAP no pudo cambiar la password.", result.Message);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ThrowsAsync(new InvalidOperationException("LDAP service unavailable"));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_99", result.ErrorCode);
            Assert.Contains("Error al cambiar contraseña", result.Message);
            Assert.Contains("LDAP service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }

        private static byte[] ValidPdf()
        {
            return new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        }
    }
}
