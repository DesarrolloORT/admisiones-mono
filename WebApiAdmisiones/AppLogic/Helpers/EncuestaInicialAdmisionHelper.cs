using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Services.Personas;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using System.Security.Cryptography;
using System.Text;

namespace AppLogic.Helpers
{
    internal static class EncuestaInicialAdmisionHelper
    {
        internal const string EstadoTemporal = "TEMPORAL";
        internal const string EstadoDefinitivo = "DEFINITIVO";

        internal static bool TieneDerechoAEncuestaInicial(string tipoDocumento, string documento, IUnitOfWork uow)
        {
            if (uow.VdEsFrescoAdmisions.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaInis.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaIniAdmisions.ExisteCompletaPorDocumento(tipoDocumento, documento))
                return false;

            return true;
        }

        internal static DtoEncuestaInicialAdmisionResponse CrearRespuestaEncuestaInicial(
            IUnitOfWork uow,
            long codigoPersona,
            EncuestaIniAdmision encuesta)
        {
            return new DtoEncuestaInicialAdmisionResponse
            {
                TieneDerechoEncuesta = true,
                Encuesta = encuesta.ToDtoWithRelated(1),
                UniversidadesConsideradas = uow.EmpresaConsideradaAdmisions.GetByPersona(codigoPersona).ToDtos(),
                UniversidadesEducacionSuperior = uow.EducacionSuperiorAdmisions.GetByPersona(codigoPersona).ToDtos(),
                OpcionesMotivosSeleccionados = uow.MotivoEleccionAdmisions.GetByPersona(codigoPersona).ToDtos(),
                OpcionesPublicidadSeleccionadas = uow.PublicidadEleccionAdmisions.GetByPersona(codigoPersona).ToDtos()
            };
        }

        internal static EncuestaIniAdmision? ObtenerEncuestaParaGuardar(
            IUnitOfWork uow,
            long codigoPersona,
            GuardarEncuestaInicialRequest request)
        {
            if (request.IdProducto.HasValue && request.IdProceso.HasValue)
            {
                var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(
                    request.IdProducto.Value,
                    request.IdProceso.Value);
                if (idComienzo.HasValue && idComienzo.Value > 0)
                {
                    var encuestaPorProductoComienzo = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(
                        codigoPersona,
                        request.IdProducto.Value,
                        idComienzo.Value);
                    if (encuestaPorProductoComienzo != null)
                    {
                        return encuestaPorProductoComienzo;
                    }
                }
            }

            return uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
        }

        internal static EncuestaIniAdmision CrearEncuestaInicial(
            IDbConnectionContext dbConnectionContext,
            Persona persona,
            long codigoPersona)
        {
            var ahora = DateTime.Now;
            return new EncuestaIniAdmision
            {
                IdEncuestaIni = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                FechaEncuestaIni = ahora,
                TipoDocumento = persona.TipoDocumento,
                Documento = persona.Documento,
                CodigoPersona = codigoPersona,
                TipoInscripcion = "SOLO_ENCUESTA_INI",
                NuevaversionEncuestaIni = CommonConstants.Booleanos.Si,
                EstadoEncuestaIniAdmision = EstadoTemporal,
                UsuarioIngreso = string.Empty,
                FechaIngreso = ahora,
                HoraIngreso = ahora.ToString("HH:mm:ss")
            };
        }

        internal static void AplicarRequestAEncuesta(
            EncuestaIniAdmision encuesta,
            GuardarEncuestaInicialRequest request,
            Persona persona,
            long? idComienzo)
        {
            AplicarIdentificadores(encuesta, request, persona, idComienzo);
            AplicarFormacion(encuesta, request);
            AplicarInfoOtras(encuesta, request);
            AplicarValoraciones(encuesta, request);
            AplicarPublicidadEInstruccionOrt(encuesta, request);
        }

        private static void AplicarIdentificadores(
            EncuestaIniAdmision encuesta,
            GuardarEncuestaInicialRequest request,
            Persona persona,
            long? idComienzo)
        {
            if (request.IdProducto.HasValue)
            {
                encuesta.IdProducto = request.IdProducto.Value;
            }

            if (idComienzo.HasValue)
            {
                encuesta.IdComienzo = idComienzo.Value;
            }

            if (request.IdProceso.HasValue)
            {
                encuesta.IdProceso = request.IdProceso.Value;
            }

            if (encuesta.IdProducto.HasValue)
            {
                encuesta.ClaveEncuestaIni = GenerarClaveEncuesta(encuesta.IdProducto.Value, persona.Documento);
            }
        }

