using System;
using AppLogic.Services;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Moq;
using Utilities;
using Xunit;
using AppLogic.IServices;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaAdmisionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly PersonaAdmisionService _service;
        private readonly PersonaService _personaService;

        public PersonaAdmisionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IGeneralService>();
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PersonaAdmisionService(_uowFactoryMock.Object, _dbConnectionContextMock.Object, _generalServiceMock.Object);
            _personaService = new PersonaService(_uowFactoryMock.Object, _ldapMock.Object, _dbConnectionContextMock.Object);
        }

        #region Encuesta

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
        public void ObtenerEncuestaInicialAdmision_ReturnsDto()
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                IdProducto = 20
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.ObtenerEncuestaInicialAdmision(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.IdEncuestaIni);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_PersonaNoEncontrada_ReturnsNotFound()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns((Persona)null);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_CiudadInvalida_ReturnsFailed()
        {
            ConfigurarPersonaBaseParaEncuesta();

            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            ciudadRepo.Setup(r => r.GetByKey(1, 1, 1)).Returns((Ciudad)null);
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_02", result.ErrorCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_ProductoInvalido_ReturnsFailed()
        {
            ConfigurarPersonaBaseParaEncuesta();

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns((Producto)null);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_03", result.ErrorCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_ProductoUniversitarioConCuarto_ReturnsFailed()
        {
            ConfigurarPersonaBaseParaEncuesta();

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var request = CrearRequestEncuesta();
            request.UltimoAnioSexto = 4;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_04", result.ErrorCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_ProcesoNoHabilitado_ReturnsFailed()
        {
            ConfigurarPersonaBaseParaEncuesta();

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns(new List<Proceso>());
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_05", result.ErrorCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_SinComienzo_ReturnsFailed()
        {
            ConfigurarPersonaBaseParaEncuesta();

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<ProcesoComienzo>());
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_06", result.ErrorCode);
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
        public void GuardarDatosPersonaEncuesta_InstitucionInvalida_ReturnsFailed()
        {
            ConfigurarBaseHastaProceso();

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns((Empresa)null);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_08", result.ErrorCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_AnioBachillerInvalido_ReturnsFailed()
        {
            ConfigurarBaseHastaProceso();

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns(new List<AnioBachiller>());
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var request = CrearRequestEncuesta();
            request.UltimoAnioSexto = 5;
            request.CodigoTitulo = 5;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_12", result.ErrorCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_SgiHappyPath_CubreRamasAlternativas()
        {
            var persona = new Persona
            {
                CodigoPersona = 1,
                TipoPersona = "SGI",
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
            ciudadRepo.Setup(r => r.GetByKey(2, 1, 1)).Returns(new Ciudad { CodigoPais = 2, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Exterior" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            tituloRepo.Setup(r => r.GetByKey(5)).Returns(new Titulo { CodigoTitulo = 5 });
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            EncuestaIniAdmision? encuestaAgregada = null;
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            _generalServiceMock.Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 20))
                .Returns(OperationResult<DateTime>.Ok(new DateTime(2026, 6, 30), "CalcularFechaVencimientoAdmisiones"));
            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION)).Returns(99);

            var request = CrearRequestEncuesta();
            request.CodigoPais = 2;
            request.UltimoAnioSecundaria = 2;
            request.UltimoAnioSexto = 6;
            request.CodigoTitulo = 5;
            request.TrabajaActualmente = "S";
            request.TipoJornada = 2;
            request.CompartidoCon = 5;
            request.AsesoramientoOrt = true;
            request.ValoracionAsesoramientoOrt = 5;
            request.VistaSitioWebOrt = true;
            request.ValoracionSitioWeb = 5;
            request.VistaInstalacionesOrt = true;
            request.ValoracionInstalacionesOrt = 5;
            request.PublicidadOrt = true;
            request.OpcionesPublicidadSeleccionadas = [new PublicidadEncuestaRequest { IdPublicidad = 1, NombrePublicidad = "Web" }];
            request.TieneEducacionSuperior = true;
            request.UniversidadesEducacionSuperior =
            [
                new EmpresaEncuestaRequest
                {
                    CodigoEmpresa = 1,
                    Nombre = "Universidad ejemplo"
                }
            ];
            request.InstruccionMadre = 5;
            request.InstruccionMadreOrt = true;
            request.InstruccionPadre = 6;
            request.InstruccionPadreOrt = false;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.True(result.Success);
            Assert.Equal("S", persona.TrabajaActualmente);
            Assert.Equal((byte)2, persona.TipoJornada);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(2898, encuestaAgregada!.CodigoInstitucionBac);
            Assert.Equal("SI", encuestaAgregada.ComparNadieEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.AsesoramientoOrtEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.VistaSitioWebOrtEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.VistaInstalacionesOrtEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.PublicidadOrtEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.InstruccionMadreOrtEncuestaIni);
            Assert.Equal("NO", encuestaAgregada.InstruccionPadreOrtEncuestaIni);
            Assert.Equal("SI", encuestaAgregada.TieneEducacionSuperiorEncuestaIni);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_SgiSinCondicionesLaborales_ReturnsFailed()
        {
            ConfigurarBaseHastaProceso("SGI");

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns([new AnioBachiller { IdAnioBachiller = 1, CantAniosAnioBachiller = 5 }]);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var request = CrearRequestEncuesta();
            request.TrabajaActualmente = null;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_15", result.ErrorCode);
            _generalServiceMock.Verify(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_ExteriorSextoAnio_UsaInstitucionOrtYTituloGenerico()
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
            ciudadRepo.Setup(r => r.GetByKey(2, 1, 1)).Returns(new Ciudad { CodigoPais = 2, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Exterior" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            tituloRepo.Setup(r => r.GetByKey(5)).Returns(new Titulo { CodigoTitulo = 5 });
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            EncuestaIniAdmision? encuestaAgregada = null;
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            _generalServiceMock.Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 20))
                .Returns(OperationResult<DateTime>.Ok(new DateTime(2026, 7, 1), "CalcularFechaVencimientoAdmisiones"));
            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION)).Returns(500);

            var request = CrearRequestEncuesta();
            request.CodigoPais = 2;
            request.CodigoEstado = 1;
            request.CodigoCiudad = 1;
            request.UltimoAnioSecundaria = 2;
            request.UltimoAnioSexto = 6;
            request.CodigoTitulo = 999;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(2898, encuestaAgregada!.CodigoInstitucionBac);
            Assert.Equal(5, encuestaAgregada.CodigoTitulo);
            Assert.Equal("6", encuestaAgregada.UltimoAnioSextoEncuestaIni);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_SextoAnioSinTitulo_ReturnsFailed()
        {
            ConfigurarBaseHastaProceso();

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var request = CrearRequestEncuesta();
            request.UltimoAnioSexto = 6;
            request.CodigoTitulo = null;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_38", result.ErrorCode);
            _generalServiceMock.Verify(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_QuintoAnio_SinTituloPersisteBachilleratoSinOrientacion()
        {
            ConfigurarBaseHastaProceso();
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("DBUSER");

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            EncuestaIniAdmision? encuestaAgregada = null;
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            _generalServiceMock.Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 20))
                .Returns(OperationResult<DateTime>.Ok(new DateTime(2026, 8, 1), "CalcularFechaVencimientoAdmisiones"));
            _dbConnectionContextMock.Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION)).Returns(600);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns([new AnioBachiller { IdAnioBachiller = 1, CantAniosAnioBachiller = 5 }]);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, CrearRequestEncuesta());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Null(encuestaAgregada!.CodigoTitulo);
            Assert.Equal("5", encuestaAgregada.UltimoAnioSextoEncuestaIni);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_SinInstitucionUruguaya_ReturnsFailed()
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

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var request = CrearRequestEncuesta();
            request.CodigoInstitucionBac = null;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_07", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _generalServiceMock.Verify(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_TituloInvalido_ReturnsFailed()
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
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            tituloRepo.Setup(r => r.GetByKey(999)).Returns((Titulo)null);
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var request = CrearRequestEncuesta();
            request.UltimoAnioSexto = 6;
            request.CodigoTitulo = 999;

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_10", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _generalServiceMock.Verify(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_AnioBachillerDelTituloInvalido_ReturnsFailed()
        {
            ConfigurarBaseHastaProceso();
            var request = CrearRequestEncuesta();
            request.UltimoAnioSexto = 6;
            request.CodigoTitulo = 200;

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            tituloRepo.Setup(r => r.GetByKey(200)).Returns(new Titulo { CodigoTitulo = 200, IdAnioBachiller = 999 });
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetByKey(999)).Returns((AnioBachiller)null);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _service.GuardarDatosPersonaEncuesta(1, request);

            Assert.False(result.Success);
            Assert.Equal("PER_DPE_11", result.ErrorCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        #endregion

        #region Archivos

        [Fact]
        public void ObtenerDocumentoPersona_TipoInvalido_ReturnsFailed()
        {
            var result = _personaService.ObtenerDocumentoPersona(1, 9);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _personaService.ObtenerDocumentoPersona(1, 1);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_Vencido_ReturnsFailed()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(new ImagenTemporal
            {
                FechaVtoDocumentoPersona = DateTime.Now.AddDays(-1),
                BlobImagen = new byte[] { 1, 2, 3 }
            });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _personaService.ObtenerDocumentoPersona(1, 1);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_03", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_SinImagen_ReturnsFailed()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(new ImagenTemporal
            {
                FechaVtoDocumentoPersona = DateTime.Now.AddDays(10),
                BlobImagen = Array.Empty<byte>()
            });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _personaService.ObtenerDocumentoPersona(1, 1);

            Assert.False(result.Success);
            Assert.Equal("GEN_DA_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_HappyPath_ReturnsBytes()
        {
            var repo = new Mock<IImagenTemporalRepository>();
            repo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(new ImagenTemporal
            {
                FechaVtoDocumentoPersona = DateTime.Now.AddDays(10),
                BlobImagen = new byte[] { 1, 2, 3 }
            });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(repo.Object);

            var result = _personaService.ObtenerDocumentoPersona(1, 1);

            Assert.True(result.Success);
            Assert.Equal(new byte[] { 1, 2, 3 }, result.Data);
        }

        [Fact]
        public void ObtenerFotoPersona_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IImagenRepository>();
            repo.Setup(r => r.GetFotoByPersona(1)).Returns((Imagen)null);
            _uowMock.Setup(u => u.Imagens).Returns(repo.Object);

            var result = _personaService.ObtenerFotoPersona(1);

            Assert.False(result.Success);
            Assert.Equal("GEN_FA_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerFotoPersona_SinImagen_ReturnsFailed()
        {
            var repo = new Mock<IImagenRepository>();
            repo.Setup(r => r.GetFotoByPersona(1)).Returns(new Imagen { BlobImagen = Array.Empty<byte>() });
            _uowMock.Setup(u => u.Imagens).Returns(repo.Object);

            var result = _personaService.ObtenerFotoPersona(1);

            Assert.False(result.Success);
            Assert.Equal("GEN_FA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerFotoPersona_HappyPath_ReturnsBytes()
        {
            var repo = new Mock<IImagenRepository>();
            repo.Setup(r => r.GetFotoByPersona(1)).Returns(new Imagen { BlobImagen = new byte[] { 5, 6, 7 } });
            _uowMock.Setup(u => u.Imagens).Returns(repo.Object);

            var result = _personaService.ObtenerFotoPersona(1);

            Assert.True(result.Success);
            Assert.Equal(new byte[] { 5, 6, 7 }, result.Data);
        }

        [Fact]
        public void SubirFotoPersona_PersonaNoEncontrada_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns((Persona)null);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _personaService.SubirFotoPersona(1, [0xFF, 0xD8, 0xFF], "foto.jpg");

            Assert.False(result.Success);
            Assert.Equal("GEN_SFA_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirFotoPersona_JpegValido_ActualizaFoto()
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

            var result = _personaService.SubirFotoPersona(1, jpegContent, "foto.jpg");

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
        public void SubirFotoPersona_PngValido_ActualizaFoto()
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

            var result = _personaService.SubirFotoPersona(1, pngContent, "foto.png");

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
        public void SubirFotoPersona_Existente_ModificaFoto()
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

            var result = _personaService.SubirFotoPersona(1, jpegContent, "foto.jpg");

            Assert.True(result.Success);
            Assert.Equal("1_3.jpg", fotoExistente.NombreImagen);
            Assert.Equal("3", fotoExistente.TipoImagen);
            Assert.Equal(jpegContent, fotoExistente.BlobImagen);
            imagenRepo.Verify(r => r.Update(fotoExistente), Times.Once);
            imagenRepo.Verify(r => r.Add(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirFotoPersona_ExtensionInvalida_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo.Setup(r => r.GetFotoByPersona(1)).Returns((Imagen)null);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _personaService.SubirFotoPersona(1, jpegContent, "foto.gif");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirFotoPersona_Existente_ContenidoInvalido_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var fotoExistente = new Imagen
            {
                IdImagen = 1,
                CodigoPersona = 1,
                NombreImagen = "1_3.jpg",
                TipoImagen = "3",
                BlobImagen = new byte[] { 1, 2, 3 }
            };

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo.Setup(r => r.GetFotoByPersona(1)).Returns(fotoExistente);
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);

            var invalidContent = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            var result = _personaService.SubirFotoPersona(1, invalidContent, "foto.jpg");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenRepo.Verify(r => r.Update(It.IsAny<Imagen>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_TipoInvalido_ReturnsFailed()
        {
            var result = _personaService.SubirDocumentoPersona(1, 3, DateTime.Today.AddYears(1), new byte[] { 1, 2, 3 }, "cedula.pdf");

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void SubirDocumentoPersona_PersonaNoEncontrada_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns((Persona)null);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var result = _personaService.SubirDocumentoPersona(1, 1, DateTime.Today.AddYears(1), new byte[] { 1, 2, 3 }, "cedula.pdf");

            Assert.False(result.Success);
            Assert.Equal("GEN_SDA_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void SubirDocumentoPersona_ActualizaFechaDocumentoPersonaYAuditoria()
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

            var result = _personaService.SubirDocumentoPersona(1, 1, fechaVencimiento, pdfContent, "cedula.pdf");

            Assert.True(result.Success);
            Assert.Equal(fechaVencimiento, persona.FechaVtoDocumentoPersona);
            Assert.Equal("1", persona.UsuarioModifFdp);
            Assert.Equal("DBUSER", persona.UsuarioUltimaActualizacion);
            Assert.True(persona.FechaUltimaActualizacion.HasValue);
            imagenTemporalRepo.Verify(r => r.Add(It.Is<ImagenTemporal>(i =>
                i.IdImagenTemporal == 456 &&
                i.CodigoPersona == 1 &&
                i.NombreImagen == "1_1.pdf" &&
                i.TipoImagen == "1" &&
                i.FechaVtoDocumentoPersona == fechaVencimiento &&
                i.BlobImagen == pdfContent)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_Existente_ModificaDocumento()
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

            var result = _personaService.SubirDocumentoPersona(1, 1, fechaVencimiento, pdfContent, "cedula.pdf");

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
        public void SubirDocumentoPersona_ExtensionInvalida_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _personaService.SubirDocumentoPersona(1, 1, DateTime.Today.AddYears(1), jpegContent, "cedula.jpg");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenTemporalRepo.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_Existente_ExtensionInvalida_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var documentoExistente = new ImagenTemporal
            {
                IdImagenTemporal = 456,
                CodigoPersona = 1,
                NombreImagen = "1_1.pdf",
                TipoImagen = "1"
            };

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns(documentoExistente);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _personaService.SubirDocumentoPersona(1, 1, DateTime.Today.AddYears(1), jpegContent, "cedula.jpg");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenTemporalRepo.Verify(r => r.Update(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirDocumentoPersona_ContenidoInvalido_ReturnsFailed()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(1)).Returns(new Persona { CodigoPersona = 1 });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var imagenTemporalRepo = new Mock<IImagenTemporalRepository>();
            imagenTemporalRepo.Setup(r => r.GetDocumentoByPersonaAndTipo(1, 1)).Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepo.Object);

            var invalidPdfContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

            var result = _personaService.SubirDocumentoPersona(1, 1, DateTime.Today.AddYears(1), invalidPdfContent, "cedula.pdf");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            imagenTemporalRepo.Verify(r => r.Add(It.IsAny<ImagenTemporal>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        private void ConfigurarPersonaBaseParaEncuesta(string tipoPersona = "WEB")
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetPersonaWithRelated(1)).Returns(new Persona
            {
                CodigoPersona = 1,
                TipoPersona = tipoPersona,
                FuncionarioActivoPersona = "NO",
                UsoexclusivodbaPersona = "NO",
                Documento = "12345678",
                TipoDocumento = "CI"
            });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var ciudadRepo = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            ciudadRepo.Setup(r => r.GetByKey(1, 1, 1)).Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Montevideo" });
            _uowMock.Setup(u => u.Ciudads).Returns(ciudadRepo.Object);
        }

        private void ConfigurarBaseHastaProceso(string tipoPersona = "WEB")
        {
            ConfigurarPersonaBaseParaEncuesta(tipoPersona);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns([new Proceso { IdProceso = 20 }]);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetAllWithRelated()).Returns([new ProcesoComienzo { IdProceso = 20, IdComienzo = 30 }]);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);
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

        #endregion
    }
}
