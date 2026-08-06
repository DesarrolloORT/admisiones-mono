namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Valida al arranque que estén definidos los secretos que la app necesita y que ya no viven en
    /// <c>appsettings</c>.
    /// </summary>
    /// <remarks>
    /// Existe porque los consumidores de estas claves están en el submódulo <c>Core</c>, que resuelve
    /// la configuración con <c>?? string.Empty</c>: si falta el valor no explota, manda credenciales
    /// vacías y el error aparece recién como un fallo de autenticación contra el servicio interno.
    /// Preferimos que la app no levante antes que ese modo de falla.
    /// </remarks>
    public static class RequiredConfigurationExtensions
    {
        /// <summary>
        /// Claves de configuración obligatorias. Se definen por variable de entorno usando <c>__</c>
        /// como separador de nivel (por ejemplo <c>ApiServicioInterno__Password</c>).
        /// </summary>
        private static readonly string[] RequiredKeys =
        [
            "ApiServicioInterno:RutaApi",
            "ApiServicioInterno:Usuario",
            "ApiServicioInterno:Password"
        ];

        /// <summary>
        /// Corta el arranque si falta alguna clave obligatoria, listándolas todas juntas para no
        /// obligar a descubrirlas de a una.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si falta al menos una clave.</exception>
        public static IConfiguration ValidateRequiredSecrets(this IConfiguration configuration)
        {
            var missing = RequiredKeys
                .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
                .Select(key => key.Replace(":", "__", StringComparison.Ordinal))
                .ToList();

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "Falta configuración obligatoria. Definí estas variables de entorno: " +
                    string.Join(", ", missing) + ". " +
                    "Dejaron de estar en appsettings porque son credenciales.");
            }

            return configuration;
        }
    }
}
