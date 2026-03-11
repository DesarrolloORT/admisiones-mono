using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using WebApiAdmisiones.Security;
using AppLogic.Helpers;

namespace UnitTesting.Security
{
    public class ResponseRedactionHelperTests
    {
        // DTO de prueba sin atributo Redact
        private class TestDto
        {
            public string PublicField { get; set; } = "public";
            public string SensitiveField { get; set; } = "sensitive";
            public int NumericField { get; set; } = 42;
        }

        private class TestDtoWithRedact
        {
            public string NormalField { get; set; } = "normal";
            
            [Redact(RedactionMode.Full)]
            public string FullRedactField { get; set; } = "secret123";
            
            [Redact(RedactionMode.PreserveLength)]
            public string PreserveLengthField { get; set; } = "password";
            
            [Redact(RedactionMode.First4Last4)]
            public string First4Last4Field { get; set; } = "1234567890";
            
            [Redact(RedactionMode.HashSha256)]
            public string HashField { get; set; } = "hash_me";
        }

        private class TestDtoWithDateTime
        {
            [Redact(RedactionMode.HashSha256)]
            public DateTime DateField { get; set; } = new DateTime(2024, 1, 1);
            
            [Redact(RedactionMode.BinaryLength)]
            public string StringField { get; set; } = "test";
        }

        private class TestDtoWithBinary
        {
            [Redact(RedactionMode.BinaryLength)]
            public byte[] BinaryData { get; set; } = new byte[] { 1, 2, 3, 4, 5 };
        }

        private class TestDtoWithShortString
        {
            [Redact(RedactionMode.First4Last4)]
            public string ShortString { get; set; } = "1234567"; // 7 caracteres
        }

        private class NestedDto
        {
            public string Name { get; set; } = "nested";
            public TestDto Inner { get; set; } = new TestDto();
        }

        private class CircularDto
        {
            public string Name { get; set; } = "circular";
            public CircularDto? Next { get; set; }
        }

        [Fact]
        public void Redact_NullValue_ReturnsNull()
        {
            // Act
            var result = ResponseRedactionHelper.Redact(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Redact_SimpleTypes_ReturnsSameValue()
        {
            // Arrange & Act & Assert
            Assert.Equal(42, ResponseRedactionHelper.Redact(42));
            Assert.Equal("test", ResponseRedactionHelper.Redact("test"));
            Assert.Equal(3.14m, ResponseRedactionHelper.Redact(3.14m));
            Assert.Equal(true, ResponseRedactionHelper.Redact(true));
        }

        [Fact]
        public void Redact_Enum_ReturnsSameValue()
        {
            // Arrange
            var enumValue = DayOfWeek.Monday;

            // Act
            var result = ResponseRedactionHelper.Redact(enumValue);

            // Assert
            Assert.Equal(enumValue, result);
        }

        [Fact]
        public void Redact_DateTime_ReturnsSameValue()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);

            // Act
            var result = ResponseRedactionHelper.Redact(date);

            // Assert
            Assert.Equal(date, result);
        }

        [Fact]
        public void Redact_Guid_ReturnsSameValue()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            var result = ResponseRedactionHelper.Redact(guid);

            // Assert
            Assert.Equal(guid, result);
        }

        [Fact]
        public void Redact_ListOfSimpleTypes_ReturnsRedactedList()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act
            var result = ResponseRedactionHelper.Redact(list);

