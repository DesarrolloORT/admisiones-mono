using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class TipoSeguroSaludRequest
    {
        public long IdSeguroSalud { get; set; }
        public required string DescripcionSeguroSalud { get; set; }
        public bool TieneHijos { get; set; }
        public bool TieneConyuge { get; set; }
        public bool TieneConcubino { get; set; }
        public bool SeguroSaludPorOrt { get; set; }
        public bool SocioVitalicio { get; set; }
        public bool AfiliacionMutualOtraEmpresa { get; set; }
    }
}
