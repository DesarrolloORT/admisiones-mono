namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Extensiones para IHostEnvironment que agrupan ambientes Production y Preproduction
    /// como "ambientes productivos" que requieren las mismas configuraciones de seguridad.
    /// </summary>
    public static class EnvironmentExtensions
    {
        /// <summary>
        /// Verifica si el ambiente actual es Production o Preproduction.
        /// Ambos ambientes se consideran productivos y requieren las mismas configuraciones de seguridad.
        /// </summary>
        /// <param name="environment">La instancia de IHostEnvironment.</param>
        /// <returns>True si el ambiente es Production o Preproduction, false en caso contrario.</returns>
        public static bool IsProductionLike(this IHostEnvironment environment)
        {
            return environment.IsProduction() || environment.IsEnvironment("Preproduction");
        }
    }
}
