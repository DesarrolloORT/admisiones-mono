using System;

namespace AppLogic.Autenticacion.Interfaces
{
    /// <summary>
    /// Servicio para generar tokens de autenticación para comunicación con APIs internas.
    /// Este servicio SOLO genera tokens. La validación se realiza en la API destino.
    /// </summary>
    public interface ITokenServiceInternalApi
    {
        /// <summary>
        /// Genera un token JWT firmado que identifica a este servicio (API Admisiones).
        /// Este token se usa en el header X-Service-Token para llamadas a APIs internas.
        /// </summary>
        /// <param name="targetApi">Nombre de la API destino (ej: "api-inscripciones-pagos").</param>
        /// <param name="scopes">Permisos específicos que se solicitan (ej: "inscripciones.write").</param>
        /// <returns>Token JWT firmado con clave simétrica.</returns>
        string GenerateServiceToken(string targetApi, params string[] scopes);
    }
}
