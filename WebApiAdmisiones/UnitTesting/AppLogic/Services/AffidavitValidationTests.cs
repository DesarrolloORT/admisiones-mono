using System.Collections.Generic;
using AppLogic.Scholarships.Constants;
using AppLogic.DevartDTOs;
using AppLogic.Scholarships.Validators;
using BusinessLogic.Entities;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class AffidavitValidationTests
    {
        [Fact]
        public void ValidarSolicitudGuardadoDeclaracion_Null_ReturnsExpectedError()
        {
            var result = AffidavitValidation.ValidateSaveRequest(
                null).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_00");
        }

        [Fact]
        public void ValidarSolicitudGuardadoDeclaracion_IdInscriptoPruebaInvalido_ReturnsExpectedError()
        {
            var result = AffidavitValidation.ValidateSaveRequest(
                new DtoDeclaracionJuradaWebDevart { IdInscriptoPrueba = 0 }).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_15");
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_AntecedentesAcademicosIncompletos_ReturnsExpectedError()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdTipoDescuento = ScholarshipFundConstants.Declaracion.TipoDescuentoAntecedentesAcademicos
            };

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_07");
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_UniversidadOrtSinFacultad_ReturnsExpectedError()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdTipoDescuento = ScholarshipFundConstants.Declaracion.TipoDescuentoAntecedentesAcademicos,
                CodigoInstitucionBac = 100,
                CodigoInstitucionUniv = ScholarshipFundConstants.Declaracion.CodigoInstitucionOrt,
                CarreraUniversitariaDj = "Ingenieria",
                CantMateriasAprobadasDj = 10,
                CantMatExaReprobadosDj = 1,
                PromTotalCalificacionesDj = 9
            };

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_08");
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_IngresoSinArchivo_ReturnsExpectedError()
        {
            var affidavit = BuildDeclaracionConIngreso(new IngresoMensualNfDj
            {
                NominalIngresoNfDj = 1000,
                DescuentoslegalesIngresoNf = 100
            });

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_14");
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_UniversidadNoOrtConFacultad_ReturnsExpectedError()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdTipoDescuento = ScholarshipFundConstants.Declaracion.TipoDescuentoAntecedentesAcademicos,
                CodigoInstitucionBac = 100,
                CodigoInstitucionUniv = 999,
                FacultadUniversidadDj = "No corresponde",
                CarreraUniversitariaDj = "Ingenieria",
                CantMateriasAprobadasDj = 10,
                CantMatExaReprobadosDj = 1,
                PromTotalCalificacionesDj = 9
            };

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_09");
        }

        [Theory]
        [InlineData(-1, 100, 100, "FDB_GDJ_10")]
        [InlineData(1000.5, 100, 100, "FDB_GDJ_11")]
        [InlineData(999, 100, 100, "FDB_GDJ_12")]
        public void ValidarConfirmacionDeclaracion_IngresoNominalInvalido_ReturnsExpectedError(
            decimal nominal,
            decimal descuentos,
            decimal liquido,
            string expectedErrorCode)
        {
            var affidavit = BuildDeclaracionConIngreso(new IngresoMensualNfDj
            {
                NominalIngresoNfDj = nominal,
                DescuentoslegalesIngresoNf = descuentos,
                LiquidoIngresoNfDj = liquido,
                ArchivoIngresoNfDj = new byte[] { 1, 2, 3 }
            });

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, expectedErrorCode);
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_DescuentosDecimales_ReturnsExpectedError()
        {
            var affidavit = BuildDeclaracionConIngreso(new IngresoMensualNfDj
            {
                NominalIngresoNfDj = 1000,
                DescuentoslegalesIngresoNf = 100.5m,
                LiquidoIngresoNfDj = 900,
                ArchivoIngresoNfDj = new byte[] { 1, 2, 3 }
            });

            var result = AffidavitValidation.ValidateForConfirmation(affidavit).ToFailure<bool>("TestMethod");

            AssertFailure(result, "FDB_GDJ_13");
        }

        [Fact]
        public void ValidarConfirmacionDeclaracion_DeclaracionValida_ReturnsSuccess()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdTipoDescuento = ScholarshipFundConstants.Declaracion.TipoDescuentoAntecedentesAcademicos,
                CodigoInstitucionBac = 100,
                CodigoInstitucionUniv = ScholarshipFundConstants.Declaracion.CodigoInstitucionOrt,
                FacultadUniversidadDj = "Universidad ORT Uruguay",
                CarreraUniversitariaDj = "Ingenieria",
                CantMateriasAprobadasDj = 10,
                CantMatExaReprobadosDj = 1,
                PromTotalCalificacionesDj = 9,
                IntegranteNfDjs = new List<IntegranteNfDj>
                {
                    new()
                    {
                        IngresoMensualNfDjs = new List<IngresoMensualNfDj>
                        {
                            new()
                            {
                                NominalIngresoNfDj = 1000,
                                DescuentoslegalesIngresoNf = 100,
                                ArchivoIngresoNfDj = new byte[] { 1, 2, 3 }
                            }
                        }
                    }
                }
            };

            var rechazo = AffidavitValidation.ValidateForConfirmation(affidavit);

            Assert.Equal(AffidavitRejection.None, rechazo);
        }

        [Fact]
        public void ValidarArchivoAdjunto_ArchivoVacio_ReturnsExpectedError()
        {
            var result = AffidavitValidation.ValidateAttachment(
                [],
                "adjunto.pdf",
                "TestMethod");

            AssertFailure(result, "FILE_VAL_01");
        }

        [Fact]
        public void ValidarArchivoAdjunto_PdfValido_ReturnsSanitizedName()
        {
            var result = AffidavitValidation.ValidateAttachment(
                [0x25, 0x50, 0x44, 0x46, 0x2D],
                "mi<adjunto>.pdf",
                "TestMethod");

            Assert.True(result.Success);
            Assert.Equal("mi_adjunto.pdf", result.Data);
        }

        private static void AssertFailure<T>(OperationResult<T> result, string expectedErrorCode)
        {
            Assert.False(result.Success);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }

        private static DeclaracionJuradaWeb BuildDeclaracionConIngreso(IngresoMensualNfDj ingreso)
        {
            return new DeclaracionJuradaWeb
            {
                IdTipoDescuento = 1,
                IntegranteNfDjs = new List<IntegranteNfDj>
                {
                    new()
                    {
                        IngresoMensualNfDjs = new List<IngresoMensualNfDj> { ingreso }
                    }
                }
            };
        }
    }
}
