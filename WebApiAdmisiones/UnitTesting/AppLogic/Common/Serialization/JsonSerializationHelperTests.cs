using System.Text.Json;
using AppLogic.Common.Serialization;

namespace UnitTesting.AppLogic.Common.Serialization
{
    public class JsonSerializationHelperTests
    {
        private sealed class SampleDto
        {
            public string? Nombre { get; set; }
            public int Edad { get; set; }
        }

        [Fact]
        public void TryDeserialize_WithValidJson_ReturnsObject()
        {
            var json = "{\"nombre\":\"Ana\",\"edad\":30}";

            var result = JsonSerializationHelper.TryDeserialize<SampleDto>(json, JsonSerializationDefaults.Redis);

            Assert.NotNull(result);
            Assert.Equal("Ana", result!.Nombre);
            Assert.Equal(30, result.Edad);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryDeserialize_WithNullOrEmpty_ReturnsNull(string? json)
        {
            var result = JsonSerializationHelper.TryDeserialize<SampleDto>(json);

            Assert.Null(result);
        }

        [Fact]
        public void TryDeserialize_WithInvalidJson_ReturnsNull()
        {
            var result = JsonSerializationHelper.TryDeserialize<SampleDto>("{ not valid json ");

            Assert.Null(result);
        }

        [Fact]
        public void TryDeserialize_WithoutOptions_UsesDefaultDeserialization()
        {
            var json = "{\"Nombre\":\"Ana\",\"Edad\":30}";

            var result = JsonSerializationHelper.TryDeserialize<SampleDto>(json);

            Assert.NotNull(result);
            Assert.Equal("Ana", result!.Nombre);
        }
    }

    public class JsonSerializationDefaultsTests
    {
        private sealed class SampleDto
        {
            public string? Nombre { get; set; }
            public string? Apellido { get; set; }
        }

        [Fact]
        public void Redis_UsesCamelCaseNaming()
        {
            var dto = new SampleDto { Nombre = "Ana", Apellido = null };

            var json = JsonSerializer.Serialize(dto, JsonSerializationDefaults.Redis);

            Assert.Contains("\"nombre\"", json);
        }

        [Fact]
        public void Redis_OmitsNullProperties()
        {
            var dto = new SampleDto { Nombre = "Ana", Apellido = null };

            var json = JsonSerializer.Serialize(dto, JsonSerializationDefaults.Redis);

            Assert.DoesNotContain("apellido", json);
        }
    }
}
