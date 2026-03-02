using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;
using BusinessLogic.IGenericRepository;

namespace DataAccess.GenericAccess.Services
{
    [ExcludeFromCodeCoverage]
    public class GenericRepository : IGenericRepository
    {
        private readonly ModelContext _context;

        public GenericRepository(ModelContext context)
        {
            _context = context;
        }

        public async Task<double> DarValorDiaTrabajado()
        {
            string sentencia = "SELECT VALOR_UI_PARAM_LIQ*CANT_UI_DIA_PARAM_LIQ AS \"Value\" FROM T_PARAM_LIQ " +
                               "WHERE ANIOMES_PARAM_LIQ = (SELECT MAX(ANIOMES_PARAM_LIQ) FROM T_PARAM_LIQ)";
            FormattableString formattableSql = FormattableStringFactory.Create(sentencia);
            var resultado = await _context.Database.SqlQuery<double>(formattableSql).FirstOrDefaultAsync();
            return resultado;
        }

        // Modified method to return a nullable decimal
        public async Task<decimal?> ObtenerAnioMesVigenciaLiq(long codigoPersona, decimal anioMesLiquidacion)
        {
            string sql = $@"
                        SELECT 
                            (SELECT ANIOMES_LIQUIDACION 
                             FROM VD_VALIDO_LIQUIDACION_SNIS
                             WHERE CODIGO_PERSONA = {codigoPersona} 
                               AND ANIOMES_LIQUIDACION = {anioMesLiquidacion}
                             GROUP BY ANIOMES_LIQUIDACION) AS ""Value""
                        FROM T_PERSONA 
                        WHERE CODIGO_PERSONA = {codigoPersona}";

            FormattableString formattableSql = FormattableStringFactory.Create(sql);
            var result = await _context.Database.SqlQuery<decimal?>(formattableSql).FirstOrDefaultAsync();
            return result;
        }

        // Verifica si existen registros en vd_valida_egreso_3100 para una persona
        public async Task<bool> ExisteEgreso3100(long codigoPersona)
        {
            // Cuenta filas que coinciden con el código de persona
            string sql = $@"select count(*) as ""Value"" from vd_valida_egreso_3100 where codigo_persona = {codigoPersona}";
            FormattableString formattableSql = FormattableStringFactory.Create(sql);
            var count = await _context.Database.SqlQuery<int>(formattableSql).FirstOrDefaultAsync();
            return count > 0;
        }

        // Verifica si el aniomes actual coincide con el primer registro de liquidación válido
        public async Task<bool> ExisteLiquidacionValida(long codigoPersona, decimal anioMesActual)
        {
            string sql = $@"SELECT ANIOMES_LIQUIDACION AS ""Value"" FROM VD_VALIDO_LIQUIDACION_SNIS
                             WHERE CODIGO_PERSONA = {codigoPersona}
                               AND ANIOMES_LIQUIDACION >= {anioMesActual}
                               AND id_estado_liquidacion >= 8
                             GROUP BY ANIOMES_LIQUIDACION";
            FormattableString formattableSql = FormattableStringFactory.Create(sql);
            var dbValue = await _context.Database.SqlQuery<decimal?>(formattableSql).FirstOrDefaultAsync();

            // Si el valor en BD es igual al aniomesActual, retornar true; en otro caso false
            return dbValue.HasValue && dbValue.Value == anioMesActual;
        }

    }
}
