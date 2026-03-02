// File: UnitTesting/Utilities/ConstantesTests.cs
using System;
using Utilities;
using Xunit;

namespace UnitTesting.Utilities
{
    public class ConstantesTests
    {
        [Fact]
        public void IntConstants_HaveExpectedValues()
        {
            Assert.Equal(-100000, Constantes.kNULL);
            Assert.Equal(1, Constantes.kcantidadCuotasVPP);
            Assert.Equal(5, Constantes.kMODO_EDICION);
            Assert.Equal(999, Constantes.kAlumnoUniversidadORT);
            Assert.Equal(10, Constantes.kEstado_Legajo_ENTREGADO);
            Assert.Equal(1, Constantes.kID_NIVEL_PRODUCTO_UNIVERSITARIO);
            Assert.Equal(2596, Constantes.kCODIGO_EMPRESA_ORT);
            Assert.Equal(1, Constantes.kCODIGO_URUGUAY);
            Assert.Equal(70, Constantes.gkMINIMO_APROBACION);
            Assert.Equal(4, Constantes.kID_TIPO_DESCUENTO_FUNC);
            Assert.Equal(1, Constantes.kID_FACTURA);
            Assert.Equal(1, Constantes.kUBICO_INSCRIPTO);
            Assert.Equal(1, Constantes.kFicha);
            Assert.Equal(23, Constantes.KCAMBIO_S);
            Assert.Equal(1, Constantes.KEFECTIVO_S);
            Assert.Equal(0, Constantes.kTRANSACCION_INSCRIPCION);
            Assert.Equal(1, Constantes.KTIPO_RECIBO_DE_PAGO);
            Assert.Equal(1, Constantes.KID_LUGAR_CENTRO);
            Assert.Equal(14, Constantes.kCANT_MAX_LINEAS_FACTURA + 5); // 9 + 5 = 14, just to show arithmetic
            Assert.Equal(5, Constantes.kMODO_QUITAR);
            Assert.Equal(1, Constantes.kIMPRESION_DETALLADA);
            Assert.Equal(1, Constantes.kMENU_REVISOR);
            Assert.Equal(1, Constantes.kId_RegimenPlanPagoTradicional);
            Assert.Equal(1, Constantes.kFORM3102_SI);
        }

        [Fact]
        public void StringConstants_HaveExpectedValues()
        {
            Assert.Equal("NO", Constantes.kstrNO);
            Assert.Equal("SI", Constantes.kstrSI);
            Assert.Equal("Consulta", Constantes.KABITAB_CONSULTA);
            Assert.Equal("OK", Constantes.KABITAB_OK);
            Assert.Equal("URUGUAY", Constantes.kNOMBRE_URUGUAY);
            Assert.Equal("INTERNET", Constantes.kUSUARIO_INTERNET);
            Assert.Equal("P", Constantes.kSTR_PARCIAL);
            Assert.Equal("$", Constantes.kSTRIDMONEDA_S);
            Assert.Equal("Facturas", Constantes.kIMPRESORA_FACTURA);
            Assert.Equal("SinIngresoEnLaWEB", Constantes.kTIPO_NOCOMPLETADO);
            Assert.Equal("ParaEnviar", Constantes.kESTADODGI_PARA_ENVIAR);
            Assert.Equal("A", Constantes.kEstadoA);
            Assert.Equal("bajas_admisiones@ort.edu.uy", Constantes.kMailCoordinadorSGI);
            Assert.Equal("Depósito en ABITAB", Constantes.kOBSERVACIONES_DEPOSITO_ABITAB_CTA_CTE);
        }

        [Fact]
        public void DoubleConstants_HaveExpectedValues()
        {
            Assert.Equal(0.015, Constantes.kRedondeoCantCuotasBalanceo, 3);
            Assert.Equal(0.999, Constantes.KdblRedondeoEspecial, 3);
        }

        [Fact]
        public void DateTimeConstants_HaveExpectedValues()
        {
            Assert.Equal(new DateTime(1999, 8, 2), Constantes.kDIASGE3);
        }
    }
}
