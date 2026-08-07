using AppLogic.People.Constants;
using AppLogic.People.Rules;
using System;
using AppLogic.People.Validators;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RequiredPersonDataTests
    {
        [Fact]
        public void AuditarPersona_SetsAuditFields()
        {
            var person = new Persona();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.ObtenerDbUserId()).Returns("testuser");

            PersonAuditStamp.Apply(person, 123, uow.Object);

            Assert.Equal("123", person.UsuarioModifFdp);
            Assert.Equal("testuser", person.UsuarioUltimaActualizacion);
            Assert.True(person.FechaUltimaActualizacion.HasValue);
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinPaisNacimiento_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.CodigoPaisNacimiento = null;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_BAS_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinNacionalidad_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.NacionalidadPersona = null;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_BAS_02");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinFechaVencimientoDocumento_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.FechaVtoDocumentoPersona = null;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_DOC_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinPaisDireccion_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.CodigoPais = null;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_DIR_01");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinTelefono1_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.Telefono1 = null;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_CON_02");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_SinCaracteristicaTelefono1_ReturnsExpectedError()
        {
            var person = BuildPersona();
            person.IdCaracteristicaPaisTel1 = 0;

            var result = RequiredPersonData.FindMissingRequiredData(person).ToFailure<bool>("TestMethod");

            AssertFailure(result, "DP_ACDP_CON_03");
        }

        [Fact]
        public void ValidarDatosObligatoriosPersona_AllDataComplete_ReturnsSuccess()
        {
            var gap = RequiredPersonData.FindMissingRequiredData(BuildPersona());

            Assert.Equal(PersonDataGap.None, gap);
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
