using AppLogic.People.UseCases;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.UseCases
{
    /// <summary>
    /// Alta y reemplazo de la foto de persona (T_IMAGEN, TipoImagen = foto). El caso de uso
    /// decide entre Add y Update según exista imagen previa, y valida el archivo en ambos caminos.
    /// </summary>
    public class UploadPersonPhotoTests
    {
        // Firma PNG: FileValidator valida por magic bytes, no por extensión.
        private static readonly byte[] PngValido = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPersonaRepository> _personaRepositoryMock = new();
        private readonly Mock<IImagenRepository> _imagenRepositoryMock = new();
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock = new();

        private readonly UploadPersonPhoto _useCase;

        public UploadPersonPhotoTests()
        {
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.Personas).Returns(_personaRepositoryMock.Object);
            _uowMock.Setup(u => u.Imagens).Returns(_imagenRepositoryMock.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("APIADMISIONES");
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN))
                .Returns(777);

            _useCase = new UploadPersonPhoto(_uowFactoryMock.Object, _dbConnectionContextMock.Object);
        }

        private void ConPersona(long personId = 100) =>
            _personaRepositoryMock.Setup(r => r.GetByKey(personId))
                .Returns(new Persona { CodigoPersona = personId });

        [Fact]
        public void Execute_PersonaInexistente_Devuelve404()
        {
            _personaRepositoryMock.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Persona)null!);

            var result = _useCase.Execute(100, PngValido, "foto.png");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void Execute_SinFotoPrevia_CreaLaImagenConElIdDeLaSecuencia()
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns((Imagen)null!);
            Imagen? agregada = null;
            _imagenRepositoryMock.Setup(r => r.Add(It.IsAny<Imagen>()))
                .Callback<Imagen>(i => agregada = i);

            var result = _useCase.Execute(100, PngValido, "foto.png");

            Assert.True(result.Success);
            Assert.NotNull(agregada);
            Assert.Equal(777, agregada.IdImagen);
            Assert.Equal(100, agregada.CodigoPersona);
            Assert.Equal(PngValido, agregada.BlobImagen);
            Assert.EndsWith(".png", agregada.NombreImagen);
            _uowMock.Verify(u => u.Save(), Times.Once);
            _imagenRepositoryMock.Verify(r => r.Update(It.IsAny<Imagen>()), Times.Never);
        }

        [Fact]
        public void Execute_ConFotoPrevia_ActualizaLaImagenExistente()
        {
            ConPersona();
            var existente = new Imagen { IdImagen = 5, CodigoPersona = 100, BlobImagen = [0x00] };
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns(existente);

            var result = _useCase.Execute(100, PngValido, "foto.jpg");

            Assert.True(result.Success);
            Assert.Equal(PngValido, existente.BlobImagen);
            Assert.EndsWith(".jpg", existente.NombreImagen);
            _imagenRepositoryMock.Verify(r => r.Update(existente), Times.Once);
            _imagenRepositoryMock.Verify(r => r.Add(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void Execute_SellaLaAuditoriaDeLaPersona()
        {
            var persona = new Persona { CodigoPersona = 100 };
            _personaRepositoryMock.Setup(r => r.GetByKey(100L)).Returns(persona);
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns((Imagen)null!);

            _useCase.Execute(100, PngValido, "foto.png");

            Assert.Equal("100", persona.UsuarioModifFdp);
            Assert.Equal("APIADMISIONES", persona.UsuarioUltimaActualizacion);
            Assert.NotNull(persona.FechaUltimaActualizacion);
            Assert.NotNull(persona.HoraUltimaActualizacion);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        public void Execute_SinFotoPreviaYArchivoVacio_DevuelveGenSfa03(byte[]? contenido)
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns((Imagen)null!);

            var result = _useCase.Execute(100, contenido!, "foto.png");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_03", result.ErrorCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void Execute_SinFotoPreviaYArchivoNoImagen_DevuelveGenSfa02()
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns((Imagen)null!);

            var result = _useCase.Execute(100, [0x25, 0x50, 0x44, 0x46], "documento.pdf");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_02", result.ErrorCode);
            _imagenRepositoryMock.Verify(r => r.Add(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        public void Execute_ConFotoPreviaYArchivoVacio_DevuelveGenSfa04(byte[]? contenido)
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100))
                .Returns(new Imagen { IdImagen = 5, CodigoPersona = 100 });

            var result = _useCase.Execute(100, contenido!, "foto.png");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_04", result.ErrorCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void Execute_ConFotoPreviaYArchivoNoImagen_DevuelveGenSfa05()
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100))
                .Returns(new Imagen { IdImagen = 5, CodigoPersona = 100 });

            var result = _useCase.Execute(100, [0x25, 0x50, 0x44, 0x46], "documento.pdf");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_05", result.ErrorCode);
            _imagenRepositoryMock.Verify(r => r.Update(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void Execute_FotoPreviaSinCodigoPersona_UsaCeroEnElNombrePersistido()
        {
            ConPersona();
            var existente = new Imagen { IdImagen = 5, CodigoPersona = null };
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns(existente);

            var result = _useCase.Execute(100, PngValido, "foto.png");

            Assert.True(result.Success);
            Assert.StartsWith("0_", existente.NombreImagen);
        }

        [Fact]
        public void Execute_SinExtensionEnElNombre_CaeAJpgPorDefecto()
        {
            ConPersona();
            _imagenRepositoryMock.Setup(r => r.GetFotoByPersona(100)).Returns((Imagen)null!);
            Imagen? agregada = null;
            _imagenRepositoryMock.Setup(r => r.Add(It.IsAny<Imagen>())).Callback<Imagen>(i => agregada = i);

            var result = _useCase.Execute(100, PngValido, "foto");

            Assert.True(result.Success);
            Assert.NotNull(agregada);
            Assert.EndsWith(".jpg", agregada.NombreImagen);
        }
    }
}
