using Utilities;

namespace AppLogic.Helpers;

/// <summary>
/// Propagación de errores entre <see cref="OperationResult{T}"/> de distinto tipo de dato
/// sin re-copiar a mano ErrorCode/Method/Message/HttpCode.
///
/// <para>Cuando el tipo de dato NO cambia, devolvé el resultado tal cual (<c>return resultado;</c>).</para>
/// <para>Cuando cambia el tipo, reproyectá el error: <c>return resultado.Failure().As&lt;bool&gt;();</c></para>
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// Captura el error de un resultado FALLIDO para reproyectarlo a otro tipo de dato.
    /// Uso previsto: sólo cuando <c>!resultado.Success</c>.
    /// </summary>
    public static ErrorCarrier Failure<TSource>(this OperationResult<TSource> source)
        => new(source.ErrorCode, source.Method, source.Message, source.HttpCode);

    /// <summary>Error de un <see cref="OperationResult{T}"/> desacoplado de su tipo de dato.</summary>
    public readonly struct ErrorCarrier(string errorCode, string method, string message, int httpCode)
    {
        /// <summary>Construye el mismo error como <see cref="OperationResult{T}"/> del tipo destino.</summary>
        public OperationResult<T> As<T>()
            => OperationResult<T>.IsFailed(errorCode, method, message, httpCode);
    }
}
