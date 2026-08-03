using System;
using System.Linq;

namespace DataAccess.DevartRepositories
{
    /// <summary>
    /// Cache en memoria de ParametrosAdmisiones: tabla de configuracion global (no depende de
    /// ninguna persona ni cambia por accion del usuario), consultada hoy en cada llamada a los
    /// repos de inscripciones fresco (1y2 y 3y4). Evita pegarle a la base por un dato que
    /// practicamente no cambia.
    /// </summary>
    internal static class ParametrosAdmisionesCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
        private static readonly object Lock = new();
        private static (DateTime ExpiraEn, long DiasExtra1y2, long DiasExtra3, long DiasExtra4)? _cache;

        public static (long DiasExtra1y2, long DiasExtra3, long DiasExtra4) Get(DataAccess.ModelContext context)
        {
            var cache = _cache;
            if (cache != null && cache.Value.ExpiraEn > DateTime.UtcNow)
            {
                return (cache.Value.DiasExtra1y2, cache.Value.DiasExtra3, cache.Value.DiasExtra4);
            }

            lock (Lock)
            {
                cache = _cache;
                if (cache != null && cache.Value.ExpiraEn > DateTime.UtcNow)
                {
                    return (cache.Value.DiasExtra1y2, cache.Value.DiasExtra3, cache.Value.DiasExtra4);
                }

                var parametros = context.ParametrosAdmisiones
                    .Select(p => new
                    {
                        p.DiasExtraPermiteInscr1y2,
                        p.DiasExtraPermiteInscr3,
                        p.DiasExtraPermiteInscr4
                    })
                    .FirstOrDefault();

                var diasExtra1y2 = parametros?.DiasExtraPermiteInscr1y2 ?? 0;
                var diasExtra3 = parametros?.DiasExtraPermiteInscr3 ?? 0;
                var diasExtra4 = parametros?.DiasExtraPermiteInscr4 ?? 0;

                _cache = (DateTime.UtcNow.Add(Ttl), diasExtra1y2, diasExtra3, diasExtra4);
                return (diasExtra1y2, diasExtra3, diasExtra4);
            }
        }
    }
}
