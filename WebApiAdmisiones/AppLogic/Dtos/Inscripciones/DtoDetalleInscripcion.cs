using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Dtos.Inscripciones
{
    /// <summary>
    /// Detalle de una inscripción de "Mis carreras". El contenido depende del estado:
    /// "En proceso" trae la oferta seleccionada; "A la espera" no trae detalle;
    /// "Pago pendiente" trae seña/vencimiento/resumen; "Confirmada" trae número de estudiante,
    /// coordinador académico, resumen de carrera y materias del primer semestre.
    /// </summary>
    public class DtoDetalleInscripcionResponse
    {
        public string Estado { get; set; } = string.Empty;

        /// <summary>Oferta seleccionada. Solo se completa cuando el estado es "En proceso".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoResumenInscripcion? Detalle { get; set; }

        /// <summary>Solo se completa cuando el estado es "Pago pendiente".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoConfirmarPreInscripcionResponse? PagoPendiente { get; set; }

        /// <summary>Solo se completa cuando el estado es "Confirmada".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoConfirmadaDetalle? Confirmada { get; set; }
    }

    public class DtoConfirmadaDetalle
    {
        public long NumeroEstudiante { get; set; }
        public DtoResumenInscripcion Resumen { get; set; } = new();
        public DtoCoordinador? CoordinadorAcademico { get; set; }
        public DtoCoordinador? CoordinadorCursos { get; set; }
        public List<DtoMateria> MateriasPrimerSemestre { get; set; } = new();
    }

    public class DtoCoordinador
    {
        public string? Nombre { get; set; }
        public string? Email { get; set; }
    }

    public class DtoMateria
    {
        public long IdMateria { get; set; }
        public string? Nombre { get; set; }
    }
}