        private static void AplicarFormacion(EncuestaIniAdmision encuesta, GuardarEncuestaInicialRequest request)
        {
            if (request.NombreInstitucion != null)
            {
                encuesta.NombreInstSecEncuestaIni = DocumentUtils.NormalizarOpcional(request.NombreInstitucion);
            }

            if (request.CodigoTitulo.HasValue)
            {
                encuesta.CodigoTitulo = request.CodigoTitulo.Value;
            }

            if (request.UltimoAnioSexto.HasValue)
            {
                encuesta.UltimoAnioSextoEncuestaIni = request.UltimoAnioSexto.Value.ToString();
            }

            if (request.VecesSextoBool.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.VecesSextoBool.Value
                    ? request.VecesSexto?.ToString()
                    : null;
            }
            else if (request.VecesSexto.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.VecesSexto.Value.ToString();
            }

            if (request.InstruccionPadre.HasValue)
            {
                encuesta.InstruccionPadreEncuestaIni = request.InstruccionPadre.Value.ToString();
            }

            if (request.InstruccionMadre.HasValue)
            {
                encuesta.InstruccionMadreEncuestaIni = request.InstruccionMadre.Value.ToString();
            }

            if (request.DecisionCarrera.HasValue)
            {
                encuesta.DecisionCarreraEncuestaIni = request.DecisionCarrera.Value.ToString();
            }

            if (request.DecisionUniversidad.HasValue)
            {
                encuesta.DecisionUniverEncuestaIni = request.DecisionUniversidad.Value.ToString();
            }
        }

        private static void AplicarInfoOtras(EncuestaIniAdmision encuesta, GuardarEncuestaInicialRequest request)
        {
            if (request.InfoOtrasUniversidadesAntes != null)
            {
                encuesta.InforOtrasAntesEncuestaIni = DocumentUtils.NormalizarSiNo(request.InfoOtrasUniversidadesAntes);
            }

            if (request.InfoOtrasLinea1 != null)
            {
                encuesta.InforOtrasLinea1Ini = DocumentUtils.NormalizarOpcional(request.InfoOtrasLinea1);
            }

            if (request.InfoOtrasLinea2 != null)
            {
                encuesta.InforOtrasLinea2Ini = DocumentUtils.NormalizarOpcional(request.InfoOtrasLinea2);
            }

            if (request.CompartidoCon.HasValue)
            {
                CargarConQuienCompartioDecision(encuesta, request.CompartidoCon.Value);
            }

            if (request.CodigoInstitucionBac.HasValue)
            {
                encuesta.CodigoInstitucionBac = request.CodigoInstitucionBac.Value;
            }

            if (request.InformarEncuesta != null)
            {
                encuesta.InformarEncuestaIni = DocumentUtils.NormalizarSiNo(request.InformarEncuesta);
            }

            if (request.UltimoAnioSecundaria.HasValue)
            {
                encuesta.UltimoanioSecundariaEncuestaIni = request.UltimoAnioSecundaria.Value == PersonaConstants.Parametros.UruguayCodigoPais;
            }

            if (request.TieneEducacionSuperior.HasValue)
            {
                encuesta.TieneEducacionSuperiorEncuestaIni = ConvertirBoolASiNo(request.TieneEducacionSuperior.Value);
            }

            if (request.NivelDecision.HasValue)
            {
                encuesta.NivelDecisionEncuestaIni = request.NivelDecision.Value == 1;
            }
        }

        private static void AplicarValoraciones(EncuestaIniAdmision encuesta, GuardarEncuestaInicialRequest request)
        {
            if (request.AsesoramientoOrt.HasValue)
            {
                encuesta.AsesoramientoOrtEncuestaIni = ConvertirBoolASiNo(request.AsesoramientoOrt.Value);
            }

            if (request.ValoracionAsesoramientoOrt.HasValue && request.AsesoramientoOrt == true)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = request.ValoracionAsesoramientoOrt.Value > 3;
            }
            else if (request.AsesoramientoOrt == false)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = null;
            }

            if (request.VistaSitioWebOrt.HasValue)
            {
                encuesta.VistaSitioWebOrtEncuestaIni = ConvertirBoolASiNo(request.VistaSitioWebOrt.Value);
            }

