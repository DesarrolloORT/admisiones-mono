using AppLogic.Dtos.EncuestaInicial;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;

namespace AppLogic.Services.Inscripciones.Encuesta
{
    internal static class EncuestaInicialChildTablesService
    {
        internal static void AplicarListasHijas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            AplicarUniversidadesConsideradas(uow, dbConnectionContext, codigoPersona, request);
            AplicarEducacionSuperior(uow, dbConnectionContext, codigoPersona, request);
            AplicarMotivosEleccion(uow, codigoPersona, request);
            AplicarPublicidad(uow, codigoPersona, request);
        }

        private static void AplicarUniversidadesConsideradas(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.SeInformoEnOtrasUniversidades == false)
            {
                uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
                return;
            }

            if (request.SeInformoEnOtrasUniversidades == true && request.UniversidadConsideradaIds != null)
            {
                uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
                foreach (var id in request.UniversidadConsideradaIds.Distinct())
                {
                    uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                    {
                        IdEmpresaConsiderada = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                        CodigoPersona = codigoPersona,
                        CodigoEmpresa = id
                    });
                }
            }
        }

        private static void AplicarEducacionSuperior(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.EstadoEducacionSuperiorPreviaId is 2 or 3)
            {
                uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
                return;
            }

            if (request.EstadoEducacionSuperiorPreviaId == 1 && request.UniversidadEducacionSuperiorIds != null)
            {
                uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
                foreach (var id in request.UniversidadEducacionSuperiorIds.Distinct())
                {
                    uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                    {
                        IdEducacionSuperior = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                        CodigoPersona = codigoPersona,
                        CodigoEmpresa = id
                    });
                }
            }
        }

        private static void AplicarMotivosEleccion(
            IUnitOfWork uow,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.MotivoEleccionOrtIds == null)
                return;

            uow.MotivoEleccionAdmisions.RemoveByPersona(codigoPersona);
            foreach (var id in request.MotivoEleccionOrtIds.Distinct())
            {
                uow.MotivoEleccionAdmisions.Add(new MotivoEleccionAdmision
                {
                    IdMotivo = id,
                    CodigoPersona = codigoPersona
                });
            }
        }

        private static void AplicarPublicidad(
            IUnitOfWork uow,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (request.RecuerdaPublicidadOrt == false)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
                return;
            }

            if (request.RecuerdaPublicidadOrt == true && request.PublicidadOrtIds != null)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var id in request.PublicidadOrtIds.Distinct())
                {
                    uow.PublicidadEleccionAdmisions.Add(new PublicidadEleccionAdmision
                    {
                        IdPublicidad = id,
                        CodigoPersona = codigoPersona
                    });
                }
            }
        }
    }
}
