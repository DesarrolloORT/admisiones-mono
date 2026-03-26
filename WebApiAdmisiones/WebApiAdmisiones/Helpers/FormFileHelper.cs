using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace WebApiAdmisiones.Helpers
{
    [ExcludeFromCodeCoverage]
    internal static class FormFileHelper
    {
        public static async Task<(byte[] Content, string FileName)> ReadFileAsync(IFormFile? file)
        {
            if (file is null)
            {
                return (Array.Empty<byte>(), string.Empty);
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return (memoryStream.ToArray(), file.FileName ?? string.Empty);
        }
    }
}