            if (request.ValoracionSitioWeb.HasValue && request.VistaSitioWebOrt == true)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = request.ValoracionSitioWeb.Value > 3;
            }
            else if (request.VistaSitioWebOrt == false)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = null;
            }

            if (request.VistaInstalacionesOrt.HasValue)
            {
                encuesta.VistaInstalacionesOrtEncuestaIni = ConvertirBoolASiNo(request.VistaInstalacionesOrt.Value);
            }

            if (request.ValoracionInstalacionesOrt.HasValue && request.VistaInstalacionesOrt == true)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = request.ValoracionInstalacionesOrt.Value > 3;
            }
            else if (request.VistaInstalacionesOrt == false)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = null;
            }
        }

        private static void AplicarPublicidadEInstruccionOrt(EncuestaIniAdmision encuesta, GuardarEncuestaInicialRequest request)
        {
            if (request.PublicidadOrt.HasValue)
            {
                encuesta.PublicidadOrtEncuestaIni = ConvertirBoolASiNo(request.PublicidadOrt.Value);
            }

            if (request.InstruccionMadreOrt.HasValue)
            {
                encuesta.InstruccionMadreOrtEncuestaIni = ConvertirBoolASiNo(request.InstruccionMadreOrt.Value);
            }

            if (request.InstruccionPadreOrt.HasValue)
            {
                encuesta.InstruccionPadreOrtEncuestaIni = ConvertirBoolASiNo(request.InstruccionPadreOrt.Value);
            }
        }

        internal static void AplicarListasHijas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            GuardarEncuestaInicialRequest request)
        {
            if (string.Equals(DocumentUtils.NormalizarSiNo(request.InfoOtrasUniversidadesAntes), CommonConstants.Booleanos.No, StringComparison.OrdinalIgnoreCase))
            {
                uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadesConsideradas != null)
            {
                ReemplazarUniversidadesConsideradas(uow, dbConnectionContext, codigoPersona, request.UniversidadesConsideradas);
            }

            if (request.TieneEducacionSuperior == false)
            {
                uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadesEducacionSuperior != null)
            {
                ReemplazarEducacionSuperior(uow, dbConnectionContext, codigoPersona, request.UniversidadesEducacionSuperior);
            }

            if (request.OpcionesMotivosSeleccionados != null)
            {
                uow.MotivoEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var motivo in request.OpcionesMotivosSeleccionados.DistinctBy(m => m.IdMotivo))
                {
                    uow.MotivoEleccionAdmisions.Add(new MotivoEleccionAdmision
                    {
                        IdMotivo = motivo.IdMotivo,
                        CodigoPersona = codigoPersona
                    });
                }
            }

            if (request.PublicidadOrt == false)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.OpcionesPublicidadSeleccionadas != null)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var publicidad in request.OpcionesPublicidadSeleccionadas.DistinctBy(p => p.IdPublicidad))
                {
                    uow.PublicidadEleccionAdmisions.Add(new PublicidadEleccionAdmision
                    {
                        IdPublicidad = publicidad.IdPublicidad,
                        CodigoPersona = codigoPersona
                    });
                }
            }
        }

        private static void ReemplazarUniversidadesConsideradas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            IEnumerable<EncuestaEmpresaRequest> universidades)
        {
            uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidad in universidades)
            {
                uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                {
                    IdEmpresaConsiderada = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidad.CodigoEmpresa == 0 ? null : universidad.CodigoEmpresa,
                    NombreOtraEmpresa = universidad.CodigoEmpresa == 0 ? DocumentUtils.NormalizarOpcional(universidad.Nombre) : null
                });
            }
        }

        private static void ReemplazarEducacionSuperior(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            IEnumerable<EncuestaEmpresaRequest> universidades)
        {
            uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidad in universidades)
            {
                uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                {
                    IdEducacionSuperior = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidad.CodigoEmpresa == 0 ? null : universidad.CodigoEmpresa,
                    NombreOtraEmpresa = universidad.CodigoEmpresa == 0 ? DocumentUtils.NormalizarOpcional(universidad.Nombre) : null
                });
            }
        }

        private static string ConvertirBoolASiNo(bool valor)
        {
            return valor ? CommonConstants.Booleanos.Si : CommonConstants.Booleanos.No;
        }

        private static string GenerarClaveEncuesta(long idProducto, string? documento)
        {
            var input = $"{idProducto}/{DocumentUtils.NormalizarMayusculas(documento)}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..30];
        }

        private static void CargarConQuienCompartioDecision(EncuestaIniAdmision encuesta, int valor)
        {
            encuesta.ComparPadresEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Padres);
            encuesta.ComparOtrosEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Otros);
            encuesta.ComparAmigoFamEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.AmigoFamilia);
            encuesta.ComparNadieEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Nadie);
            encuesta.ComparAmigoPropEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.AmigoPropio);
        }
    }
}
