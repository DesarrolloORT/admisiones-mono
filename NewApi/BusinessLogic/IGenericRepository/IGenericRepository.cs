using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.IGenericRepository
{
    public interface IGenericRepository
    {
        Task<double> DarValorDiaTrabajado();
        Task<decimal?> ObtenerAnioMesVigenciaLiq(long codigoPersona, decimal anioMesLiquidacion);
        Task<bool> ExisteEgreso3100(long codigoPersona);
        Task<bool> ExisteLiquidacionValida(long codigoPersona, decimal anioMesActual);
    }
}
