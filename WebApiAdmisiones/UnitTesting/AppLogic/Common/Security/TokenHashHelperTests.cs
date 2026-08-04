using System.Security.Cryptography;
using System.Text;
using AppLogic.Common.Security;

namespace UnitTesting.AppLogic.Common.Security
{
    public class TokenHashHelperTests
    {
        [Fact]
        public void HashSha256Base64_MatchesPreviousAlgorithm()
        {
            const string token = "refresh-token";
            var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            var result = TokenHashHelper.HashSha256Base64(token);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void HashSha256Base64_SameInput_ReturnsSameHash()
        {
            const string token = "some-token-value";

            var first = TokenHashHelper.HashSha256Base64(token);
            var second = TokenHashHelper.HashSha256Base64(token);

            Assert.Equal(first, second);
        }

        [Fact]
        public void HashSha256Base64_DifferentInput_ReturnsDifferentHash()
        {
            var first = TokenHashHelper.HashSha256Base64("token-a");
            var second = TokenHashHelper.HashSha256Base64("token-b");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void HashSha256Base64_ReturnsValidBase64OfSha256Length()
        {
            var result = TokenHashHelper.HashSha256Base64("any-value");

            Assert.Equal(32, Convert.FromBase64String(result).Length);
        }
    }
}
