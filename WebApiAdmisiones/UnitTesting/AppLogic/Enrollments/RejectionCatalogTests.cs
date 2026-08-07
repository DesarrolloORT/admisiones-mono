using System;
using System.Linq;
using AppLogic.Enrollments.Constants;
using AppLogic.People.Constants;
using AppLogic.Scholarships.Constants;
using Xunit;

namespace UnitTesting.AppLogic.Enrollments
{
    /// <summary>
    /// Cada catálogo de rechazos es el único lugar donde viven los códigos de error y sus HTTP.
    /// Estos tests garantizan que ningún motivo quede sin mapeo y fijan los códigos que el front
    /// ya consume.
    /// </summary>
    public class RejectionCatalogTests
    {
        private const string Method = "TestMethod";

        [Theory]
        [InlineData(InitialSurveyRejection.InvalidRequest, "INS_EI_02", 400)]
        [InlineData(InitialSurveyRejection.InvalidProduct, "INS_EI_03", 400)]
        [InlineData(InitialSurveyRejection.NoEnabledProcessForProduct, "INS_EI_34", 404)]
        [InlineData(InitialSurveyRejection.UniversityRequiresFifthOrSixthYear, "INS_EI_64", 400)]
        [InlineData(InitialSurveyRejection.InvalidSelectedUniversity, "INS_EI_25", 400)]
        [InlineData(InitialSurveyRejection.UnknownSelectedUniversity, "INS_EI_27", 400)]
        [InlineData(InitialSurveyRejection.MotherOrtGraduateRequired, "INS_EI_62", 400)]
        public void EncuestaInicial_PreservaCodigoYHttp(
            InitialSurveyRejection rejection, string errorCodeEsperado, int httpEsperado)
        {
            var result = rejection.ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal(errorCodeEsperado, result.ErrorCode);
            Assert.Equal(httpEsperado, result.HttpCode);
        }

        [Theory]
        [InlineData(PersonDataGap.BirthCountryMissing, "DP_ACDP_BAS_01")]
        [InlineData(PersonDataGap.DocumentExpiryMissing, "DP_ACDP_DOC_01")]
        [InlineData(PersonDataGap.AddressMissing, "DP_ACDP_DIR_04")]
        [InlineData(PersonDataGap.PhoneCountryCodeMissing, "DP_ACDP_CON_03")]
        public void DatosPersona_PreservaCodigo(PersonDataGap gap, string errorCodeEsperado)
        {
            var result = gap.ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal(errorCodeEsperado, result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Theory]
        [InlineData(AffidavitRejection.ModifiedAffidavitRequired, "FDB_GDJ_00")]
        [InlineData(AffidavitRejection.ValidTestEnrollmentRequired, "FDB_GDJ_15")]
        [InlineData(AffidavitRejection.AcademicBackgroundRequired, "FDB_GDJ_07")]
        [InlineData(AffidavitRejection.IncomeFileRequired, "FDB_GDJ_14")]
        public void DeclaracionJurada_PreservaCodigo(AffidavitRejection rejection, string errorCodeEsperado)
        {
            var result = rejection.ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal(errorCodeEsperado, result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void TodoMotivoDistintoDeNone_TieneMapeo()
        {
            foreach (var motivo in Enum.GetValues<InitialSurveyRejection>().Where(m => m != InitialSurveyRejection.None))
                Assert.False(string.IsNullOrWhiteSpace(motivo.ToFailure<bool>(Method).ErrorCode));

            foreach (var motivo in Enum.GetValues<PersonDataGap>().Where(m => m != PersonDataGap.None))
                Assert.False(string.IsNullOrWhiteSpace(motivo.ToFailure<bool>(Method).ErrorCode));

            foreach (var motivo in Enum.GetValues<AffidavitRejection>().Where(m => m != AffidavitRejection.None))
                Assert.False(string.IsNullOrWhiteSpace(motivo.ToFailure<bool>(Method).ErrorCode));
        }

        [Fact]
        public void None_NoTieneRepresentacionHttp()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => InitialSurveyRejection.None.ToFailure<bool>(Method));
            Assert.Throws<ArgumentOutOfRangeException>(() => PersonDataGap.None.ToFailure<bool>(Method));
            Assert.Throws<ArgumentOutOfRangeException>(() => AffidavitRejection.None.ToFailure<bool>(Method));
        }
    }
}
