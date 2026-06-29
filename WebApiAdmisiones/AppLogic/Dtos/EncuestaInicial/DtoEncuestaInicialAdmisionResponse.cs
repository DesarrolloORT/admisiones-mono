using AppLogic.DevartDTOs;
using System.Text.Json.Serialization;

namespace AppLogic.Dtos.EncuestaInicial
{
    public class DtoEncuestaInicialAdmisionResponse
    {
        public bool TieneDerechoEncuesta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoEncuestaIniAdmisionDevart? Encuesta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DtoEmpresaConsideradaAdmisionDevart>? UniversidadesConsideradas { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DtoEducacionSuperiorAdmisionDevart>? UniversidadesEducacionSuperior { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DtoMotivoEleccionAdmisionDevart>? OpcionesMotivosSeleccionados { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DtoPublicidadEleccionAdmisionDevart>? OpcionesPublicidadSeleccionadas { get; set; }
    }
}
