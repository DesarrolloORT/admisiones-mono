using AppLogic.Catalogos.Dtos;
using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogos.Mappers;

[ExcludeFromCodeCoverage]
public static class ComienzosMapper
{
    public static DtoComienzoResponse ToAdmisionesDto(
        this Proceso comienzo)
    {
        return new DtoComienzoResponse
        {
            IdProceso = comienzo.IdProceso,
            NombreProceso = comienzo.NombreProceso
        };
    }

    public static DtoComienzoResponse ToAdmisionesDto(
        this VdProcesosDisponibles1y2 comienzo)
    {
        return new DtoComienzoResponse
        {
            IdProceso = (long)comienzo.IdProceso,
            NombreProceso = comienzo.NombreProceso
        };
    }
}
