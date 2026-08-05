using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

public static class InstitutionMapper
{
    public static InstitutionResponse ToResponse(this Empresa empresa) => new()
    {
        Id = empresa.CodigoEmpresa,
        Name = empresa.Nombre
    };
}