            // Assert
            Assert.NotNull(result);
            var resultList = Assert.IsType<List<object?>>(result);
            Assert.Equal(3, resultList.Count);
            Assert.Contains(1, resultList);
            Assert.Contains(2, resultList);
            Assert.Contains(3, resultList);
        }

        [Fact]
        public void Redact_Array_ReturnsRedactedList()
        {
            // Arrange
            var array = new[] { "a", "b", "c" };

            // Act
            var result = ResponseRedactionHelper.Redact(array);

            // Assert
            Assert.NotNull(result);
            var resultList = Assert.IsType<List<object?>>(result);
            Assert.Equal(3, resultList.Count);
        }

        [Fact]
        public void Redact_ObjectWithoutRedactAttributes_ReturnsDictionary()
        {
            // Arrange
            var dto = new TestDto();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Equal("public", dict["PublicField"]);
            Assert.Equal("sensitive", dict["SensitiveField"]);
            Assert.Equal(42, dict["NumericField"]);
        }

        [Fact]
        public void Redact_ObjectWithFullRedactMode_ReturnsAsterisks()
        {
            // Arrange
            var dto = new TestDtoWithRedact();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Equal("***", dict["FullRedactField"]);
        }

        [Fact]
        public void Redact_ObjectWithPreserveLengthMode_PreservesLength()
        {
            // Arrange
            var dto = new TestDtoWithRedact();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var redacted = Assert.IsType<string>(dict["PreserveLengthField"]);
            Assert.Equal(8, redacted.Length); // "password" tiene 8 caracteres
            Assert.All(redacted, c => Assert.Equal('*', c));
        }

        [Fact]
        public void Redact_ObjectWithFirst4Last4Mode_ShowsFirstAndLastFour()
        {
            // Arrange
            var dto = new TestDtoWithRedact();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var redacted = Assert.IsType<string>(dict["First4Last4Field"]);
            Assert.StartsWith("1234", redacted);
            Assert.EndsWith("7890", redacted);
            Assert.Contains("***", redacted);
        }

        [Fact]
        public void Redact_ObjectWithFirst4Last4Mode_ShortString_ReturnsAsterisks()
        {
            // Arrange
            var dto = new TestDtoWithShortString();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Equal("***", dict["ShortString"]);
        }

        [Fact]
        public void Redact_ObjectWithHashMode_ReturnsHexHash()
        {
            // Arrange
            var dto = new TestDtoWithRedact();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var hash = Assert.IsType<string>(dict["HashField"]);
            Assert.Matches("^[A-F0-9]+$", hash); // Debe ser hexadecimal
            Assert.Equal(64, hash.Length); // SHA256 produce 64 caracteres hex
        }

        [Fact]
        public void Redact_ObjectWithDateTimeAndHash_ConvertsAndHashes()
        {
            // Arrange
            var dto = new TestDtoWithDateTime();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var dateHash = Assert.IsType<string>(dict["DateField"]);
            Assert.Matches("^[A-F0-9]+$", dateHash);
        }

        [Fact]
        public void Redact_ObjectWithBinaryData_ShowsBinaryLength()
        {
            // Arrange
            var dto = new TestDtoWithBinary();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var redacted = Assert.IsType<string>(dict["BinaryData"]);
            Assert.Equal("<bin:5>", redacted);
        }

        [Fact]
        public void Redact_ObjectWithBinaryLengthModeOnString_ShowsLength()
        {
            // Arrange
            var dto = new TestDtoWithDateTime();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            var redacted = Assert.IsType<string>(dict["StringField"]);
            Assert.StartsWith("<len:", redacted);
        }

        [Fact]
        public void Redact_NestedObject_RedactsRecursively()
        {
            // Arrange
            var dto = new NestedDto();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Equal("nested", dict["Name"]);
            
            var innerDict = Assert.IsType<Dictionary<string, object?>>(dict["Inner"]);
            Assert.Equal("public", innerDict["PublicField"]);
        }

        [Fact]
        public void Redact_CircularReference_HandlesGracefully()
        {
            // Arrange
            var dto = new CircularDto();
            dto.Next = dto; // Referencia circular

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Equal("circular", dict["Name"]);
            Assert.Null(dict["Next"]); // La referencia circular debe ser null
        }

        [Fact]
        public void Redact_ListOfComplexObjects_RedactsAll()
        {
            // Arrange
            var list = new List<TestDto>
            {
                new TestDto { PublicField = "first" },
                new TestDto { PublicField = "second" }
            };

            // Act
            var result = ResponseRedactionHelper.Redact(list);

            // Assert
            Assert.NotNull(result);
            var resultList = Assert.IsType<List<object?>>(result);
            Assert.Equal(2, resultList.Count);
            
            var firstDict = Assert.IsType<Dictionary<string, object?>>(resultList[0]);
            Assert.Equal("first", firstDict["PublicField"]);
            
            var secondDict = Assert.IsType<Dictionary<string, object?>>(resultList[1]);
            Assert.Equal("second", secondDict["PublicField"]);
        }

        [Fact]
        public void Redact_NullPropertyValue_ReturnsNullInDictionary()
        {
            // Arrange
            var dto = new TestDto { SensitiveField = null! };

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Null(dict["SensitiveField"]);
        }

        private class WriteOnlyPropertyDto
        {
            private string _field = "test";
            public string WriteOnly { set => _field = value; }
            public string ReadWrite { get; set; } = "readable";
        }

        [Fact]
        public void Redact_WriteOnlyProperty_SkipsProperty()
        {
            // Arrange
            var dto = new WriteOnlyPropertyDto();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.DoesNotContain("WriteOnly", dict.Keys);
            Assert.Contains("ReadWrite", dict.Keys);
        }

        private class NullRedactValueDto
        {
            [Redact(RedactionMode.Full)]
            public string? NullField { get; set; } = null;
        }

        [Fact]
        public void Redact_RedactAttributeOnNullValue_ReturnsNull()
        {
            // Arrange
            var dto = new NullRedactValueDto();

            // Act
            var result = ResponseRedactionHelper.Redact(dto);

            // Assert
            Assert.NotNull(result);
            var dict = Assert.IsType<Dictionary<string, object?>>(result);
            Assert.Null(dict["NullField"]);
        }
    }
}
