namespace AppLogic.Registration.Constants;

public static class RegistrationFlowConstants
{
    /// <summary>
    /// Valores posibles de <see cref="Dtos.RegistrationFlowSession.Step"/>.
    /// </summary>
    public static class Step
    {
        public const string Evaluado = "evaluado";
        public const string IdentidadVerificada = "identidad_verificada";
        public const string Confirmado = "confirmado";
    }
}
