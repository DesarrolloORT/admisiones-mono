using AppLogic.Registration.Mapping;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;

namespace AppLogic.Registration.Services;

/// <summary>
/// Alta en T_REGISTRO_ADMISIONES. Cuelga o de la persona ya creada o de la solicitud de alta que
/// todavía no tiene persona asociada; nunca de las dos.
/// </summary>
public static class AdmissionRecords
{
    public static void AddForPerson(IUnitOfWork uow, IDbConnectionContext dbConnectionContext, long personId) =>
        Add(uow, dbConnectionContext, personId, null);

    public static void AddForRegistrationRequest(IUnitOfWork uow, IDbConnectionContext dbConnectionContext, long idSolicitudAlta) =>
        Add(uow, dbConnectionContext, null, idSolicitudAlta);

    private static void Add(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long? personId,
        long? idSolicitudAlta) =>
        uow.RegistroAdmisiones.Add(RegistrationMapper.CreateAdmissionRecord(
            dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES),
            personId,
            idSolicitudAlta));
}
