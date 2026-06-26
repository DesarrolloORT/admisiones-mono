using AppLogic.Constants;
using AppLogic.DTOs;
using AppLogic.Helpers.ValidationHelpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace UnitTesting.AppLogic.Helpers
{
    /// <summary>
    /// Tests de caracterización que fijan el comportamiento actual de
    /// EncuestaInicialValidationHelper antes de refactorizar su complejidad cognitiva.
    /// No deben cambiar al refactorizar: cualquier diferencia es una regresión.
    /// </summary>
    public class EncuestaInicialValidationHelperTests
    {
        private const string Method = "Test";

        // ====================== ValidarConsistenciaParcial ======================

        private sealed class ParcialCtx
        {
            public Mock<IUnitOfWork> Uow { get; } = new();
            public Mock<IProductoRepository> Productos { get; } = new();
            public Mock<IEmpresaRepository> Empresas { get; } = new();
            public Mock<ITituloRepository> Titulos { get; } = new();
            public Mock<IAnioBachillerRepository> Anios { get; } = new();
            public Mock<BusinessLogic.IDevartRepositories.IMotivoOpcionesAdmisionRepository> MotivoOpc { get; } = new();
            public Mock<BusinessLogic.IDevartRepositories.IPublicidadOpcionesAdmisionRepository> PublOpc { get; } = new();

            public ParcialCtx()
            {
                Productos.Setup(r => r.EsProductoValidoParaInteres(It.IsAny<long>())).Returns(true);
                Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
                Empresas.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Empresa { CodigoEmpresa = 50, Nombre = "Liceo" });
                Empresas.Setup(r => r.GetUniversidades()).Returns(new List<Empresa> { new() { CodigoEmpresa = 50, Nombre = "Universidad" } });
                Titulos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Titulo { CodigoTitulo = 1300 });
                Anios.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>
                {
                    AnioBachillerCatalogo(4),
                    AnioBachillerCatalogo(5),
                    AnioBachillerCatalogo(6, new Titulo { CodigoTitulo = 1300 })
                });
                MotivoOpc.Setup(r => r.GetAll()).Returns(new List<MotivoOpcionesAdmision> { new() { IdMotivo = 1 } });
                PublOpc.Setup(r => r.GetAll()).Returns(new List<PublicidadOpcionesAdmision> { new() { IdPublicidad = 1 } });
                Uow.Setup(u => u.Productos).Returns(Productos.Object);
                Uow.Setup(u => u.Empresas).Returns(Empresas.Object);
                Uow.Setup(u => u.Titulos).Returns(Titulos.Object);
                Uow.Setup(u => u.AnioBachillers).Returns(Anios.Object);
                Uow.Setup(u => u.MotivoOpcionesAdmisions).Returns(MotivoOpc.Object);
                Uow.Setup(u => u.PublicidadOpcionesAdmisions).Returns(PublOpc.Object);
            }
        }

        private static AnioBachiller AnioBachillerCatalogo(long cantAnios, params Titulo[] titulos)
        {
            return new AnioBachiller
            {
                IdAnioBachiller = cantAnios,
                CantAniosAnioBachiller = cantAnios,
                Titulos = titulos.ToList()
            };
        }

        private static GuardarEncuestaInicialRequest RequestValido() => new()
        {
            IdProducto = 10,
            IdProceso = 20,
            CodigoTitulo = 1300,
            UltimoAnioSexto = 6,
            InstruccionPadre = 1,
            InstruccionMadre = 1,
            DecisionCarrera = 2,
            DecisionUniversidad = 2,
            InfoOtrasUniversidadesAntes = "NO",
            CompartidoCon = 1,
            CodigoInstitucionBac = 50,
            InformarEncuesta = "NO",
            UltimoAnioSecundaria = 1,
            NivelDecision = 1
        };

        private static string? ErrorDe(ParcialCtx ctx, GuardarEncuestaInicialRequest request)
        {
            var r = EncuestaInicialValidationHelper.ValidarConsistenciaParcial(ctx.Uow.Object, request, Method);
            return r.Success ? null : r.ErrorCode;
        }

        [Fact]
        public void Parcial_RequestValido_Ok()
        {
            var r = EncuestaInicialValidationHelper.ValidarConsistenciaParcial(new ParcialCtx().Uow.Object, RequestValido(), Method);
            Assert.True(r.Success);
        }

        [Fact]
        public void Parcial_RequestNull_INS_EI_02()
            => Assert.Equal("INS_EI_02", ErrorDe(new ParcialCtx(), null!));

        [Fact]
        public void Parcial_ProductoInvalido_INS_EI_03()
        {
            var ctx = new ParcialCtx();
            ctx.Productos.Setup(r => r.EsProductoValidoParaInteres(It.IsAny<long>())).Returns(false);
            Assert.Equal("INS_EI_03", ErrorDe(ctx, RequestValido()));
        }

        [Fact]
        public void Parcial_ProductoNoEncontrado_INS_EI_03()
        {
            var ctx = new ParcialCtx();
            ctx.Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Producto)null!);
            Assert.Equal("INS_EI_03", ErrorDe(ctx, RequestValido()));
        }

        [Fact]
        public void Parcial_UniversitarioConBachilleratoCuarto_INS_EI_04()
        {
            var ctx = new ParcialCtx();
            ctx.Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            var req = RequestValido();
            req.UltimoAnioSexto = 4;
            Assert.Equal("INS_EI_04", ErrorDe(ctx, req));
        }

        [Fact]
        public void Parcial_ProcesoInvalido_INS_EI_05()
        {
            var req = RequestValido();
            req.IdProceso = 0;
            Assert.Equal("INS_EI_05", ErrorDe(new ParcialCtx(), req));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        public void Parcial_UltimoAnioSextoFueraDeRango_INS_EI_06(long valor)
        {
            var req = RequestValido();
            req.UltimoAnioSexto = valor;
            Assert.Equal("INS_EI_06", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InstruccionMadreFueraDeRango_INS_EI_07()
        {
            var req = RequestValido();
            req.InstruccionMadre = 8;
            Assert.Equal("INS_EI_07", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InstruccionPadreFueraDeRango_INS_EI_08()
        {
            var req = RequestValido();
            req.InstruccionPadre = 0;
            Assert.Equal("INS_EI_08", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_DecisionCarreraInvalida_INS_EI_09()
        {
            var req = RequestValido();
            req.DecisionCarrera = 1;
            Assert.Equal("INS_EI_09", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_DecisionUniversidadInvalida_INS_EI_10()
        {
            var req = RequestValido();
            req.DecisionUniversidad = 1;
            Assert.Equal("INS_EI_10", ErrorDe(new ParcialCtx(), req));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Parcial_CompartidoConFueraDeRango_INS_EI_11(int valor)
        {
            var req = RequestValido();
            req.CompartidoCon = valor;
            Assert.Equal("INS_EI_11", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InfoOtrasInvalido_INS_EI_12()
        {
            var req = RequestValido();
            req.InfoOtrasUniversidadesAntes = "X";
            Assert.Equal("INS_EI_12", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InformarEncuestaInvalido_INS_EI_13()
        {
            var req = RequestValido();
            req.InformarEncuesta = "X";
            Assert.Equal("INS_EI_13", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_UltimoAnioSecundariaInvalido_INS_EI_14()
        {
            var req = RequestValido();
            req.UltimoAnioSecundaria = 3;
            Assert.Equal("INS_EI_14", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_NivelDecisionInvalido_INS_EI_15()
        {
            var req = RequestValido();
            req.NivelDecision = 3;
            Assert.Equal("INS_EI_15", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_ValoracionAsesoramientoInvalida_INS_EI_16()
        {
            var req = RequestValido();
            req.ValoracionAsesoramientoOrt = 6;
            Assert.Equal("INS_EI_16", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_ValoracionSitioWebInvalida_INS_EI_17()
        {
            var req = RequestValido();
            req.ValoracionSitioWeb = 0;
            Assert.Equal("INS_EI_17", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_ValoracionInstalacionesInvalida_INS_EI_18()
        {
            var req = RequestValido();
            req.ValoracionInstalacionesOrt = 6;
            Assert.Equal("INS_EI_18", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InstitucionInvalida_INS_EI_19()
        {
            var req = RequestValido();
            req.CodigoInstitucionBac = 0;
            Assert.Equal("INS_EI_19", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_InstitucionNoEncontrada_INS_EI_20()
        {
            var ctx = new ParcialCtx();
            ctx.Empresas.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Empresa)null!);
            Assert.Equal("INS_EI_20", ErrorDe(ctx, RequestValido()));
        }

        [Fact]
        public void Parcial_TituloInvalido_INS_EI_21()
        {
            var req = RequestValido();
            req.CodigoTitulo = 0;
            Assert.Equal("INS_EI_21", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_TituloNoEncontrado_INS_EI_22()
        {
            var ctx = new ParcialCtx();
            ctx.Anios.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>
            {
                AnioBachillerCatalogo(4),
                AnioBachillerCatalogo(5),
                AnioBachillerCatalogo(6)
            });
            Assert.Equal("INS_EI_22", ErrorDe(ctx, RequestValido()));
        }

        [Fact]
        public void Parcial_MotivoInvalido_INS_EI_23()
        {
            var req = RequestValido();
            req.OpcionesMotivosSeleccionados = [new EncuestaMotivoRequest { IdMotivo = 0 }];
            Assert.Equal("INS_EI_23", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_PublicidadInvalida_INS_EI_24()
        {
            var req = RequestValido();
            req.OpcionesPublicidadSeleccionadas = [new EncuestaPublicidadRequest { IdPublicidad = 0 }];
            Assert.Equal("INS_EI_24", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_UniversidadCodigoNegativo_INS_EI_25()
        {
            var req = RequestValido();
            req.UniversidadesConsideradas = [new EncuestaEmpresaRequest { CodigoEmpresa = -1 }];
            Assert.Equal("INS_EI_25", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_UniversidadCodigoCero_INS_EI_25()
        {
            var req = RequestValido();
            req.UniversidadesConsideradas = [new EncuestaEmpresaRequest { CodigoEmpresa = 0, Nombre = "Otro" }];
            Assert.Equal("INS_EI_25", ErrorDe(new ParcialCtx(), req));
        }

        [Fact]
        public void Parcial_UniversidadNoEncontrada_INS_EI_27()
        {
            var ctx = new ParcialCtx();
            ctx.Empresas.Setup(r => r.GetUniversidades()).Returns(new List<Empresa>());
            var req = RequestValido();
            req.CodigoInstitucionBac = null; // evitar INS_EI_20 antes de llegar a las listas
            req.UniversidadesConsideradas = [new EncuestaEmpresaRequest { CodigoEmpresa = 99 }];
            Assert.Equal("INS_EI_27", ErrorDe(ctx, req));
        }

        // ====================== ResolverCompletitud ======================

        private sealed class CompletitudCtx
        {
            public Mock<IUnitOfWork> Uow { get; } = new();
            public Mock<IProductoRepository> Productos { get; } = new();
            public Mock<IEmpresaRepository> Empresas { get; } = new();
            public Mock<ITituloRepository> Titulos { get; } = new();
            public Mock<IAnioBachillerRepository> Anios { get; } = new();
            public Mock<IEmpresaConsideradaAdmisionRepository> EmpresaConsiderada { get; } = new();
            public Mock<IMotivoEleccionAdmisionRepository> MotivoEleccion { get; } = new();
            public Mock<IPublicidadEleccionAdmisionRepository> PublicidadEleccion { get; } = new();
            public Mock<IEducacionSuperiorAdmisionRepository> EducacionSuperior { get; } = new();

            public CompletitudCtx()
            {
                Productos.Setup(r => r.EsProductoValidoParaInteres(It.IsAny<long>())).Returns(true);
                Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Producto { IdProducto = 10, IdNivelProducto = 2 });
                Empresas.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Empresa { CodigoEmpresa = 50, Nombre = "Liceo" });
                Titulos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Titulo { CodigoTitulo = 1300 });
                EmpresaConsiderada.Setup(r => r.GetByPersona(It.IsAny<long>())).Returns(new List<EmpresaConsideradaAdmision>());
                MotivoEleccion.Setup(r => r.GetByPersona(It.IsAny<long>())).Returns(new List<MotivoEleccionAdmision> { new() { CodigoPersona = 123, IdMotivo = 1 } });
                PublicidadEleccion.Setup(r => r.GetByPersona(It.IsAny<long>())).Returns(new List<PublicidadEleccionAdmision>());
                EducacionSuperior.Setup(r => r.GetByPersona(It.IsAny<long>())).Returns(new List<EducacionSuperiorAdmision>());
                Uow.Setup(u => u.Productos).Returns(Productos.Object);
                Uow.Setup(u => u.Empresas).Returns(Empresas.Object);
                Uow.Setup(u => u.Titulos).Returns(Titulos.Object);
                Uow.Setup(u => u.AnioBachillers).Returns(Anios.Object);
                Uow.Setup(u => u.EmpresaConsideradaAdmisions).Returns(EmpresaConsiderada.Object);
                Uow.Setup(u => u.MotivoEleccionAdmisions).Returns(MotivoEleccion.Object);
                Uow.Setup(u => u.PublicidadEleccionAdmisions).Returns(PublicidadEleccion.Object);
                Uow.Setup(u => u.EducacionSuperiorAdmisions).Returns(EducacionSuperior.Object);
            }
        }

        private static EncuestaIniAdmision EncuestaCompletable() => new()
        {
            CodigoPersona = 123,
            IdProducto = 10,
            IdProceso = 20,
            IdComienzo = 30,
            UltimoAnioSextoEncuestaIni = "6",
            InstruccionPadreEncuestaIni = "1",
            InstruccionMadreEncuestaIni = "1",
            DecisionCarreraEncuestaIni = "2",
            DecisionUniverEncuestaIni = "2",
            InforOtrasAntesEncuestaIni = "NO",
            InformarEncuestaIni = "NO",
            UltimoanioSecundariaEncuestaIni = true,
            NivelDecisionEncuestaIni = true,
            AsesoramientoOrtEncuestaIni = "NO",
            VistaSitioWebOrtEncuestaIni = "NO",
            VistaInstalacionesOrtEncuestaIni = "NO",
            PublicidadOrtEncuestaIni = "NO",
            ComparPadresEncuestaIni = "SI",
            TieneEducacionSuperiorEncuestaIni = "NO",
            CodigoInstitucionBac = 50,
            CodigoTitulo = 1300,
            NombreInstSecEncuestaIni = "Liceo"
        };

        private static Persona PersonaNoSgi() => new() { CodigoPersona = 123, TipoPersona = "AL" };

        private static (bool success, bool? data, string? error) Completar(CompletitudCtx ctx, EncuestaIniAdmision encuesta, Persona persona)
        {
            var r = EncuestaInicialValidationHelper.ResolverCompletitud(ctx.Uow.Object, encuesta, persona, 123, Method);
            return (r.Success, r.Success ? r.Data : null, r.Success ? null : r.ErrorCode);
        }

        [Fact]
        public void Completitud_Completable_OkTrue()
        {
            var r = Completar(new CompletitudCtx(), EncuestaCompletable(), PersonaNoSgi());
            Assert.True(r.success);
            Assert.True(r.data);
        }

        [Fact]
        public void Completitud_FaltanCamposMinimos_OkFalse()
        {
            var e = EncuestaCompletable();
            e.UltimoAnioSextoEncuestaIni = null;
            var r = Completar(new CompletitudCtx(), e, PersonaNoSgi());
            Assert.True(r.success);
            Assert.False(r.data);
        }

        [Fact]
        public void Completitud_ProductoInvalido_INS_EI_28()
        {
            var ctx = new CompletitudCtx();
            ctx.Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Producto)null!);
            var r = Completar(ctx, EncuestaCompletable(), PersonaNoSgi());
            Assert.Equal("INS_EI_28", r.error);
        }

        [Fact]
        public void Completitud_UniversitarioBachilleratoCuarto_INS_EI_29()
        {
            var ctx = new CompletitudCtx();
            ctx.Productos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            var e = EncuestaCompletable();
            e.UltimoAnioSextoEncuestaIni = "4";
            var r = Completar(ctx, e, PersonaNoSgi());
            Assert.Equal("INS_EI_29", r.error);
        }

        [Fact]
        public void Completitud_DatosAcademicosIncompletos_OkFalse()
        {
            var e = EncuestaCompletable();
            e.CodigoInstitucionBac = 0; // UltimoanioSecundaria=true + institucion<=0 -> datos null -> Ok(false)
            var r = Completar(new CompletitudCtx(), e, PersonaNoSgi());
            Assert.True(r.success);
            Assert.False(r.data);
        }

        [Fact]
        public void Completitud_InstitucionNoEncontrada_INS_EI_30()
        {
            var ctx = new CompletitudCtx();
            ctx.Empresas.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Empresa)null!);
            var r = Completar(ctx, EncuestaCompletable(), PersonaNoSgi());
            Assert.Equal("INS_EI_30", r.error);
        }

        [Fact]
        public void Completitud_TituloNoEncontrado_INS_EI_31()
        {
            var ctx = new CompletitudCtx();
            ctx.Titulos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Titulo)null!);
            var r = Completar(ctx, EncuestaCompletable(), PersonaNoSgi());
            Assert.Equal("INS_EI_31", r.error);
        }

        [Fact]
        public void Completitud_SinMotivosEleccion_OkFalse()
        {
            var ctx = new CompletitudCtx();
            ctx.MotivoEleccion.Setup(r => r.GetByPersona(It.IsAny<long>())).Returns(new List<MotivoEleccionAdmision>());
            var r = Completar(ctx, EncuestaCompletable(), PersonaNoSgi());
            Assert.True(r.success);
            Assert.False(r.data);
        }

        [Fact]
        public void Completitud_AsesoramientoSinValoracion_OkFalse()
        {
            var e = EncuestaCompletable();
            e.AsesoramientoOrtEncuestaIni = "SI";
            e.ValoracionAsesoramientoOrtEncuestaIni = null;
            var r = Completar(new CompletitudCtx(), e, PersonaNoSgi());
            Assert.True(r.success);
            Assert.False(r.data);
        }

        [Fact]
        public void Completitud_PersonaSgiSinTrabajaActualmente_NoBloqueaCompletitud()
        {
            var persona = new Persona { CodigoPersona = 123, TipoPersona = PersonaConstants.TipoPersonaSgi, TrabajaActualmente = null };
            var r = Completar(new CompletitudCtx(), EncuestaCompletable(), persona);
            Assert.True(r.success);
            Assert.True(r.data);
        }

        [Fact]
        public void Completitud_AnioBachillerDeTituloNoEncontrado_INS_EI_32()
        {
            var ctx = new CompletitudCtx();
            ctx.Titulos.Setup(r => r.GetByKey(It.IsAny<long>())).Returns(new Titulo { CodigoTitulo = 1300, IdAnioBachiller = 6 });
            // Anios.GetByKey(6) no configurado -> null -> INS_EI_32
            var r = Completar(ctx, EncuestaCompletable(), PersonaNoSgi());
            Assert.Equal("INS_EI_32", r.error);
        }

        [Fact]
        public void Completitud_BachilleratoNoSextoSinAnio_INS_EI_33()
        {
            var ctx = new CompletitudCtx();
            ctx.Anios.Setup(r => r.GetAll()).Returns(new List<AnioBachiller>());
            var e = EncuestaCompletable();
            e.UltimoAnioSextoEncuestaIni = "5"; // != 6 -> rama else -> GetAll().FirstOrDefault -> null -> INS_EI_33
            var r = Completar(ctx, e, PersonaNoSgi());
            Assert.Equal("INS_EI_33", r.error);
        }
    }
}
