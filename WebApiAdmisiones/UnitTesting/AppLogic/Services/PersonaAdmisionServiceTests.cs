using System;
using AppLogic.Interfaces;
using AppLogic.Services;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaAdmisionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly IConfiguration _configuration;
        private readonly PersonaAdmisionService _service;

        public PersonaAdmisionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IGeneralService>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Admisiones:PersonaAdmision:FechaMinimaNacimiento"] = "1900-01-01",
                    ["Admisiones:PersonaAdmision:UruguayCodigoPais"] = "1",
                    ["Admisiones:PersonaAdmision:ExteriorInstitucionOrt"] = "2898",
                    ["Admisiones:PersonaAdmision:TituloGenericoSextoExterior"] = "5"
                })
                .Build();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PersonaAdmisionService(_uowFactoryMock.Object, _dbConnectionContextMock.Object, _generalServiceMock.Object, _configuration);
        }

        [Fact]
        public void ObtenerPersona_NotFound_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(123)).Returns((Persona)null);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _service.ObtenerPersona(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_PER_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ActualizarPersona_MailNoCoincide_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _service.ActualizarPersona(1, new ActualizarPersonaRequest
            {
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                Mail = "ana@test.com",
                VerificacionMail = "otro@test.com",
                Direccion = "18 de julio 1234",
                Sexo = "F",
                FechaNacimiento = new DateTime(2000, 1, 1),
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1
            });

            Assert.False(result.Success);
            Assert.Equal("PER_AP_05", result.ErrorCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void ActualizarPersona_HappyPath_ActualizaPersona()
        {
            var persona = new Persona
            {
                CodigoPersona = 1,
                TipoPersona = "WEB",
                PrimerNombre = "nombre viejo",
                PrimerApellido = "apellido viejo",
                PrimerNombreMay = "NOMBRE VIEJO",
                PrimerApellidoMay = "APELLIDO VIEJO",
                FuncionarioActivoPersona = "NO",
                UsoexclusivodbaPersona = "NO"
            };

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            ciudadRepo.Setup(r => r.GetByKey(1, 2, 3)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var result = _service.ActualizarPersona(1, new ActualizarPersonaRequest
            {
                PrimerApellido = "perez",
                SegundoApellido = "lopez",
                PrimerNombre = "ana",
                SegundoNombre = "maria",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com",
                Direccion = "18 de julio 1234",
                Sexo = "F",
                Telefono1 = "24001234",
                FechaNacimiento = new DateTime(2000, 1, 1),
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3
            });

            Assert.True(result.Success);
            Assert.Equal("Ana", persona.PrimerNombre);
            Assert.Equal("Perez", persona.PrimerApellido);
            Assert.Equal("ANA", persona.PrimerNombreMay);
            Assert.Equal("PEREZ", persona.PrimerApellidoMay);
            Assert.Equal(1, persona.CodigoPais);
            Assert.Equal(2, persona.CodigoEstado);
            Assert.Equal(3, persona.CodigoCiudad);
            Assert.Equal("24001234", persona.Telefono1);
            Assert.Equal("ana@test.com", persona.Email);
            Assert.Equal("1", persona.UsuarioModifFdp);
            personaRepo.Verify(r => r.Update(persona), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ObtenerEncuestaInicialAdmision_NotFound_ReturnsFailed()
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.ObtenerEncuestaInicialAdmision(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_DPI_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_Duplicada_ReturnsConflict()
        {
            var persona = new Persona
            {
                CodigoPersona = 1,
                TipoPersona = "WEB",
                FuncionarioActivoPersona = "NO",
                UsoexclusivodbaPersona = "NO",
                Documento = "12345678",
                TipoDocumento = "CI"
            };

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            ciudadRepo.Setup(r => r.GetByKey(1, 1, 1)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa
            {
                CodigoEmpresa = 100,
                Nombre = "Instituto Ejemplo",
                UsuarioUltimaActualizacion = "USR",
                FechaUltimaActualizacion = DateTime.Today,
                HoraUltimaActualizacion = "10:00:00",
                UsuarioIngreso = "USR",
                FechaIngreso = DateTime.Today,
                HoraIngreso = "10:00:00",
                CodigoTipoEmpresa = 9
            });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20, HabilitadoInteresSitio = "SI", ComienzoSemestre1Proceso = DateTime.Today.AddDays(10) }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersonaProductoComienzo(1, 10, 30)).Returns(new EncuestaIniAdmision { IdEncuestaIni = 99 });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns([new AnioBachiller
            {
                IdAnioBachiller = 1,
                CantAniosAnioBachiller = 5,
                UsuarioIngreso = "USR",
                FechaIngreso = DateTime.Today,
                HoraIngreso = "10:00:00"
            }]);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_13", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_HappyPath_GuardaEncuestaYActualizaPersona()
        {
            var persona = new Persona
            {
                CodigoPersona = 1,
                TipoPersona = "WEB",
                PrimerNombre = "old",
                PrimerApellido = "old",
                PrimerNombreMay = "OLD",
                PrimerApellidoMay = "OLD",
                FuncionarioActivoPersona = "NO",
                UsoexclusivodbaPersona = "NO",
                Documento = "12345678",
                TipoDocumento = "CI"
            };

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            ciudadRepo.Setup(r => r.GetByKey(1, 1, 1)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var proceso = new Proceso { IdProceso = 20, HabilitadoInteresSitio = "SI", ComienzoSemestre1Proceso = DateTime.Today.AddDays(10) };
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([proceso]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersonaProductoComienzo(1, 10, 30)).Returns((EncuestaIniAdmision)null);
            EncuestaIniAdmision? encuestaAgregada = null;
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(200);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa
            {
                CodigoEmpresa = 100,
                Nombre = "Instituto Ejemplo",
                UsuarioUltimaActualizacion = "USR",
                FechaUltimaActualizacion = DateTime.Today,
                HoraUltimaActualizacion = "10:00:00",
                UsuarioIngreso = "USR",
                FechaIngreso = DateTime.Today,
                HoraIngreso = "10:00:00",
                CodigoTipoEmpresa = 9
            });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var declaracionRepo = new Mock<IDeclaracionJuradaWebRepository>();
            declaracionRepo.Setup(r => r.GetFechaEntregaDjAdmisiones(1)).Returns((DateTime?)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(declaracionRepo.Object);

            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 20))
                .Returns(OperationResult<DateTime>.Ok(new DateTime(2026, 3, 30), "CalcularFechaVencimientoAdmisiones"));

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns([new AnioBachiller
            {
                IdAnioBachiller = 1,
                CantAniosAnioBachiller = 5,
                UsuarioIngreso = "USR",
                FechaIngreso = DateTime.Today,
                HoraIngreso = "10:00:00"
            }]);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.True(result.Success);
            Assert.Equal("Ana", persona.PrimerNombre);
            Assert.Equal("Perez", persona.PrimerApellido);
            personaRepo.Verify(r => r.Update(persona), Times.Once);
            encuestaRepo.Verify(r => r.Add(It.IsAny<EncuestaIniAdmision>()), Times.Once);
            _dbConnectionContextMock.Verify(
                d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                Times.Once);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(200, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal(1, encuestaAgregada.CodigoPersona);
            Assert.Equal(10, encuestaAgregada.IdProducto);
            Assert.Equal(30, encuestaAgregada.IdComienzo);
            Assert.Equal(20, encuestaAgregada.IdProceso);
            Assert.Equal(100, encuestaAgregada.CodigoInstitucionBac);
            Assert.Equal(new DateTime(2026, 3, 30), encuestaAgregada.FechaVtoAdmision);
            Assert.Equal("SOLO_ENCUESTA_INI", encuestaAgregada.TipoInscripcion);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ObtenerDocumentoAlumno_TipoInvalido_ReturnsFailed()
        {
            var result = _service.ObtenerDocumentoAlumno(1, 9);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoAlumno_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _service.ObtenerDocumentoAlumno(1, 1);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoAlumno_Vencido_ReturnsFailed()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(new ImagenTemporal
            {
                FechaVtoDocumentoPersona = DateTime.Now.AddDays(-1),
                BlobImagen = new byte[] { 1, 2, 3 }
            });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _service.ObtenerDocumentoAlumno(1, 1);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_03", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void ObtenerFotoAlumno_SinImagen_ReturnsFailed()
        {
            var repo = new Mock<IImagenRepository>();
            repo.Setup(r => r.GetFotoByPersona(1)).Returns(new Imagen { BlobImagen = Array.Empty<byte>() });
            _uowMock.Setup(u => u.Imagens).Returns(repo.Object);

            var result = _service.ObtenerFotoAlumno(1);

            Assert.False(result.Success);
            Assert.Equal("GEN_FA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void SubirFotoAlumno_JpegValido_ActualizaFoto()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var persona = new Persona { CodigoPersona = 1 };
            personaRepo.Setup(r => r.GetByKey(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo.Setup(r => r.GetFotoByPersona(1)).Returns((Imagen)null);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);

            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN)).Returns(123);
            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirFotoAlumno(1, jpegContent, "foto.jpg");

            Assert.True(result.Success);
            Assert.Equal("1", persona.UsuarioModifFdp);
            Assert.Equal("DBUSER", persona.UsuarioUltimaActualizacion);
            Assert.True(persona.FechaUltimaActualizacion.HasValue);
            imagenRepo.Verify(r => r.Add(It.Is<Imagen>(i =>
                i.IdImagen == 123 &&
                i.CodigoPersona == 1 &&
                i.NombreImagen == "1_3.jpg" &&
                i.TipoImagen == "3" &&
                i.BlobImagen == jpegContent)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirFotoAlumno_PngValido_ActualizaFoto()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var persona = new Persona { CodigoPersona = 1 };
            personaRepo.Setup(r => r.GetByKey(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo.Setup(r => r.GetFotoByPersona(1)).Returns((Imagen)null);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);

            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN)).Returns(123);
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            var result = _service.SubirFotoAlumno(1, pngContent, "foto.png");

            Assert.True(result.Success);
            Assert.Equal("1", persona.UsuarioModifFdp);
            Assert.Equal("DBUSER", persona.UsuarioUltimaActualizacion);
            imagenRepo.Verify(r => r.Add(It.Is<Imagen>(i =>
                i.IdImagen == 123 &&
                i.CodigoPersona == 1 &&
                i.NombreImagen == "1_3.png" &&
                i.TipoImagen == "3" &&
                i.BlobImagen == pngContent)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirFotoAlumno_Existente_ModificaFoto()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            var persona = new Persona { CodigoPersona = 1 };
            personaRepo.Setup(r => r.GetByKey(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var fotoExistente = new Imagen
            {
                IdImagen = 123,
                CodigoPersona = 1,
                NombreImagen = "1_3.jpg",
                TipoImagen = "3",
                BlobImagen = new byte[] { 1, 2, 3 }
            };

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo.Setup(r => r.GetFotoByPersona(1)).Returns(fotoExistente);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirFotoAlumno(1, jpegContent, "foto.jpg");

            Assert.True(result.Success);
            Assert.Equal("1_3.jpg", fotoExistente.NombreImagen);
            Assert.Equal("3", fotoExistente.TipoImagen);
            Assert.Equal(jpegContent, fotoExistente.BlobImagen);
            imagenRepo.Verify(r => r.Update(fotoExistente), Times.Once);
            imagenRepo.Verify(r => r.Add(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoAlumno_ActualizaFechaDocumentoPersonaYAuditoria()
        {
            var fechaVencimiento = new DateTime(2030, 12, 31);
            var persona = new Persona { CodigoPersona = 1 };

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL)).Returns(456);
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirDocumentoAlumno(1, 1, fechaVencimiento, pdfContent, "cedula.pdf");

            Assert.True(result.Success);
            Assert.Equal(fechaVencimiento, persona.FechaVtoDocumentoPersona);
            Assert.Equal("1", persona.UsuarioModifFdp);
            Assert.Equal("DBUSER", persona.UsuarioUltimaActualizacion);
            Assert.True(persona.FechaUltimaActualizacion.HasValue);
            imagenTemporalRepo.Verify(r => r.Add(It.Is<ImagenTemporal>(i =>
                i.IdImagenTemporal == 456 &&
                i.CodigoPersona == 1 &&
                i.TipoImagen == "1" &&
                i.FechaVtoDocumentoPersona == fechaVencimiento &&
                i.BlobImagen == pdfContent)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoAlumno_Existente_ModificaDocumento()
        {
            var fechaVencimiento = new DateTime(2031, 1, 15);
            var persona = new Persona { CodigoPersona = 1 };

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var documentoExistente = new ImagenTemporal
            {
                IdImagenTemporal = 456,
                CodigoPersona = 1,
                NombreImagen = "1_1.pdf",
                TipoImagen = "1",
                BlobImagen = new byte[] { 1, 2, 3 },
                FechaVtoDocumentoPersona = new DateTime(2030, 1, 1)
            };

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(documentoExistente);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirDocumentoAlumno(1, 1, fechaVencimiento, pdfContent, "cedula.pdf");

            Assert.True(result.Success);
            Assert.Equal("1_1.pdf", documentoExistente.NombreImagen);
            Assert.Equal("1", documentoExistente.TipoImagen);
            Assert.Equal(pdfContent, documentoExistente.BlobImagen);
            Assert.Equal(fechaVencimiento, documentoExistente.FechaVtoDocumentoPersona);
            Assert.Equal(fechaVencimiento, persona.FechaVtoDocumentoPersona);
            imagenTemporalRepo.Verify(r => r.Update(documentoExistente), Times.Once);
            imagenTemporalRepo.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoAlumno_ExtensionInvalida_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirDocumentoAlumno(1, 1, DateTime.Today.AddYears(1), jpegContent, "cedula.jpg");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenTemporalRepo.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoAlumno_ContenidoInvalido_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var invalidPdfContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

            var result = _service.SubirDocumentoAlumno(1, 1, DateTime.Today.AddYears(1), invalidPdfContent, "cedula.pdf");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenTemporalRepo.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        private static GuardarDatosPersonaEncuestaRequest CrearRequestEncuesta()
        {
            return new GuardarDatosPersonaEncuestaRequest
            {
                PrimerApellido = "perez",
                SegundoApellido = "lopez",
                PrimerNombre = "ana",
                SegundoNombre = "maria",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com",
                Direccion = "18 de julio 1234",
                Sexo = "F",
                FechaNacimiento = new DateTime(2000, 1, 1),
                Telefono1 = "24001234",
                Telefono2 = "",
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1,
                Documento = "12345678",
                TipoDocumento = "CI",
                IdProducto = 10,
                IdProceso = 20,
                CodigoTitulo = null,
                UltimoAnioSexto = 5,
                VecesSexto = 0,
                VecesSextoBool = false,
                InstruccionPadre = 3,
                InstruccionMadre = 3,
                DecisionCarrera = 2,
                DecisionUniversidad = 3,
                InfoOtrasUniversidadesAntes = "NO",
                InfoOtrasLinea1 = "",
                InfoOtrasLinea2 = "",
                CompartidoCon = 1,
                CodigoInstitucionBac = 100,
                InformarEncuesta = "SI",
                NombreInstitucion = "",
                UltimoAnioSecundaria = 1,
                TieneEducacionSuperior = false,
                NivelDecision = 1,
                AsesoramientoOrt = true,
                ValoracionAsesoramientoOrt = 5,
                VistaSitioWebOrt = true,
                ValoracionSitioWeb = 4,
                VistaInstalacionesOrt = true,
                ValoracionInstalacionesOrt = 4,
                PublicidadOrt = true,
                InstruccionMadreOrt = null,
                InstruccionPadreOrt = null,
                UniversidadesConsideradas = [],
                UniversidadesEducacionSuperior = [],
                OpcionesPublicidadSeleccionadas = [new PublicidadEncuestaRequest { IdPublicidad = 1, NombrePublicidad = "Web" }],
                OpcionesMotivosSeleccionados = [new MotivoEncuestaRequest { IdMotivo = 1, NombreMotivo = "Prestigio" }]
            };
        }
    }
}
