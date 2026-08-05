using AppLogic.Authentication.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Services;
using AppLogic.Registration.UseCases;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Flows
{
    /// <summary>
    /// Verifica el <b>orden</b> de las operaciones del alta de persona, no solo que ocurran.
    /// <para>
    /// El alta es la parte más frágil del sistema: dos transacciones de base con el alta de LDAP en
    /// el medio, porque LDAP no es transaccional. Las consecuencias de cada punto de fallo están
    /// documentadas en <c>CompleteNewPerson</c>, pero los tests con <c>Verify(Times.Once)</c> no
    /// prueban la secuencia: si alguien mueve LDAP antes del commit —que rompe la invariante— esos
    /// tests siguen pasando.
    /// </para>
    /// <para>
    /// Acá cada colaborador escribe en una bitácora compartida y se assertea la secuencia completa.
    /// </para>
    /// </summary>
    public class PersonRegistrationFlowTests
    {
        private const int NuevoCodigoPersona = 123;
        private const string Documento = "1234567-2";
        private const string Password = "NuevaPassword1!";

        private readonly List<string> _bitacora = [];
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPersonaRepository> _personaRepo = new();
        private readonly Mock<BusinessLogic.IDevartRepositories.ICiudadRepository> _ciudadRepo = new();
        private readonly Mock<IRegistroAdmisioneRepository> _registroAdmisionesRepo = new();
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock = new();
        private readonly Mock<ILdap> _ldapMock = new();

        public PersonRegistrationFlowTests()
        {
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.Personas).Returns(_personaRepo.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(_ciudadRepo.Object);
            _uowMock.Setup(u => u.RegistroAdmisiones).Returns(_registroAdmisionesRepo.Object);

            _uowMock.Setup(u => u.BeginTransaction()).Callback(() => _bitacora.Add("tx:begin"));
            _uowMock.Setup(u => u.Commit()).Callback(() => _bitacora.Add("tx:commit"));
            _uowMock.Setup(u => u.Rollback()).Callback(() => _bitacora.Add("tx:rollback"));
            _uowMock.Setup(u => u.Save()).Callback(() => _bitacora.Add("db:save"));
            _personaRepo.Setup(r => r.Add(It.IsAny<Persona>())).Callback(() => _bitacora.Add("db:persona-add"));
            _personaRepo.Setup(r => r.Update(It.IsAny<Persona>())).Callback(() => _bitacora.Add("db:persona-update"));
            _registroAdmisionesRepo
                .Setup(r => r.Add(It.IsAny<RegistroAdmisione>()))
                .Callback(() => _bitacora.Add("db:admision-add"));

            // Por defecto: persona nueva (no existe en el padrón) y ciudad válida.
            _personaRepo.Setup(r => r.GetByDocumento(Documento)).Returns(default(Persona)!);
            _ciudadRepo
                .Setup(r => r.GetByKey(1, 2, 3))
                .Returns(new Ciudad { CodigoPais = 1, CodigoEstado = 2, CodigoCiudad = 3, Nombre = "Montevideo" });
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA))
                .Returns(NuevoCodigoPersona);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES))
                .Returns(555);

            LdapCrearUsuario(exito: true);
            LdapForzarPassword(exito: true);
        }

        private void LdapCrearUsuario(bool exito) =>
            _ldapMock
                .Setup(l => l.CrearUsuarioAsync(It.IsAny<LdapService.DTOs.ParamCrearUsuarioLdap>()))
                .Callback(() => _bitacora.Add("ldap:crear-usuario"))
                .ReturnsAsync(exito
                    ? OperationResult<bool>.Ok(true, "CrearUsuarioAsync")
                    : OperationResult<bool>.IsFailed("LDAP_CREATE_01", "CrearUsuarioAsync", "No se pudo crear.", 500, false));

        private void LdapForzarPassword(bool exito) =>
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => _bitacora.Add("ldap:forzar-password"))
                .ReturnsAsync(exito
                    ? OperationResult<bool>.Ok(true, "ForzarCambiarPasswordAsync")
                    : OperationResult<bool>.IsFailed("LDAP_PASS_01", "ForzarCambiarPasswordAsync", "No se pudo.", 500, false));

        private CompleteNewPerson CrearCasoDeUso() =>
            new(_uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                new LdapUserDirectory(_ldapMock.Object),
                Mock.Of<ILogger<CompleteNewPerson>>());

        private static PendingPerson CrearPendiente() => new()
        {
            DocumentType = "CI",
            DocumentNumber = Documento,
            FirstName = "Ana",
            FirstSurname = "Perez",
            Email = "ana@example.com",
            BirthDate = new DateTime(2000, 1, 1),
            Sex = "F",
            Address = "Calle 1",
            PrimaryPhone = "+59899123456",
            CountryId = 1,
            StateId = 2,
            CityId = 3
        };

        [Fact]
        public async Task AltaFeliz_CommiteaLaPersonaAntesDeLlamarALdap()
        {
            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.True(result.Success);
            Assert.Equal(NuevoCodigoPersona, result.Data);

            // La invariante: el primer commit ocurre ANTES del primer contacto con LDAP.
            var primerCommit = _bitacora.IndexOf("tx:commit");
            var primerLdap = _bitacora.IndexOf("ldap:crear-usuario");
            Assert.True(primerCommit >= 0, "no hubo commit de la persona");
            Assert.True(primerLdap >= 0, "no se llamó a LDAP");
            Assert.True(
                primerCommit < primerLdap,
                $"LDAP se llamó antes de commitear la persona. Secuencia: {string.Join(" → ", _bitacora)}");
        }

        [Fact]
        public async Task AltaFeliz_SecuenciaCompletaEsLaEsperada()
        {
            await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.Equal(
                [
                    // Tx #1: crear la persona. El segundo save es para forzar
                    // FechaConfDatosPersona = null (Oracle la completa sola por DEFAULT sysdate).
                    "tx:begin", "db:persona-add", "db:save", "db:save", "tx:commit",
                    // LDAP fuera de transacción: no es transaccional, no se puede revertir.
                    "ldap:crear-usuario", "ldap:forzar-password",
                    // Tx #2: metadata de password + alta de admisión.
                    "tx:begin", "db:persona-update", "db:admision-add", "tx:commit"
                ],
                _bitacora);
        }

        [Fact]
        public async Task AltaFeliz_DejaFechaConfDatosPersonaEnNullParaForzarConfirmacion()
        {
            Persona? agregada = null;
            _personaRepo.Setup(r => r.Add(It.IsAny<Persona>()))
                .Callback<Persona>(p => { _bitacora.Add("db:persona-add"); agregada = p; });

            await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.NotNull(agregada);
            Assert.Null(agregada!.FechaConfDatosPersona);
        }

        [Fact]
        public async Task SiFallaLdapAlCrearUsuario_LaPersonaQuedaCommiteadaYNoSeRevierte()
        {
            LdapCrearUsuario(exito: false);

            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.False(result.Success);
            Assert.Equal("LDAP_CREATE_01", result.ErrorCode);
            Assert.Contains("tx:commit", _bitacora);
            Assert.DoesNotContain("tx:rollback", _bitacora);
            // No se avanza al paso siguiente ni se abre la Tx #2.
            Assert.DoesNotContain("ldap:forzar-password", _bitacora);
            Assert.DoesNotContain("db:admision-add", _bitacora);
        }

        [Fact]
        public async Task SiFallaLdapAlForzarPassword_LaPersonaYaExisteConUsuarioPeroSinTx2()
        {
            LdapForzarPassword(exito: false);

            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.False(result.Success);
            Assert.Equal("LDAP_PASS_01", result.ErrorCode);
            Assert.Contains("ldap:crear-usuario", _bitacora);
            Assert.DoesNotContain("tx:rollback", _bitacora);
            Assert.DoesNotContain("db:admision-add", _bitacora);
        }

        [Fact]
        public async Task SiFallaLaTx2_SeHaceRollbackPeroElUsuarioLdapYaQuedoActivo()
        {
            // Segundo BeginTransaction explota: la Tx #2 no puede persistir metadata ni admisión.
            var vecesBegin = 0;
            _uowMock.Setup(u => u.BeginTransaction()).Callback(() =>
            {
                _bitacora.Add("tx:begin");
                if (++vecesBegin == 2) throw new InvalidOperationException("fallo simulado en Tx #2");
            });

            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.False(result.Success);
            Assert.Equal("REG_PERSONA_99", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
            // El usuario LDAP ya está activo y NO se revierte: es el estado inconsistente asumido.
            Assert.Contains("ldap:crear-usuario", _bitacora);
            Assert.Contains("ldap:forzar-password", _bitacora);
            Assert.Contains("tx:rollback", _bitacora);
        }

        [Fact]
        public async Task ReintentoDeUnAltaCortada_NoRecreaLaPersonaYSoloCompletaLaPassword()
        {
            // Escenario real: un intento anterior creó la persona pero falló antes de dejarle
            // la contraseña. El reintento la encuentra por documento y solo completa LDAP.
            _personaRepo.Setup(r => r.GetByDocumento(Documento)).Returns(new Persona
            {
                CodigoPersona = NuevoCodigoPersona,
                TipoDocumento = "CI",
                Documento = Documento,
                Email = "ana@example.com",
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                PrimerNombreMay = "ANA",
                PrimerApellidoMay = "PEREZ",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            });

            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.True(result.Success);
            Assert.Equal(NuevoCodigoPersona, result.Data);
            Assert.DoesNotContain("db:persona-add", _bitacora);
            Assert.DoesNotContain("ldap:crear-usuario", _bitacora);
            Assert.Equal(
                ["ldap:forzar-password", "db:persona-update", "db:save"],
                _bitacora);
        }

        [Fact]
        public async Task SiElDocumentoYaEsDeOtraPersona_RechazaSinTocarNada()
        {
            _personaRepo.Setup(r => r.GetByDocumento(Documento)).Returns(new Persona
            {
                CodigoPersona = 999,
                TipoDocumento = "CI",
                Documento = Documento,
                Email = "otra@example.com",
                PrimerNombre = "Otra",
                PrimerApellido = "Persona",
                PrimerNombreMay = "OTRA",
                PrimerApellidoMay = "PERSONA",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            });

            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), Password);

            Assert.False(result.Success);
            Assert.Equal("REG_PERSONA_02", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            Assert.Empty(_bitacora);
        }

        [Fact]
        public async Task ConPasswordInvalida_NoAbreTransaccionNiLlamaALdap()
        {
            var result = await CrearCasoDeUso().ExecuteAsync(CrearPendiente(), "corta");

            Assert.False(result.Success);
            Assert.Equal("INI_PAS_02", result.ErrorCode);
            Assert.Empty(_bitacora);
        }
    }
}
