using System;
using AppLogic.Helpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaValidationTests
    {
        [Fact]
        public void AuditarPersona_WithoutConfirmacionDatosPersonales_SetsAuditFields()
        {
            var persona = new Persona();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.ObtenerDbUserId()).Returns("testuser");

            PersonaValidation.AuditarPersona(persona, 123, uow.Object, false);

            Assert.Equal("123", persona.UsuarioModifFdp);
            Assert.Equal("testuser", persona.UsuarioUltimaActualizacion);
            Assert.True(persona.FechaUltimaActualizacion.HasValue);
        }

        [Fact]
        public void AuditarPersona_WithConfirmacionDatosPersonales_SetsConfirmationFields()
        {
            var persona = new Persona();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.ObtenerDbUserId()).Returns("testuser");

            PersonaValidation.AuditarPersona(persona, 123, uow.Object, true);

            Assert.Equal("123", persona.UsuarioModifFdp);
            Assert.Equal("testuser", persona.UsuarioUltimaActualizacion);
            Assert.True(persona.FechaUltimaActualizacion.HasValue);
            Assert.True(persona.FechaConfDatosPersona.HasValue);
            Assert.Equal("testuser", persona.UsuarioConfDatosPersona);
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinPaisNacimiento_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.CodigoPaisNacimiento = null;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_BAS_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinNacionalidad_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.NacionalidadPersona = null;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_BAS_02");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinFechaVencimientoDocumento_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.FechaVtoDocumentoPersona = null;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_DOC_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinPaisDireccion_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.CodigoPais = null;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_DIR_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinTelefono1_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.Telefono1 = null;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_CON_02");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinCaracteristicaTelefono1_ReturnsExpectedError()
        {
            var persona = BuildPersona();
            persona.IdCaracteristicaPaisTel1 = 0;

            var result = PersonaValidation.ValidarDatosObligatoriosPersona(persona, "TestMethod");

            AssertFailure(result, "DP_ACDP_CON_03");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_AllDataComplete_ReturnsSuccess()
        {
            var result = PersonaValidation.ValidarDatosObligatoriosPersona(BuildPersona(), "TestMethod");

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        private static Persona BuildPersona()
        {
            return new Persona
            {
                CodigoPaisNacimiento = 1,
                NacionalidadPersona = "Uruguaya",
                FuncionarioActivoPersona = "SI",
                FechaVtoDocumentoPersona = DateTime.Now.AddYears(1),
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1,
                Direccion = "Calle 123",
                Email = "test@test.com",
                Telefono1 = "12345678",
                IdCaracteristicaPaisTel1 = 598
            };
        }

        private static void AssertFailure(OperationResult<bool> result, string expectedErrorCode)
        {
            Assert.False(result.Success);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
    }
}
