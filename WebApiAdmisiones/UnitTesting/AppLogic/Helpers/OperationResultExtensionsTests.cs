using AppLogic.Helpers;
using Utilities;

namespace UnitTesting.AppLogic.Helpers;

public class OperationResultExtensionsTests
{
    [Fact]
    public void FailureAs_ConResultadoFallido_PreservaCamposYCambiaTipo()
    {
        var original = OperationResult<string>.IsFailed("FDB_SAI_01", "SubirArchivoIngreso", "No se encontró el ingreso.", 404);

        OperationResult<bool> propagado = original.Failure().As<bool>();

        Assert.False(propagado.Success);
        Assert.Equal("FDB_SAI_01", propagado.ErrorCode);
        Assert.Equal("SubirArchivoIngreso", propagado.Method);
        Assert.Equal("No se encontró el ingreso.", propagado.Message);
        Assert.Equal(404, propagado.HttpCode);
    }

    [Fact]
    public void FailureAs_UsaHttpCodePorDefecto400_CuandoElOriginalLoUsa()
    {
        var original = OperationResult<int>.IsFailed("PER_DIR_02", "ValidarDireccion", "Falta estado/provincia.");

        var propagado = original.Failure().As<string>();

        Assert.False(propagado.Success);
        Assert.Equal(400, propagado.HttpCode);
        Assert.Equal("PER_DIR_02", propagado.ErrorCode);
    }
}
