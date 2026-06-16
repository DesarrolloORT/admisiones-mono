using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Catalogos;
using AppLogic.Services.Inscripciones;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class InscripcionesServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IGeneralService>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock
                .Setup(d => d.CurrentDateTime())
                .Returns(FechaBase);
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(global::Utilities.OperationResult<DateTime>.Ok(FechaBase.AddDays(5), nameof(IGeneralService.CalcularFechaVencimientoAdmisiones)));
            _service = new InscripcionesService(_uowFactoryMock.Object, _dbConnectionContextMock.Object, _generalServiceMock.Object);
        }

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

        [Fact]
        public void ObtenerUltimaInscripcionActiva_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_UI_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerUltimaInscripcionActiva_ReturnsMappedDto()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns(new Inscripto
            {
                IdInscripto = 9,
                Oferta = new Oferta
                {
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 5, NombreComienzo = "Abril" },
                        Paquete = new Paquete
                        {
                            Producto = new Producto
                            {
                                IdProducto = 7,
                                NombreProducto = "ATI",
                                NombreExtensoProducto = "Analista en TI"
                            }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(9, result.Data.IdInscripto);
            Assert.Equal(7, result.Data.IdProducto);
            Assert.Equal("ATI", result.Data.NombreProducto);
            Assert.Equal("Analista en TI", result.Data.NombreExtensoProducto);
            Assert.Equal(5, result.Data.IdComienzo);
            Assert.Equal("Abril", result.Data.NombreComienzo);
        }

        [Fact]
        public void ObtenerUltimaInscripcionActiva_ConOfertaNula_UsaValoresPorDefecto()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns(new Inscripto
            {
                IdInscripto = 9,
                Oferta = null
            });
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.IdProducto);
            Assert.Equal(0, result.Data.IdComienzo);
            Assert.Null(result.Data.NombreProducto);
            Assert.Null(result.Data.NombreComienzo);
        }

        [Fact]
        public void RegistrarInteresProducto_CreaInteresNuevoYPersistenciaRelacionada()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            var encuesta = new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 };
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetComienzoActivoPorProcesoOProducto(10, 20)).Returns(30);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var fechaAntes = DateTime.Now;
            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });
            var fechaDespues = DateTime.Now;

            Assert.True(result.Success);
            intereRepo.Verify(r => r.Add(It.Is<Intere>(i =>
                i.CodigoPersona == 123 &&
                i.IdProceso == 20 &&
                i.IdFormaContacto == 7m)), Times.Once);
            interesProductoRepo.Verify(r => r.Add(It.Is<InteresProducto>(i =>
                i.IdInteres == 500m &&
                i.IdProducto == 10 &&
                i.IdGradoInteres == 4m &&
                i.FechaInteresProd.HasValue &&
                i.FechaInteresProd.Value >= fechaAntes &&
                i.FechaInteresProd.Value <= fechaDespues)), Times.Once);
            personaAdmiteRepo.Verify(r => r.Add(It.Is<PersonaAdmite>(p => p.CodigoPersona == 123)), Times.Once);
            interesProductoOfertaRepo.Verify(r => r.Add(It.Is<InteresProductoOferta>(x =>
                x.IdInteres == 500 &&
                x.IdProducto == 10 &&
                x.IdOferta == 30)), Times.Once);
            Assert.Equal(20, encuesta.IdProceso);
            Assert.Equal(30, encuesta.IdComienzo);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public void RegistrarInteresProducto_ConEncuestaSinComienzoActivo_DevuelveErrorYRollback()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetComienzoActivoPorProcesoOProducto(10, 20)).Returns((long?)null);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("GEN_IP_06", result.ErrorCode);
            _uowMock.Verify(u => u.Rollback(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInteresExistente_ReseteaYActualiza()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var interesExistente = new Intere
            {
                IdInteres = 700,
                CodigoPersona = 123,
                IdProceso = 20,
                InteresProductos = new List<InteresProducto>
                {
                    new InteresProducto { IdInteres = 700, IdProducto = 10, IdGradoInteres = 2 },
                    new InteresProducto { IdInteres = 700, IdProducto = 11, IdGradoInteres = 4 },
                    new InteresProducto { IdInteres = 700, IdProducto = 12, IdGradoInteres = 5 }
                }
            };

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere> { interesExistente });
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            var interesProductoProducto10 = new InteresProducto { IdInteres = 700, IdProducto = 10, IdGradoInteres = 2 };
            var interesProductoProducto11 = new InteresProducto { IdInteres = 700, IdProducto = 11, IdGradoInteres = 4 };
            var interesProductoProducto12 = new InteresProducto { IdInteres = 700, IdProducto = 12, IdGradoInteres = 5 };
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 10))
                .Returns(interesProductoProducto10);
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 11))
                .Returns(interesProductoProducto11);
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 12))
                .Returns(interesProductoProducto12);
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns(new PersonaAdmite { CodigoPersona = 123, FechaFrescoPersonaAdmite = DateTime.Today });
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.True(result.Success);
            Assert.Equal(4m, interesProductoProducto10.IdGradoInteres);
            Assert.Equal(0m, interesProductoProducto10.IdGradoInteresAnt);
            Assert.Equal(0m, interesProductoProducto11.IdGradoInteres);
            Assert.Equal(4m, interesProductoProducto11.IdGradoInteresAnt);
            Assert.Equal(5m, interesProductoProducto12.IdGradoInteres);
            interesProductoRepo.Verify(r => r.Update(It.IsAny<InteresProducto>()), Times.AtLeast(2));
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto12), Times.Never);
            intereRepo.Verify(r => r.Add(It.IsAny<Intere>()), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConAccionExistente_NoDuplicaActividadNiAccion()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(true);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.True(result.Success);
            actividadRepo.Verify(r => r.Add(It.IsAny<Actividad>()), Times.Never);
            accionRepo.Verify(r => r.Add(It.IsAny<Accion>()), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInscripcionPrevia_DevuelveConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(true);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_04", result.ErrorCode);
        }

        [Fact]
        public void RegistrarInteresProducto_ConPendiente_DevuelveConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(true);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
        }

        [Fact]
        public void TieneInscripcionActivaParaProceso_ReturnsRepositoryValue()
        {
            var repo = new Mock<IVdEsFrescoAdmisionRepository>();
            repo.Setup(r => r.TieneInscripcionActivaParaProceso(1, 2, 3)).Returns(true);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(repo.Object);

            var result = _service.TieneInscripcionActivaParaProceso(1, 2, 3);

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void TieneInscripcionAdmisiones_ReturnsRepositoryValue()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.TieneInscripcionAdmisiones(1, 2, 3)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.TieneInscripcionAdmisiones(1, 2, 3);

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public void GuardarEncuestaInicial_ParcialMinimo_CreaTemporalSinActualizarPersona()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(900, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal("TEMPORAL", encuestaAgregada.EstadoEncuestaIniAdmision);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Personas.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_MotivosVacio_ReemplazaBorrandoSeleccion()
        {
            SetupPersonaValida();

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "TEMPORAL"
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var motivoRepo = new Mock<IMotivoEleccionAdmisionRepository>();
            _uowMock.Setup(u => u.MotivoEleccionAdmisions).Returns(motivoRepo.Object);

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest
            {
                OpcionesMotivosSeleccionados = []
            });

            Assert.True(result.Success);
            motivoRepo.Verify(r => r.RemoveByPersona(123), Times.Once);
            motivoRepo.Verify(r => r.Add(It.IsAny<MotivoEleccionAdmision>()), Times.Never);
        }

        private void SetupPersonaValida()
        {
            SetupPersona(new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "DE",
                Documento = "123"
            });
        }

        private void SetupPersona(Persona? persona)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupReposDerechoEncuesta(
            bool existeFresco = false,
            bool existeEncuestaIni = false,
            bool existeEncuestaCompleta = false)
        {
            var frescoRepo = new Mock<IVdEsFrescoAdmisionRepository>();
            frescoRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeFresco);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(frescoRepo.Object);

            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeEncuestaIni);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.ExisteCompletaPorDocumento("DE", "123")).Returns(existeEncuestaCompleta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
        }
    }
}
