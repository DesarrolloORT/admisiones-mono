using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Detalle de una inscripción de "Mis carreras". El contenido depende del estado:
    /// "En proceso" trae la oferta seleccionada; "A la espera" no trae detalle;
    /// "Pago pendiente" trae seña/vencimiento/resumen; "Confirmada" trae número de estudiante,
    /// coordinador académico, resumen de carrera y materias del primer semestre.
    /// </summary>
    public class DetalleInscripcionResponse
    {
        public string Estado { get; set; } = string.Empty;

        /// <summary>Oferta seleccionada. Solo se completa cuando el estado es "En proceso".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResumenInscripcionDto? Detalle { get; set; }

        /// <summary>Solo se completa cuando el estado es "Pago pendiente".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ConfirmarPreInscripcionResponse? PagoPendiente { get; set; }

        /// <summary>Solo se completa cuando el estado es "Confirmada".</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ConfirmadaDetalleDto? Confirmada { get; set; }
    }

    public class ConfirmadaDetalleDto
    {
        public long NumeroEstudiante { get; set; }
        public ResumenInscripcionDto Resumen { get; set; } = new();
        public CoordinadorDto? CoordinadorAcademico { get; set; }
        public CoordinadorDto? CoordinadorCursos { get; set; }
        public List<MateriaDto> MateriasPrimerSemestre { get; set; } = new();
    }

    public class CoordinadorDto
    {
        public string? Nombre { get; set; }
        public string? Email { get; set; }
    }

    public class MateriaDto
    {
        public long IdMateria { get; set; }
        public string? Nombre { get; set; }
    }
}
