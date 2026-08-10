using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;

namespace AppLogic.Enrollments.Survey.Services;

internal static class InitialSurveyChildRecords
{
    internal static void ApplyChildLists(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        SaveInitialSurveyRequest request)
    {
        ApplyConsideredUniversities(uow, dbConnectionContext, personId, request);
        ApplyHigherEducation(uow, dbConnectionContext, personId, request);
        ApplyChoiceReasons(uow, personId, request);
        ApplyAdvertising(uow, personId, request);
    }

    private static void ApplyConsideredUniversities(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        SaveInitialSurveyRequest request)
    {
        if (request.ResearchedOtherUniversities == false)
        {
            uow.EmpresaConsideradaAdmisions.RemoveByPersona(personId);
            return;
        }

        if (request.ResearchedOtherUniversities == true
            && (request.ConsideredUniversityIds != null || HasOthers(request.ConsideredUniversityOthers)))
        {
            uow.EmpresaConsideradaAdmisions.RemoveByPersona(personId);
            foreach (var id in (request.ConsideredUniversityIds ?? []).Where(id => id > 0).Distinct())
            {
                uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                {
                    IdEmpresaConsiderada = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                    CodigoPersona = personId,
                    CodigoEmpresa = id
                });
            }

            foreach (var name in NormalizedOthers(request.ConsideredUniversityOthers))
            {
                uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                {
                    IdEmpresaConsiderada = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                    CodigoPersona = personId,
                    NombreOtraEmpresa = name
                });
            }
        }
    }

    private static void ApplyHigherEducation(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        SaveInitialSurveyRequest request)
    {
        if (request.PreviousHigherEducationId is InitialSurveyState.EstadoEducacionSuperiorPrevia.Exterior
            or InitialSurveyState.EstadoEducacionSuperiorPrevia.Ninguna)
        {
            uow.EducacionSuperiorAdmisions.RemoveByPersona(personId);
            return;
        }

        if (request.PreviousHigherEducationId == InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay
            && (request.HigherEducationUniversityIds != null || HasOthers(request.HigherEducationUniversityOthers)))
        {
            uow.EducacionSuperiorAdmisions.RemoveByPersona(personId);
            foreach (var id in (request.HigherEducationUniversityIds ?? []).Where(id => id > 0).Distinct())
            {
                uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                {
                    IdEducacionSuperior = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                    CodigoPersona = personId,
                    CodigoEmpresa = id
                });
            }

            foreach (var name in NormalizedOthers(request.HigherEducationUniversityOthers))
            {
                uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                {
                    IdEducacionSuperior = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                    CodigoPersona = personId,
                    NombreOtraEmpresa = name
                });
            }
        }
    }

    private static void ApplyChoiceReasons(
        IUnitOfWork uow,
        long personId,
        SaveInitialSurveyRequest request)
    {
        if (request.OrtChoiceReasonIds == null)
            return;

        uow.MotivoEleccionAdmisions.RemoveByPersona(personId);
        foreach (var id in request.OrtChoiceReasonIds.Distinct())
        {
            uow.MotivoEleccionAdmisions.Add(new MotivoEleccionAdmision
            {
                IdMotivo = id,
                CodigoPersona = personId
            });
        }
    }

    private static void ApplyAdvertising(
        IUnitOfWork uow,
        long personId,
        SaveInitialSurveyRequest request)
    {
        if (request.RecallsOrtAdvertising == false)
        {
            uow.PublicidadEleccionAdmisions.RemoveByPersona(personId);
            return;
        }

        if (request.RecallsOrtAdvertising == true && request.OrtAdvertisingIds != null)
        {
            uow.PublicidadEleccionAdmisions.RemoveByPersona(personId);
            foreach (var id in request.OrtAdvertisingIds.Distinct())
            {
                uow.PublicidadEleccionAdmisions.Add(new PublicidadEleccionAdmision
                {
                    IdPublicidad = id,
                    CodigoPersona = personId
                });
            }
        }
    }

    private static IEnumerable<string> NormalizedOthers(IEnumerable<string>? otros)
    {
        return otros?
            .Select(o => o?.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    private static bool HasOthers(IEnumerable<string>? otros)
        => otros?.Any(o => !string.IsNullOrWhiteSpace(o)) == true;
}
