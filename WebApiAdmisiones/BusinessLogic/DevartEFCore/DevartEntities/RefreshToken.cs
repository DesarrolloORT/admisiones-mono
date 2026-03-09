//------------------------------------------------------------------------------
// This code was generated based on the EF Core template for the admisiones model.
//------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Entities
{
    public partial class RefreshToken
    {
        public RefreshToken()
        {
            this.IsActive = @"NO";
        }

        [Key]
        public long CodigoPersona { get; set; }

        [Key]
        [Required]
        public string Sistema { get; set; } = string.Empty;

        [Required]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RemplaceByTokenId { get; set; }

        [Required]
        public string IsActive { get; set; }

        public DateTime? FechaIngreso { get; set; }

        public string? HoraIngreso { get; set; }

        public string? UsuarioIngreso { get; set; }
    }
}
