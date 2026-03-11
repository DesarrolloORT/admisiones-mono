// Archivo: UnitTesting/OperationResultTests.cs
using System;
using Utilities;
using Xunit;

namespace UnitTesting.Utilities
{
    public class OperationResultTests
    {
       

        [Fact]
        public void Ok_StaticMethod_SetsSuccessAndData()
        {
            var result = OperationResult<string>.Ok("valor", "MetodoTest");

            Assert.True(result.Success);
            Assert.Equal("MetodoTest", result.Method);
            Assert.Equal("valor", result.Data);
            Assert.True(string.IsNullOrEmpty(result.ErrorCode));
            Assert.True(string.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Failed_StaticMethod_SetsErrorFields()
        {
            var result = OperationResult<string>.IsFailed("ERR01", "MetodoTest", "Mensaje de error", 400, "dataError");

            Assert.False(result.Success);
            Assert.Equal("ERR01", result.ErrorCode);
            Assert.Equal("MetodoTest", result.Method);
            Assert.Equal("Mensaje de error", result.Message);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("dataError", result.Data);
        }

        [Fact]
        public void Failed_StaticMethod_DefaultData()
        {
            var result = OperationResult<string>.IsFailed("ERR02", "MetodoTest", "Otro error");

            Assert.False(result.Success);
            Assert.Equal("ERR02", result.ErrorCode);
            Assert.Equal("MetodoTest", result.Method);
            Assert.Equal("Otro error", result.Message);
            Assert.Equal(400, result.HttpCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Ok_AllowsAnyType()
        {
            var resultInt = OperationResult<int>.Ok(123, "MetodoInt");
            var resultObj = OperationResult<object>.Ok(new { X = 1 }, "MetodoObj");

            Assert.Equal(123, resultInt.Data);
            Assert.NotNull(resultObj.Data);
        }

        [Fact]
        public void Failed_AllowsAnyType()
        {
            var resultInt = OperationResult<int>.IsFailed("E", "M", "msg", 500, 999);
            var resultObj = OperationResult<object>.IsFailed("E", "M", "msg", 500, new { X = 2 });

            Assert.Equal(999, resultInt.Data);
            Assert.NotNull(resultObj.Data);
        }
    }
}
