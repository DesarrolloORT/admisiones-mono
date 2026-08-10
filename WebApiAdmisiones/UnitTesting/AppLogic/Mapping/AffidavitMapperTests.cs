using AppLogic.DevartDTOs;
using AppLogic.Scholarships.Mapping;
using Xunit;

namespace UnitTesting.AppLogic.Mapping
{
    /// <summary>
    /// El mapper resuelve cada campo con navegación opcional (Producto/TipoDescuento/prueba),
    /// así que hay dos escenarios: navegaciones cargadas y navegaciones vacías (fallback).
    /// </summary>
    public class AffidavitMapperTests
    {
        private static DtoDeclaracionJuradaWebDevart CrearDeclaracion() => new()
        {
            Subestado = "PENDIENTE",
            IdProducto = 11,
            IdTipoDescuento = 22,
            IdInscriptoPrueba = 33
        };

        [Fact]
        public void ToResponse_ConNavegacionesCargadas_UsaLosDatosDeLasEntidadesRelacionadas()
        {
            var affidavit = CrearDeclaracion();
            affidavit.Producto = new DtoProductoDevart
            {
                IdProducto = 111,
                NombreExtensoProducto = "Licenciatura en Sistemas",
                IdCentroCostos = "CC-01",
                IdNivelProducto = 4
            };
            affidavit.TipoDescuento = new DtoTipoDescuentoDevart
            {
                IdTipoDescuento = 222,
                NombreTipoDescuento = "Fondo de Beca",
                AliasTipoDescuento = "FB",
                DetalleTipoDescuento = "Detalle del fondo"
            };

            var prueba = new DtoPruebaDevart
            {
                IdPrueba = 999,
                FechaLimiteGuiaPrueba = new DateTime(2026, 3, 1),
                HoraLimiteGuiaPrueba = "18:00",
                FechaDifusionwebPrueba = new DateTime(2026, 4, 15),
                FechaEntregaDjPrueba = new DateTime(2026, 2, 20),
                HoraEntregaDjPrueba = "23:59"
            };

            var result = affidavit.ToResponse(prueba);

            Assert.Equal("PENDIENTE", result.SubStatus);
            Assert.Equal(111, result.ProductId);
            Assert.Equal("Licenciatura en Sistemas", result.ProductFullName);
            Assert.Equal("CC-01", result.CostCenterName);
            Assert.Equal(33, result.TestEnrollmentId);
            Assert.Equal(999, result.TestId);
            Assert.Equal(222, result.ScholarshipFundId);
            Assert.Equal(new DateTime(2026, 3, 1), result.StudyGuideDeadlineDate);
            Assert.Equal("18:00", result.StudyGuideDeadlineTime);
            Assert.Equal(new DateTime(2026, 4, 15), result.ResultsPublicationDate);
            Assert.Equal("FB", result.ScholarshipFundAlias);
            Assert.Equal(4, result.ProductLevelId);
            Assert.Equal("Detalle del fondo", result.Detail);
            Assert.Equal("Fondo de Beca", result.Name);
            Assert.Equal(new DateTime(2026, 2, 20), result.AffidavitDeadlineDate);
            Assert.Equal("23:59", result.AffidavitDeadlineTime);
        }

        [Fact]
        public void ToResponse_SinNavegaciones_CaeALosIdsPropiosDeLaDeclaracion()
        {
            var affidavit = CrearDeclaracion();
            affidavit.Producto = null!;
            affidavit.TipoDescuento = null!;

            var result = affidavit.ToResponse(prueba: null);

            Assert.Equal(11, result.ProductId);
            Assert.Equal(22, result.ScholarshipFundId);
            Assert.Equal(0, result.TestId);
            Assert.Equal(0, result.ProductLevelId);
            Assert.Null(result.ProductFullName);
            Assert.Null(result.CostCenterName);
            Assert.Null(result.ScholarshipFundAlias);
            Assert.Null(result.Detail);
            Assert.Null(result.Name);
            Assert.Null(result.StudyGuideDeadlineDate);
            Assert.Null(result.StudyGuideDeadlineTime);
            Assert.Null(result.ResultsPublicationDate);
            Assert.Null(result.AffidavitDeadlineDate);
            Assert.Null(result.AffidavitDeadlineTime);
        }

        [Fact]
        public void ToResponse_ConProductoPeroSinPrueba_MezclaAmbasFuentes()
        {
            var affidavit = CrearDeclaracion();
            affidavit.Producto = new DtoProductoDevart
            {
                IdProducto = 111,
                NombreExtensoProducto = "Analista",
                IdNivelProducto = 2
            };
            affidavit.TipoDescuento = null!;

            var result = affidavit.ToResponse(prueba: null);

            Assert.Equal(111, result.ProductId);
            Assert.Equal("Analista", result.ProductFullName);
            Assert.Equal(2, result.ProductLevelId);
            Assert.Equal(22, result.ScholarshipFundId);
            Assert.Equal(0, result.TestId);
        }
    }
}
