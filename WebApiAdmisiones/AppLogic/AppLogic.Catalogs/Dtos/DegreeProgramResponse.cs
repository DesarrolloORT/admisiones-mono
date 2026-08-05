using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Catalogs.Dtos;

/// <summary>Carreras de un nivel de producto, agrupadas por escuela.</summary>
[ExcludeFromCodeCoverage]
public class DegreeProgramsByLevelResponse
{
    /// <summary>Código del nivel de producto.</summary>
    public long ProductLevelId { get; set; }

    /// <summary>Nombre del nivel de producto.</summary>
    public string? ProductLevelName { get; set; }

    /// <summary>Escuelas que dictan carreras de este nivel.</summary>
    public List<DegreeProgramsBySchoolResponse> Schools { get; set; } = [];
}

/// <summary>Carreras de una escuela. La forma cambia según el nivel de producto.</summary>
[ExcludeFromCodeCoverage]
public class DegreeProgramsBySchoolResponse
{
    /// <summary>Código de la escuela.</summary>
    public long SchoolId { get; set; }

    /// <summary>Nombre de la escuela.</summary>
    public string? SchoolName { get; set; }

    /// <summary>Nivel 1/2 (Carrera universitaria/Tecnicatura): productos directos, sin concepto de seminario.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DegreeProgramResponse>? Products { get; set; }

    /// <summary>Nivel 3/4 (Actualizacion profesional): productos agrupados por si tienen seminario o no.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DegreeProgramsBySeminarResponse>? Seminars { get; set; }
}

/// <summary>Productos agrupados según tengan o no seminario.</summary>
[ExcludeFromCodeCoverage]
public class DegreeProgramsBySeminarResponse
{
    /// <summary>Indica si los productos de este grupo tienen seminario.</summary>
    public bool HasSeminar { get; set; }

    /// <summary>Productos del grupo.</summary>
    public List<DegreeProgramResponse> Products { get; set; } = [];
}

/// <summary>Carrera ofrecida, con el proceso de admisión habilitado si corresponde.</summary>
[ExcludeFromCodeCoverage]
public class DegreeProgramResponse
{
    /// <summary>Código del producto.</summary>
    public long ProductId { get; set; }

    /// <summary>Nombre del producto.</summary>
    public string? ProductName { get; set; }

    /// <summary>Proceso de admisión habilitado, cuando existe.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? AdmissionProcessId { get; set; }
}

/// <summary>Comienzo (cohorte) disponible para un producto.</summary>
[ExcludeFromCodeCoverage]
public class IntakeResponse
{
    /// <summary>Código del proceso de admisión.</summary>
    public long AdmissionProcessId { get; set; }

    /// <summary>Nombre del proceso de admisión.</summary>
    public string? AdmissionProcessName { get; set; }
}
