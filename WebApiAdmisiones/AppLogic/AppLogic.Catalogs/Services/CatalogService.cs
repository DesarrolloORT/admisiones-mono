using AppLogic.Catalogs.Dtos;
using AppLogic.Catalogs.Interfaces;
using AppLogic.Catalogs.Mapping;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Catalogs.Services;

public class CatalogService(
    IUnitOfWorkFactory uowFactory,
    IEnrollmentsAndPaymentsApiClient enrollmentsAndPaymentsApiClient) : ICatalogService
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IEnrollmentsAndPaymentsApiClient _enrollmentsAndPaymentsApiClient = enrollmentsAndPaymentsApiClient;

    public Task<OperationResult<IEnumerable<CountryStateCityResponse>>> GetCountriesStatesCitiesAsync()
    {
        using var uow = _uowFactory.Create();

        var response = uow.Paises.GetPaisesConEstadosYCiudades()
            .Select(LocationMapper.ToResponse)
            .ToList();

        return Task.FromResult(OperationResult<IEnumerable<CountryStateCityResponse>>.Ok(
            response,
            nameof(GetCountriesStatesCitiesAsync)));
    }

    public Task<OperationResult<InitialSurveyCatalogsResponse>> GetInitialSurveyCatalogsAsync()
    {
        using var uow = _uowFactory.Create();

        var response = InitialSurveyCatalogMapper.ToResponse(
            InitialSurveyCatalogMapper.ToUniversities(uow.Empresas.GetUniversidades()),
            InitialSurveyCatalogMapper.ToHighSchoolYears(uow.AnioBachillers.GetAllWithRelated()),
            InitialSurveyCatalogMapper.ToOrtChoiceReasons(uow.MotivoOpcionesAdmisions.GetAll()),
            InitialSurveyCatalogMapper.ToOrtAdvertisements(uow.PublicidadOpcionesAdmisions.GetAll()));

        return Task.FromResult(OperationResult<InitialSurveyCatalogsResponse>.Ok(
            response,
            nameof(GetInitialSurveyCatalogsAsync)));
    }

    public OperationResult<IEnumerable<DegreeProgramsByLevelResponse>> GetDegreePrograms(
        long personId,
        AcademicOffer academicOffer)
    {
        if (!Enum.IsDefined(academicOffer))
        {
            return OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>.IsFailed(
                "CAT_CARRERAS_01",
                nameof(GetDegreePrograms),
                "Propuesta académica no válida.",
                400,
                default);
        }

        using var uow = _uowFactory.Create();

        var rows = academicOffer switch
        {
            AcademicOffer.UniversityDegree =>
                uow.VdProductosDisponibles1y2s.GetProductosDisponibles(personId, idNivelProducto: 1)
                    .Select(DegreeProgramMapper.ToRow),
            AcademicOffer.TechnicalDegree =>
                uow.VdProductosDisponibles1y2s.GetProductosDisponibles(personId, idNivelProducto: 2)
                    .Select(DegreeProgramMapper.ToRow),
            _ =>
                uow.VdOfertasDisponibles3y4s.GetProductosDisponibles()
                    .Select(DegreeProgramMapper.ToRow)
        };

        var response = DegreeProgramMapper.ToGroupedResponse(
            rows,
            groupBySeminar: academicOffer == AcademicOffer.ProfessionalUpdate);

        return OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>.Ok(response, nameof(GetDegreePrograms));
    }

    public OperationResult<IEnumerable<IntakeResponse>> GetIntakes(long degreeProgramId)
    {
        using var uow = _uowFactory.Create();
        var admissionProcesses = uow.VdProcesosDisponibles1y2s.GetProcesosDisponibles(degreeProgramId);
        return OperationResult<IEnumerable<IntakeResponse>>.Ok(
            admissionProcesses.Select(IntakeMapper.ToResponse),
            nameof(GetIntakes));
    }

    public async Task<OperationResult<List<OfferingResponse>>> GetShifts(long personId, long degreeProgramId, long admissionProcessId)
    {
        using var uow = _uowFactory.Create();
        var product = uow.Productos.GetByKey(degreeProgramId);

        if (product == null)
        {
            return OperationResult<List<OfferingResponse>>.IsFailed(
                "CAT_TURNOS_02",
                nameof(GetShifts),
                "Producto no encontrado.",
                404,
                default);
        }

        if (IsProductLevel3Or4(product.IdNivelProducto))
        {
            var offerings = uow.VdOfertasDisponibles3y4s
                .GetOfertasDisponibles(degreeProgramId, personId)
                .GroupBy(o => new { o.IdOferta, o.IdTurno })
                .Select(g => g.First())
                .OrderBy(o => o.IdTurno)
                .ThenBy(o => o.IdOferta)
                .ToList();

            var shiftsById = uow.Turnos
                .GetByKeys(offerings.Select(o => o.IdTurno))
                .ToDictionary(t => t.IdTurno);

            var response = offerings
                .Select(offering =>
                {
                    shiftsById.TryGetValue(offering.IdTurno, out var turno);
                    return offering.ToResponse(turno);
                })
                .ToList();

            return OperationResult<List<OfferingResponse>>.Ok(response, nameof(GetShifts));
        }

        if (!IsProductLevel1Or2(product.IdNivelProducto))
        {
            return OperationResult<List<OfferingResponse>>.IsFailed(
                "CAT_TURNOS_04",
                nameof(GetShifts),
                "Nivel de producto no soportado para obtener turnos.",
                400,
                default);
        }

        var offeringsResult = await _enrollmentsAndPaymentsApiClient
            .GetOfferingsForEnrollmentWithProcessAsync(degreeProgramId, admissionProcessId);

        if (!offeringsResult.Success)
        {
            return OperationResult<List<OfferingResponse>>.IsFailed(
                offeringsResult.ErrorCode,
                nameof(GetShifts),
                offeringsResult.Message,
                offeringsResult.HttpCode,
                offeringsResult.Data?.Select(OfferingMapper.ToResponse).ToList());
        }

        return OperationResult<List<OfferingResponse>>.Ok(
            offeringsResult.Data!.Select(OfferingMapper.ToResponse).ToList(),
            nameof(GetShifts));
    }

    public Task<OperationResult<IEnumerable<BankResponse>>> GetBanksAsync()
    {
        using var uow = _uowFactory.Create();
        var banks = uow.Bancos.GetAllHabilitados();
        return Task.FromResult(OperationResult<IEnumerable<BankResponse>>.Ok(
            banks.Select(BankMapper.ToResponse).ToList(),
            nameof(GetBanksAsync)));
    }

    public OperationResult<IEnumerable<InstitutionResponse>> GetInstitutions(long countryId, long stateId)
    {
        using var uow = _uowFactory.Create();
        var institutions = uow.Empresas.GetInstituciones(countryId, stateId);
        return OperationResult<IEnumerable<InstitutionResponse>>.Ok(
            institutions.Select(InstitutionMapper.ToResponse).ToList(),
            nameof(GetInstitutions));
    }

    // Agrupación de IdNivelProducto ya usada en el resto del código para separar fuentes de datos
    // (VdOfertasDisponibles3y4 / VdProductosDisponibles1y2).
    private static bool IsProductLevel3Or4(long productLevelId) => productLevelId is 3 or 4;

    private static bool IsProductLevel1Or2(long productLevelId) => productLevelId is 1 or 2;
}
