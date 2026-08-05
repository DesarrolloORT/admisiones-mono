using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

public static class BankMapper
{
    public static BankResponse ToResponse(this Banco bank) => new()
    {
        Id = bank.IdBanco,
        Name = bank.NombreBanco,
        Code = bank.CodigoBanco,
        SistarbancBankId = bank.IdBancoSistarbanc
    };
}
