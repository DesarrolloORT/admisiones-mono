using AppLogic.Constants;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Helpers
{
    public static class FondoDeBecaValidation
    {
        public static OperationResult<bool> ValidarSolicitudGuardadoDeclaracion(
            DtoDeclaracionJuradaWebDevart? declaracionModificada,
            string methodName)
        {
            if (declaracionModificada is null)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_00",
                    methodName,
                    "Se requiere la declaración modificada.",
                    400);
            }

            if (declaracionModificada.IdInscriptoPrueba <= 0)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_15",
                    methodName,
                    "Se requiere una inscripción a prueba válida para guardar la declaración jurada.",
                    400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<bool> ValidarConfirmacionDeclaracion(
            DeclaracionJuradaWeb declaracion,
            string methodName)
        {
            var validacionAcademica = ValidarAntecedentesAcademicosParaConfirmacion(declaracion, methodName);
            if (!validacionAcademica.Success)
            {
                return validacionAcademica;
            }

            foreach (var ingreso in declaracion.IntegranteNfDjs.SelectMany(i => i.IngresoMensualNfDjs))
            {
                var validacionIngreso = ValidarIngresoParaConfirmacion(ingreso, methodName);
                if (!validacionIngreso.Success)
                {
                    return validacionIngreso;
                }
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<string> ValidarArchivoAdjunto(byte[] fileContent, string fileName, string methodName)
        {
            var validacion = FileValidationHelper.ValidateDeclaracionJuradaAttachment(fileContent, fileName, methodName);
            if (!validacion.Success)
            {
                return OperationResult<string>.IsFailed(
                    validacion.ErrorCode,
                    methodName,
                    validacion.Message,
                    validacion.HttpCode);
            }

            return FileValidationHelper.SanitizeDeclaracionJuradaAttachmentName(fileName, methodName);
        }

        private static OperationResult<bool> ValidarAntecedentesAcademicosParaConfirmacion(
            DeclaracionJuradaWeb declaracion,
            string methodName)
        {
            if (declaracion.IdTipoDescuento != FondoDeBecaConstants.Declaracion.TipoDescuentoAntecedentesAcademicos)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            if (!declaracion.CodigoInstitucionBac.HasValue
                || !declaracion.CodigoInstitucionUniv.HasValue
                || string.IsNullOrWhiteSpace(declaracion.CarreraUniversitariaDj)
                || !declaracion.CantMateriasAprobadasDj.HasValue
                || !declaracion.CantMatExaReprobadosDj.HasValue
                || !declaracion.PromTotalCalificacionesDj.HasValue)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_07",
                    methodName,
                    "Se deben indicar los antecedentes académicos para confirmar la declaración jurada.",
                    400);
            }

            if (declaracion.CodigoInstitucionUniv == FondoDeBecaConstants.Declaracion.CodigoInstitucionOrt
                && string.IsNullOrWhiteSpace(declaracion.FacultadUniversidadDj))
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_08",
                    methodName,
                    "Debe indicar la universidad.",
                    400);
            }

            if (declaracion.CodigoInstitucionUniv != FondoDeBecaConstants.Declaracion.CodigoInstitucionOrt
                && !string.IsNullOrWhiteSpace(declaracion.FacultadUniversidadDj))
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_09",
                    methodName,
                    "Existe una inconsistencia en la información sobre la universidad indicada.",
                    400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarIngresoParaConfirmacion(
            IngresoMensualNfDj ingreso,
            string methodName)
        {
            if (ingreso.NominalIngresoNfDj < 0)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_10",
                    methodName,
                    "El ingreso debe ser mayor o igual a cero.",
                    400);
            }

            if (ingreso.NominalIngresoNfDj.HasValue
                && decimal.Truncate(ingreso.NominalIngresoNfDj.Value) != ingreso.NominalIngresoNfDj.Value)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_11",
                    methodName,
                    "Los ingresos nominales no pueden contener decimales.",
                    400);
            }

            if (ingreso.NominalIngresoNfDj.HasValue
                && ingreso.NominalIngresoNfDj.Value > 0
                && ingreso.NominalIngresoNfDj.Value < 1000)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_12",
                    methodName,
                    "Los ingresos nominales deben ser mayores o iguales a 1000.",
                    400);
            }

            if (ingreso.DescuentoslegalesIngresoNf.HasValue
                && decimal.Truncate(ingreso.DescuentoslegalesIngresoNf.Value) != ingreso.DescuentoslegalesIngresoNf.Value)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_13",
                    methodName,
                    "Los descuentos legales no pueden contener decimales.",
                    400);
            }

            if (ingreso.ArchivoIngresoNfDj is null || ingreso.ArchivoIngresoNfDj.Length == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "FDB_GDJ_14",
                    methodName,
                    "Para confirmar la declaración jurada, todos los ingresos deben tener archivo cargado.",
                    400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }
    }
}
