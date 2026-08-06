using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogs.Mapping;

[ExcludeFromCodeCoverage]
public static class IntakeMapper
{
    public static IntakeResponse ToResponse(
        this Proceso intake)
    {
        return new IntakeResponse
        {
            AdmissionProcessId = intake.IdProceso,
            AdmissionProcessName = intake.NombreProceso
        };
    }

    public static IntakeResponse ToResponse(
        this VdProcesosDisponibles1y2 intake)
    {
        return new IntakeResponse
        {
            AdmissionProcessId = (long)intake.IdProceso,
            AdmissionProcessName = intake.NombreProceso
        };
    }
}
