using AppLogic.DevartDTOs;
using AppLogic.Scholarships.Dtos;

namespace AppLogic.Scholarships.Mapping;

public static class AffidavitMapper
{
    public static AffidavitDetails ToResponse(
        this DtoDeclaracionJuradaWebDevart affidavit,
        DtoPruebaDevart? prueba) => new()
    {
        SubStatus = affidavit.Subestado,
        ProductId = affidavit.Producto?.IdProducto ?? affidavit.IdProducto,
        ProductFullName = affidavit.Producto?.NombreExtensoProducto,
        CostCenterName = affidavit.Producto?.IdCentroCostos,
        TestEnrollmentId = (long)affidavit.IdInscriptoPrueba,
        TestId = prueba?.IdPrueba ?? 0,
        ScholarshipFundId = affidavit.TipoDescuento?.IdTipoDescuento ?? affidavit.IdTipoDescuento,
        StudyGuideDeadlineDate = prueba?.FechaLimiteGuiaPrueba,
        StudyGuideDeadlineTime = prueba?.HoraLimiteGuiaPrueba,
        ResultsPublicationDate = prueba?.FechaDifusionwebPrueba,
        ScholarshipFundAlias = affidavit.TipoDescuento?.AliasTipoDescuento,
        ProductLevelId = affidavit.Producto?.IdNivelProducto ?? 0,
        Detail = affidavit.TipoDescuento?.DetalleTipoDescuento,
        Name = affidavit.TipoDescuento?.NombreTipoDescuento,
        AffidavitDeadlineDate = prueba?.FechaEntregaDjPrueba,
        AffidavitDeadlineTime = prueba?.HoraEntregaDjPrueba
    };
}
