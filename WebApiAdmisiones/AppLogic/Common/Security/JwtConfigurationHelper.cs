using System.Globalization;

namespace AppLogic.Common.Security;

/// <summary>
/// Lectura de variables de entorno de configuración JWT, unificando el parseo
/// duplicado entre <c>TokenService</c>, <c>TokenServiceInternalApi</c> y <c>AuthService</c>.
/// Cada caller sigue leyendo su propia variable (secret key o expiración) — este helper
/// solo centraliza el "cómo" (validar presencia, parsear), no el "qué" variable ni su valor.
/// No usar para <c>PASSWORD_ACTIVATION_SECRET_KEY</c> (tiene una validación de longitud mínima
/// adicional) ni para la lectura de expiración de cookies en <c>AuthController</c> (que tiene
/// defaults y tipo de dato distintos) — ver docs/20-shared-jwt-config.md.
/// </summary>
public static class JwtConfigurationHelper
{
    public static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"La variable de entorno {variableName} no está configurada.");
        }

        return value;
    }

    public static double GetRequiredDouble(string variableName)
    {
        var rawValue = GetRequiredEnvironmentVariable(variableName);

        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"La variable de entorno {variableName} tiene un valor inválido.");
        }

        return value;
    }
}
