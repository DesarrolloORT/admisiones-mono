using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Constants;
using AppLogic.DevartDTOs;
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
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.CarreraId.HasValue && request.ComienzoId.HasValue)
            {
                var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(
                    request.CarreraId.Value,
                    request.ComienzoId.Value);
                if (idComienzo.HasValue && idComienzo.Value > 0)
                {
                    var encuestaPorProductoComienzo = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(
                        codigoPersona,
                        request.CarreraId.Value,
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
            DtoGuardarEncuestaInicialRequest request,
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
            DtoGuardarEncuestaInicialRequest request,
            Persona persona,
            long? idComienzo)
        {
            if (request.CarreraId.HasValue)
            {
                encuesta.IdProducto = request.CarreraId.Value;
            }

            if (idComienzo.HasValue)
            {
                encuesta.IdComienzo = idComienzo.Value;
            }

            if (request.ComienzoId.HasValue)
            {
                encuesta.IdProceso = request.ComienzoId.Value;
            }

            if (encuesta.IdProducto.HasValue)
            {
                encuesta.ClaveEncuestaIni = GenerarClaveEncuesta(encuesta.IdProducto.Value, persona.Documento);
            }
        }

        private static void AplicarFormacion(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
        {
            if (request.NombreInstitucionSecundaria != null)
            {
                encuesta.NombreInstSecEncuestaIni = DocumentUtils.NormalizarOpcional(request.NombreInstitucionSecundaria);
            }

            if (request.OrientacionBachilleratoId.HasValue)
            {
                encuesta.CodigoTitulo = request.OrientacionBachilleratoId.Value;
            }

            if (request.AnioBachillerato.HasValue)
            {
                encuesta.UltimoAnioSextoEncuestaIni = request.AnioBachillerato.Value.ToString();
            }

            if (request.RecursaAnioBachillerato.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.RecursaAnioBachillerato.Value
                    ? request.VecesRecursaAnioBachillerato?.ToString()
                    : null;
            }
            else if (request.VecesRecursaAnioBachillerato.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.VecesRecursaAnioBachillerato.Value.ToString();
            }

            if (request.NivelFormacionPadreTutorId.HasValue)
            {
                encuesta.InstruccionPadreEncuestaIni = request.NivelFormacionPadreTutorId.Value.ToString();
            }

            if (request.NivelFormacionMadreTutorId.HasValue)
            {
                encuesta.InstruccionMadreEncuestaIni = request.NivelFormacionMadreTutorId.Value.ToString();
            }

            if (request.AnioDecisionCarreraId.HasValue)
            {
                encuesta.DecisionCarreraEncuestaIni = request.AnioDecisionCarreraId.Value.ToString();
            }

            if (request.AnioDecisionOrtId.HasValue)
            {
                encuesta.DecisionUniverEncuestaIni = request.AnioDecisionOrtId.Value.ToString();
            }
        }

        private static void AplicarInfoOtras(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
        {
            if (request.SeInformoEnOtrasUniversidades.HasValue)
            {
                encuesta.InforOtrasAntesEncuestaIni = ConvertirBoolASiNo(request.SeInformoEnOtrasUniversidades.Value);
            }

            if (request.InformacionOtrasUniversidadesLinea1 != null)
            {
                encuesta.InforOtrasLinea1Ini = DocumentUtils.NormalizarOpcional(request.InformacionOtrasUniversidadesLinea1);
            }

            if (request.InformacionOtrasUniversidadesLinea2 != null)
            {
                encuesta.InforOtrasLinea2Ini = DocumentUtils.NormalizarOpcional(request.InformacionOtrasUniversidadesLinea2);
            }

            if (request.ApoyoDecisionId.HasValue)
            {
                CargarConQuienCompartioDecision(encuesta, request.ApoyoDecisionId.Value);
            }

            if (request.InstitucionSecundariaId.HasValue)
            {
                encuesta.CodigoInstitucionBac = request.InstitucionSecundariaId.Value;
            }

            if (request.AutorizaInformarEncuesta.HasValue)
            {
                encuesta.InformarEncuestaIni = ConvertirBoolASiNo(request.AutorizaInformarEncuesta.Value);
            }

            if (request.UbicacionUltimoAnioSecundariaId.HasValue)
            {
                encuesta.UltimoanioSecundariaEncuestaIni = request.UbicacionUltimoAnioSecundariaId.Value == PersonaConstants.Parametros.UruguayCodigoPais;
            }

            if (request.EstadoEducacionSuperiorPreviaId.HasValue)
            {
                encuesta.TieneEducacionSuperiorEncuestaIni =
                    ConvertirBoolASiNo(request.EstadoEducacionSuperiorPreviaId.Value is 1 or 2);
            }

            if (request.NivelDecisionId.HasValue)
            {
                encuesta.NivelDecisionEncuestaIni = request.NivelDecisionId.Value == 1;
            }
        }

        private static void AplicarValoraciones(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
        {
            if (request.TuvoAsesoramientoOrt.HasValue)
            {
                encuesta.AsesoramientoOrtEncuestaIni = ConvertirBoolASiNo(request.TuvoAsesoramientoOrt.Value);
            }

            if (request.ValoracionAsesoramientoOrtId.HasValue && request.TuvoAsesoramientoOrt == true)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = request.ValoracionAsesoramientoOrtId.Value > 3;
            }
            else if (request.TuvoAsesoramientoOrt == false)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = null;
            }

            if (request.VisitoSitioWebOrt.HasValue)
            {
                encuesta.VistaSitioWebOrtEncuestaIni = ConvertirBoolASiNo(request.VisitoSitioWebOrt.Value);
            }

            if (request.ValoracionSitioWebOrtId.HasValue && request.VisitoSitioWebOrt == true)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = request.ValoracionSitioWebOrtId.Value > 3;
            }
            else if (request.VisitoSitioWebOrt == false)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = null;
            }

            if (request.VisitoInstalacionesOrt.HasValue)
            {
                encuesta.VistaInstalacionesOrtEncuestaIni = ConvertirBoolASiNo(request.VisitoInstalacionesOrt.Value);
            }

            if (request.ValoracionInstalacionesOrtId.HasValue && request.VisitoInstalacionesOrt == true)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = request.ValoracionInstalacionesOrtId.Value > 3;
            }
            else if (request.VisitoInstalacionesOrt == false)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = null;
            }
        }

        private static void AplicarPublicidadEInstruccionOrt(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
        {
            if (request.RecuerdaPublicidadOrt.HasValue)
            {
                encuesta.PublicidadOrtEncuestaIni = ConvertirBoolASiNo(request.RecuerdaPublicidadOrt.Value);
            }

            if (request.MadreTutorEgresadoOrt.HasValue)
            {
                encuesta.InstruccionMadreOrtEncuestaIni = ConvertirBoolASiNo(request.MadreTutorEgresadoOrt.Value);
            }

            if (request.PadreTutorEgresadoOrt.HasValue)
            {
                encuesta.InstruccionPadreOrtEncuestaIni = ConvertirBoolASiNo(request.PadreTutorEgresadoOrt.Value);
            }
        }

        internal static void AplicarListasHijas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.SeInformoEnOtrasUniversidades == false)
            {
                uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadConsideradaIds != null)
            {
                ReemplazarUniversidadesConsideradas(uow, dbConnectionContext, codigoPersona, request.UniversidadConsideradaIds);
            }

            if (request.EstadoEducacionSuperiorPreviaId == 3)
            {
                uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadEducacionSuperiorIds != null)
            {
                ReemplazarEducacionSuperior(uow, dbConnectionContext, codigoPersona, request.UniversidadEducacionSuperiorIds);
            }

            if (request.MotivoEleccionOrtIds != null)
            {
                uow.MotivoEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var motivoId in request.MotivoEleccionOrtIds.Distinct())
                {
                    uow.MotivoEleccionAdmisions.Add(new MotivoEleccionAdmision
                    {
                        IdMotivo = motivoId,
                        CodigoPersona = codigoPersona
                    });
                }
            }

            if (request.RecuerdaPublicidadOrt == false)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.PublicidadOrtIds != null)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var publicidadId in request.PublicidadOrtIds.Distinct())
                {
                    uow.PublicidadEleccionAdmisions.Add(new PublicidadEleccionAdmision
                    {
                        IdPublicidad = publicidadId,
                        CodigoPersona = codigoPersona
                    });
                }
            }
        }

        private static void ReemplazarUniversidadesConsideradas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            IEnumerable<long> universidades)
        {
            uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidadId in universidades)
            {
                uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                {
                    IdEmpresaConsiderada = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidadId
                });
            }
        }

        private static void ReemplazarEducacionSuperior(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            IEnumerable<long> universidades)
        {
            uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidadId in universidades)
            {
                uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                {
                    IdEducacionSuperior = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidadId
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

