namespace AppLogic.Helpers
{
    public static class EmailMaskingHelper
    {
        private const string MaskValue = "******";

        public static string Mask(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return MaskValue;

            var normalizedEmail = email.Trim();
            var atIndex = normalizedEmail.IndexOf('@');

            if (atIndex <= 0 || atIndex != normalizedEmail.LastIndexOf('@') || atIndex == normalizedEmail.Length - 1)
                return MaskValue;

            var local = normalizedEmail[..atIndex];
            var domain = normalizedEmail[(atIndex + 1)..];
            var firstDotIndex = domain.IndexOf('.');

            if (firstDotIndex <= 0 || firstDotIndex == domain.Length - 1)
                return MaskValue;

            return $"{MaskLocalPart(local)}@{domain[..firstDotIndex]}.{MaskValue}";
        }

        private static string MaskLocalPart(string local)
        {
            if (local.Length == 1)
                return $"{local[0]}{MaskValue}";

            return $"{local[0]}{MaskValue}{local[^1]}";
        }
    }
}
