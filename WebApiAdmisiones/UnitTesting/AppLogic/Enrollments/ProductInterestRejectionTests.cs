using System;
using System.Linq;
using AppLogic.Enrollments.Constants;
using Xunit;

namespace UnitTesting.AppLogic.Enrollments
{
    /// <summary>
    /// El catálogo de rechazos es el único lugar donde viven los códigos GEN_IP_* y sus HTTP.
    /// Estos tests fijan ese contrato: si alguien agrega un motivo sin mapeo, o cambia un código,
    /// falla acá y no en producción.
    /// </summary>
    public class ProductInterestRejectionTests
    {
        private const string Method = "RegisterProductInterest";

        [Theory]
        [InlineData(ProductInterestRejection.InvalidRequestedOfferings, "GEN_IP_11", 400)]
        [InlineData(ProductInterestRejection.PersonNotFound, "GEN_IP_01", 404)]
        [InlineData(ProductInterestRejection.InvalidProduct, "GEN_IP_02", 400)]
        [InlineData(ProductInterestRejection.ProcessNotEnabled, "GEN_IP_03", 400)]
        [InlineData(ProductInterestRejection.AlreadyEnrolledInProduct, "GEN_IP_04", 409)]
        [InlineData(ProductInterestRejection.PendingEnrollment, "GEN_IP_05", 409)]
        [InlineData(ProductInterestRejection.InterestAlreadyRegistered, "GEN_IP_06", 409)]
        [InlineData(ProductInterestRejection.InvalidOfferingId, "GEN_IP_07", 400)]
        [InlineData(ProductInterestRejection.OfferingNotFound, "GEN_IP_07", 404)]
        [InlineData(ProductInterestRejection.OfferingNotInProduct, "GEN_IP_08", 400)]
        [InlineData(ProductInterestRejection.OfferingClosed, "GEN_IP_09", 409)]
        [InlineData(ProductInterestRejection.OfferingNotInProcess, "GEN_IP_10", 400)]
        public void ToFailure_PreservaCodigoYHttpDelContratoPublico(
            ProductInterestRejection rejection, string errorCodeEsperado, int httpEsperado)
        {
            var result = rejection.ToFailure<bool>(Method);

            Assert.False(result.Success);
            Assert.Equal(errorCodeEsperado, result.ErrorCode);
            Assert.Equal(httpEsperado, result.HttpCode);
            Assert.Equal(Method, result.Method);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }

        [Fact]
        public void ToFailure_TodoMotivoDistintoDeNone_TieneMapeo()
        {
            var reasons = Enum.GetValues<ProductInterestRejection>()
                .Where(m => m != ProductInterestRejection.None);

            foreach (var motivo in reasons)
            {
                var result = motivo.ToFailure<bool>(Method);
                Assert.False(string.IsNullOrWhiteSpace(result.ErrorCode));
            }
        }

        [Fact]
        public void ToFailure_None_NoTieneRepresentacionHttp()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ProductInterestRejection.None.ToFailure<bool>(Method));
        }
    }
}
