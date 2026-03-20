using System;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaAdmisionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly PersonaAdmisionService _service;

        public PersonaAdmisionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PersonaAdmisionService(_uowFactoryMock.Object, _dbConnectionContextMock.Object);
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
    }
}
