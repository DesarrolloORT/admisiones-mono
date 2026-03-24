using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Helpers
{
    /// <summary>
    /// Respuesta de validación de ingreso/actualización de declaración 3100.
    /// Contiene el resultado de la validación y un mensaje descriptivo sobre qué validación no se cumplió.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ValidationResponse
    {
        /// <summary>
        /// Indica si la persona puede ingresar o actualizar la declaración 3100.
        /// </summary>
        public bool PuedeIngresar { get; set; }

        /// <summary>
        /// Mensaje descriptivo sobre el resultado de la validación.
        /// Si PuedeIngresar es false, indica qué validación no se cumplió.
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Constructor para crear una respuesta exitosa.
        /// </summary>
        public static ValidationResponse Exitosa()
        {
            return new ValidationResponse
            {
                PuedeIngresar = true,
                Mensaje = "Validación exitosa."
            };
        }

        /// <summary>
        /// Constructor para crear una respuesta fallida con mensaje específico.
        /// </summary>
        public static ValidationResponse Fallida(string mensaje)
        {
            return new ValidationResponse
            {
                PuedeIngresar = false,
                Mensaje = mensaje,
            };
        }
    }
}
