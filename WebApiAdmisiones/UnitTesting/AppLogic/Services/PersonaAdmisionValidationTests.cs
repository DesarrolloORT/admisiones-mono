using System;
using System.Collections.Generic;
using AppLogic.Constants;
using AppLogic.Requests;
using AppLogic.Utilities;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaAdmisionValidationTests
    {
        [Fact]
        public void CrearActualizarPersonaRequestDesdeEncuesta_MapeaCamposPrincipales()
        {
            var request = BuildValidEncuestaRequest();

            var result = PersonaAdmisionValidation.CrearActualizarPersonaRequestDesdeEncuesta(request);

            Assert.Equal(request.PrimerApellido, result.PrimerApellido);
            Assert.Equal(request.PrimerNombre, result.PrimerNombre);
            Assert.Equal(request.Mail, result.Mail);
            Assert.Equal(request.CodigoCiudad, result.CodigoCiudad);
            Assert.Equal(request.TrabajaActualmente, result.TrabajaActualmente);
            Assert.Equal(request.TipoJornada, result.TipoJornada);
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_SexoInvalido_ReturnsExpectedError()
        {
            var request = BuildValidEncuestaRequest();
            request.Sexo = "X";

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, "PER_AP_06");
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_SgiSinTrabajaActualmente_ReturnsExpectedError()
        {
            var request = BuildValidEncuestaRequest();
            request.TrabajaActualmente = string.Empty;

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                PersonaAdmisionConstants.TipoPersonaSgi,
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, "PER_DPE_15");
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_SgiConJornadaInvalida_ReturnsExpectedError()
        {
            var request = BuildValidEncuestaRequest();
            request.TrabajaActualmente = "S";
            request.TipoJornada = 3;

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                PersonaAdmisionConstants.TipoPersonaSgi,
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, "PER_DPE_16");
        }

        [Theory]
        [InlineData(0, 5, 5, "SI", 1, 1, "PER_DPE_17")]
        [InlineData(6, 0, 5, "SI", 1, 1, "PER_DPE_18")]
        [InlineData(6, 5, 0, "SI", 1, 1, "PER_DPE_19")]
        [InlineData(7, 5, 5, "SI", 1, 1, "PER_DPE_25")]
        [InlineData(6, 5, 5, "TALVEZ", 1, 1, "PER_DPE_26")]
        [InlineData(6, 5, 5, "SI", 3, 1, "PER_DPE_27")]
        [InlineData(6, 5, 5, "SI", 1, 3, "PER_DPE_28")]
        public void ValidarDatosPersonaEncuestaRequest_ReglasBase_ReturnsExpectedError(
            long ultimoAnioSexto,
            int instruccionMadre,
            int instruccionPadre,
            string informarEncuesta,
            long ultimoAnioSecundaria,
            long nivelDecision,
            string expectedErrorCode)
        {
            var request = BuildValidEncuestaRequest();
            request.UltimoAnioSexto = ultimoAnioSexto;
            request.InstruccionMadre = instruccionMadre;
            request.InstruccionPadre = instruccionPadre;
            request.InformarEncuesta = informarEncuesta;
            request.UltimoAnioSecundaria = ultimoAnioSecundaria;
            request.NivelDecision = nivelDecision;

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, expectedErrorCode);
        }

        [Theory]
        [InlineData(1, 2, 1, "SI", "NO", true, "PER_DPE_20")]
        [InlineData(2, 1, 1, "SI", "NO", true, "PER_DPE_21")]
        [InlineData(2, 2, 99, "SI", "NO", true, "PER_DPE_22")]
        [InlineData(2, 2, 1, "QUIZAS", "NO", true, "PER_DPE_23")]
        public void ValidarDatosPersonaEncuestaRequest_ReglasDecisionEscalares_ReturnsExpectedError(
            int? decisionCarrera,
            int? decisionUniversidad,
            int compartidoCon,
            string infoOtrasUniversidadesAntes,
            string informarEncuesta,
            bool includeMotivos,
            string expectedErrorCode)
        {
            var request = BuildValidEncuestaRequest();
            request.DecisionCarrera = decisionCarrera;
            request.DecisionUniversidad = decisionUniversidad;
            request.CompartidoCon = compartidoCon;
            request.InfoOtrasUniversidadesAntes = infoOtrasUniversidadesAntes;
            request.InformarEncuesta = informarEncuesta;
            if (!includeMotivos)
            {
                request.OpcionesMotivosSeleccionados.Clear();
            }

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, expectedErrorCode);
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_InfoOtrasUniversidadesSinDetalle_ReturnsExpectedError()
        {
            var request = BuildValidEncuestaRequest();
            request.InfoOtrasUniversidadesAntes = CommonConstants.Booleanos.Si;
            request.UniversidadesConsideradas.Clear();

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, "PER_DPE_24");
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_SinMotivosSeleccionados_ReturnsExpectedError()
        {
            var request = BuildValidEncuestaRequest();
            request.OpcionesMotivosSeleccionados.Clear();

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, "PER_DPE_37");
        }

        [Theory]
        [InlineData(null, 3, true, 3, true, 3, false, false, "PER_DPE_29")]
        [InlineData(true, 0, true, 3, true, 3, false, false, "PER_DPE_30")]
        [InlineData(false, 3, null, 3, true, 3, false, false, "PER_DPE_31")]
        [InlineData(false, 3, true, 0, true, 3, false, false, "PER_DPE_32")]
        [InlineData(false, 3, false, 3, null, 3, false, false, "PER_DPE_33")]
        [InlineData(false, 3, false, 3, true, 0, false, false, "PER_DPE_34")]
        [InlineData(false, 3, false, 3, false, 3, null, false, "PER_DPE_35")]
        [InlineData(false, 3, false, 3, false, 3, true, false, "PER_DPE_36")]
        public void ValidarDatosPersonaEncuestaRequest_ReglasAsesoramiento_ReturnsExpectedError(
            bool? asesoramientoOrt,
            long valoracionAsesoramientoOrt,
            bool? vistaSitioWebOrt,
            long valoracionSitioWeb,
            bool? vistaInstalacionesOrt,
            long valoracionInstalacionesOrt,
            bool? publicidadOrt,
            bool includePublicidad,
            string expectedErrorCode)
        {
            var request = BuildValidEncuestaRequest();
            request.AsesoramientoOrt = asesoramientoOrt;
            request.ValoracionAsesoramientoOrt = valoracionAsesoramientoOrt;
            request.VistaSitioWebOrt = vistaSitioWebOrt;
            request.ValoracionSitioWeb = valoracionSitioWeb;
            request.VistaInstalacionesOrt = vistaInstalacionesOrt;
            request.ValoracionInstalacionesOrt = valoracionInstalacionesOrt;
            request.PublicidadOrt = publicidadOrt;
            if (!includePublicidad)
            {
                request.OpcionesPublicidadSeleccionadas.Clear();
            }

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, expectedErrorCode);
        }

        [Theory]
        [InlineData(6L, null, 5, false, 5, false, null, true, "PER_DPE_38")]
        [InlineData(6L, 10L, 5, null, 5, false, false, true, "PER_DPE_39")]
        [InlineData(6L, 10L, 4, false, 6, null, false, true, "PER_DPE_40")]
        [InlineData(6L, 10L, 4, false, 4, false, null, true, "PER_DPE_41")]
        [InlineData(6L, 10L, 4, false, 4, false, true, false, "PER_DPE_42")]
        public void ValidarDatosPersonaEncuestaRequest_ReglasEducacion_ReturnsExpectedError(
            long ultimoAnioSexto,
            long? codigoTitulo,
            int instruccionMadre,
            bool? instruccionMadreOrt,
            int instruccionPadre,
            bool? instruccionPadreOrt,
            bool? tieneEducacionSuperior,
            bool includeUniversidadesEducacionSuperior,
            string expectedErrorCode)
        {
            var request = BuildValidEncuestaRequest();
            request.UltimoAnioSexto = ultimoAnioSexto;
            request.CodigoTitulo = codigoTitulo;
            request.InstruccionMadre = instruccionMadre;
            request.InstruccionMadreOrt = instruccionMadreOrt;
            request.InstruccionPadre = instruccionPadre;
            request.InstruccionPadreOrt = instruccionPadreOrt;
            request.TieneEducacionSuperior = tieneEducacionSuperior;
            if (!includeUniversidadesEducacionSuperior)
            {
                request.UniversidadesEducacionSuperior.Clear();
            }

            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            AssertFailure(result, expectedErrorCode);
        }

        [Fact]
        public void ValidarDatosPersonaEncuestaRequest_RequestValido_ReturnsSuccess()
        {
            var result = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                BuildValidEncuestaRequest(),
                "WEB",
                "TestMethod",
                PersonaAdmisionConstants.Parametros.FechaMinimaNacimiento);

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        private static GuardarDatosPersonaEncuestaRequest BuildValidEncuestaRequest()
        {
            return new GuardarDatosPersonaEncuestaRequest
            {
                PrimerApellido = "Perez",
                SegundoApellido = "Lopez",
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com",
                Direccion = "18 de julio 1234",
                Sexo = CommonConstants.Sexo.Femenino,
                FechaNacimiento = new DateTime(2000, 1, 1),
                TrabajaActualmente = "N",
                TipoJornada = 1,
                Telefono1 = "123456",
                Telefono2 = "789012",
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1,
                Documento = "12345678",
                TipoDocumento = "CI",
                IdProducto = 10,
                IdProceso = 20,
                CodigoTitulo = 10,
                UltimoAnioSexto = 6,
                VecesSexto = 1,
                VecesSextoBool = true,
                InstruccionPadre = 4,
                InstruccionMadre = 4,
                DecisionCarrera = 2,
                DecisionUniversidad = 2,
                InfoOtrasUniversidadesAntes = CommonConstants.Booleanos.No,
                CompartidoCon = PersonaAdmisionConstants.CompartidoCon.Padres,
                CodigoInstitucionBac = 100,
                InformarEncuesta = CommonConstants.Booleanos.Si,
                NombreInstitucion = "Instituto",
                UltimoAnioSecundaria = 1,
                TieneEducacionSuperior = false,
                NivelDecision = 1,
                AsesoramientoOrt = false,
                ValoracionAsesoramientoOrt = 3,
                VistaSitioWebOrt = false,
                ValoracionSitioWeb = 3,
                VistaInstalacionesOrt = false,
                ValoracionInstalacionesOrt = 3,
                PublicidadOrt = false,
                InstruccionMadreOrt = false,
                InstruccionPadreOrt = false,
                UniversidadesConsideradas = new List<EmpresaEncuestaRequest>
                {
                    new() { CodigoEmpresa = 1, Nombre = "ORT" }
                },
                UniversidadesEducacionSuperior = new List<EmpresaEncuestaRequest>
                {
                    new() { CodigoEmpresa = 2, Nombre = "UDELAR" }
                },
                OpcionesPublicidadSeleccionadas = new List<PublicidadEncuestaRequest>
                {
                    new() { IdPublicidad = 1, NombrePublicidad = "Publicidad" }
                },
                OpcionesMotivosSeleccionados = new List<MotivoEncuestaRequest>
                {
                    new() { IdMotivo = 1, NombreMotivo = "Motivo" }
                }
            };
        }

        private static void AssertFailure(OperationResult<bool> result, string expectedErrorCode)
        {
            Assert.False(result.Success);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
    }
}
