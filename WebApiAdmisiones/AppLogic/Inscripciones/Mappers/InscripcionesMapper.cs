using System.Diagnostics.CodeAnalysis;
using AppLogic.ApiClients.Dtos;
using AppLogic.Inscripciones.Dtos;
using AppLogic.Inscripciones.Rules;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Inscripciones.Mappers
{
    /// <summary>
    /// Traduce entidades y respuestas de la API interna a los DTOs de respuesta del módulo Inscripciones.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class InscripcionesMapper
    {
        private sealed record CoordinadorMapeado(DtoCoordinador Coordinador, long? Codigo);

        public static DtoConfirmarPreInscripcionResponse MapearPagoPendiente(Inscripto inscripto, CarritosInscripcionApiResponse? carritos)
        {
            var pagoReserva = ConfirmarPreInscripcionRules.SumarPagoReserva(carritos?.Carritos);
            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                EstadoCuenta = MapearEstadoCuenta(carritos?.EstadoCuenta),
                Resumen = MapearResumenDesdeInscripto(inscripto),
                Ofertas =
                [
                    new DtoResultadoInscripcionOferta
                    {
                        IdOferta = inscripto.IdOferta ?? 0,
                        IdInscripcion = inscripto.IdInscripto,
                        FechaVencimientoPago = inscripto.FechaVtoInscr,
                        PagoReserva = pagoReserva
                    }
                ],
                PagoReserva = pagoReserva
            };
        }

        public static DtoConfirmadaDetalle MapearConfirmada(
            long codigoPersona,
            Inscripto inscripto,
            ICollection<VdInscriptoCoordinadore> coordinadores,
            ICollection<VdInscriptoCreditoAlumno> materias)
        {
            var coordinadorAcademico = MapearCoordinadorAcademico(coordinadores);
            var coordinadorCursos = MapearCoordinadorCursos(coordinadores);
            if (EsMismoCoordinador(coordinadorAcademico, coordinadorCursos))
            {
                coordinadorCursos = null;
            }

            return new DtoConfirmadaDetalle
            {
                NumeroEstudiante = codigoPersona,
                Resumen = MapearResumenDesdeInscripto(inscripto),
                CoordinadorAcademico = coordinadorAcademico?.Coordinador,
                CoordinadorCursos = coordinadorCursos?.Coordinador,
                MateriasPrimerSemestre = materias
                    .Where(m => m.IdMateria.HasValue)
                    .GroupBy(m => m.IdMateria!.Value)
                    .Select(g => new DtoMateria { IdMateria = g.Key, Nombre = g.First().DescripcionMateria?.Trim() })
                    .ToList()
            };
        }

        public static OperationResult<DtoConfirmarPreInscripcionResponse> MapearResultadoApiMultiple(
            OperationResult<ConfirmarPreInscripcionMultipleApiResponse> apiResult,
            DatosConfirmacionOferta contexto,
            string methodName)
        {
            if (!apiResult.Success)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(apiResult.ErrorCode, methodName, apiResult.Message, apiResult.HttpCode);
            }

            if (apiResult.Data == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_13", methodName, "La API interna no devolvio datos de confirmacion.", 502);
            }

            return OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(
                MapearConfirmacionPreInscripcionMultiple(apiResult.Data, contexto),
                methodName);
        }

        public static DtoResumenInscripcion MapearResumenDesdeInscripto(Inscripto inscripto)
        {
            var producto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto;
            var comienzo = inscripto.Oferta?.Supraoferta?.Comienzo;
            return new DtoResumenInscripcion
            {
                IdOferta = inscripto.IdOferta ?? 0,
                IdProducto = producto?.IdProducto ?? 0,
                Carrera = NombreCarrera(producto),
                IdComienzo = comienzo?.IdComienzo ?? 0,
                Comienzo = comienzo?.NombreComienzo,
                IdTurno = inscripto.Oferta?.IdTurno ?? 0,
                Turno = inscripto.Oferta?.Turno?.NombreTurno
            };
        }

        public static DtoResumenInscripcion? MapearOfertaResumen(Oferta? oferta, long idProceso)
        {
            if (oferta == null)
            {
                return null;
            }

            var producto = oferta.Supraoferta?.Paquete?.Producto;
            var comienzo = oferta.Supraoferta?.Comienzo;
            return new DtoResumenInscripcion
            {
                IdOferta = oferta.IdOferta,
                IdProducto = producto?.IdProducto ?? 0,
                Carrera = NombreCarrera(producto),
                IdComienzo = idProceso,
                Comienzo = comienzo?.NombreComienzo,
                IdTurno = oferta.IdTurno,
                Turno = oferta.Turno?.NombreTurno
            };
        }

        private static DtoConfirmarPreInscripcionResponse MapearConfirmacionPreInscripcionMultiple(
            ConfirmarPreInscripcionMultipleApiResponse source,
            DatosConfirmacionOferta contexto)
        {
            var ofertas = source.Ofertas
                .Select(o => new DtoResultadoInscripcionOferta
                {
                    IdOferta = o.IdOferta,
                    IdInscripcion = o.IdInscripcion,
                    FechaVencimientoPago = o.FechaVencimientoPago,
                    PagoReserva = (decimal)o.ValorSeniaMinima
                })
                .ToList();

            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = source.Confirmada || source.Respuesta,
                EnEspera = source.InscripcionPendiente,
                EstadoCuenta = MapearEstadoCuenta(source.EstadoCuenta),
                Resumen = MapearResumen(source.Resumen, contexto),
                Ofertas = ofertas,
                // El alumno paga todas las ofertas confirmadas de una sola vez, no elige: el front recibe
                // directamente el total (para nivel 1 y 2, con una sola oferta, coincide con esa unica seña).
                PagoReserva = ofertas.Sum(o => o.PagoReserva)
            };
        }

        private static DtoResumenInscripcion MapearResumen(ResumenInscripcionApiDto? source, DatosConfirmacionOferta contexto)
        {
            return new DtoResumenInscripcion
            {
                IdOferta = source != null && source.IdOferta > 0 ? source.IdOferta : contexto.IdOferta,
                IdProducto = source?.IdProducto ?? contexto.IdProducto,
                Carrera = source?.Carrera ?? contexto.Producto?.NombreExtensoProducto ?? contexto.Producto?.NombreProducto,
                IdComienzo = source?.IdComienzo ?? contexto.IdComienzo,
                Comienzo = source?.Comienzo ?? contexto.Comienzo?.NombreComienzo,
                IdTurno = source?.IdTurno ?? contexto.IdTurno,
                Turno = source?.Turno ?? contexto.Turno?.NombreTurno
            };
        }

        private static DtoEstadoCuenta? MapearEstadoCuenta(EstadoCuentaApiDto? source)
        {
            if (source == null)
            {
                return null;
            }

            return new DtoEstadoCuenta
            {
                SaldoActual = source.SaldoActual
            };
        }

        private static CoordinadorMapeado? MapearCoordinadorAcademico(IEnumerable<VdInscriptoCoordinadore> coordinadores)
        {
            var coordinador = coordinadores.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.CooacadPrimerNombre)
                || !string.IsNullOrWhiteSpace(c.CooacadPrimerApellido)
                || !string.IsNullOrWhiteSpace(c.MailAcad));

            return coordinador == null
                ? null
                : new CoordinadorMapeado(
                    new DtoCoordinador
                    {
                        Nombre = NombreCompleto(coordinador.CooacadPrimerNombre, coordinador.CooacadPrimerApellido),
                        Email = coordinador.MailAcad?.Trim()
                    },
                    coordinador.CooacadCodigo);
        }

        private static CoordinadorMapeado? MapearCoordinadorCursos(IEnumerable<VdInscriptoCoordinadore> coordinadores)
        {
            var coordinador = coordinadores.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.CoorespPrimerNombre)
                || !string.IsNullOrWhiteSpace(c.CoorespPrimerApellido)
                || !string.IsNullOrWhiteSpace(c.MailResp));

            return coordinador == null
                ? null
                : new CoordinadorMapeado(
                    new DtoCoordinador
                    {
                        Nombre = NombreCompleto(coordinador.CoorespPrimerNombre, coordinador.CoorespPrimerApellido),
                        Email = coordinador.MailResp?.Trim()
                    },
                    coordinador.CoorespCodigo);
        }

        private static bool EsMismoCoordinador(CoordinadorMapeado? academico, CoordinadorMapeado? cursos)
        {
            if (academico == null || cursos == null)
            {
                return false;
            }

            if (academico.Codigo.HasValue && cursos.Codigo.HasValue)
            {
                return academico.Codigo.Value == cursos.Codigo.Value;
            }

            return SonTextosEquivalentes(academico.Coordinador.Email, cursos.Coordinador.Email)
                || SonTextosEquivalentes(academico.Coordinador.Nombre, cursos.Coordinador.Nombre);
        }

        private static bool SonTextosEquivalentes(string? textoA, string? textoB)
        {
            return !string.IsNullOrWhiteSpace(textoA)
                && !string.IsNullOrWhiteSpace(textoB)
                && string.Equals(textoA.Trim(), textoB.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string? NombreCompleto(string? nombre, string? apellido)
        {
            var partes = new[] { nombre?.Trim(), apellido?.Trim() }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var completo = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(completo) ? null : completo;
        }

        /// <summary>Nombre de carrera a mostrar: web, si no el extenso, si no el corto.</summary>
        private static string? NombreCarrera(Producto? producto)
        {
            return producto?.NombreWebProducto ?? producto?.NombreExtensoProducto ?? producto?.NombreProducto;
        }
    }
}
