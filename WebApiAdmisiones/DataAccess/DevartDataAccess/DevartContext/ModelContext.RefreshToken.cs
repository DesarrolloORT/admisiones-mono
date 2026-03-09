using BusinessLogic.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    /// <summary>
    /// Extensión parcial de ModelContext para agregar el soporte de RefreshToken
    /// sin modificar el archivo auto-generado.
    /// </summary>
    public partial class ModelContext
    {
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        partial void CustomizeMapping(ref ModelBuilder modelBuilder)
        {
            // RefreshToken mapping
            modelBuilder.Entity<RefreshToken>().ToTable(@"T_REFRESH_TOKEN", @"CREADOR");
            modelBuilder.Entity<RefreshToken>().Property(x => x.CodigoPersona).HasColumnName(@"CODIGO_PERSONA").IsRequired().ValueGeneratedNever();
            modelBuilder.Entity<RefreshToken>().Property(x => x.Sistema).HasColumnName(@"SISTEMA").HasColumnType(@"VARCHAR2").IsRequired().ValueGeneratedNever().HasMaxLength(30);
            modelBuilder.Entity<RefreshToken>().Property(x => x.TokenHash).HasColumnName(@"TOKEN_HASH").HasColumnType(@"VARCHAR2").IsRequired().ValueGeneratedNever().HasMaxLength(200);
            modelBuilder.Entity<RefreshToken>().Property(x => x.ExpiresAt).HasColumnName(@"EXPIRES_AT").HasColumnType(@"DATE").IsRequired().ValueGeneratedNever();
            modelBuilder.Entity<RefreshToken>().Property(x => x.CreatedAt).HasColumnName(@"CREATED_AT").HasColumnType(@"DATE").IsRequired().ValueGeneratedNever();
            modelBuilder.Entity<RefreshToken>().Property(x => x.RevokedAt).HasColumnName(@"REVOKED_AT").HasColumnType(@"DATE").ValueGeneratedNever();
            modelBuilder.Entity<RefreshToken>().Property(x => x.RemplaceByTokenId).HasColumnName(@"REMPLACE_BY_TOKEN_ID").HasColumnType(@"VARCHAR2").ValueGeneratedNever().HasMaxLength(200);
            modelBuilder.Entity<RefreshToken>().Property(x => x.IsActive).HasColumnName(@"IS_ACTIVE").HasColumnType(@"VARCHAR2").IsRequired().ValueGeneratedNever().HasMaxLength(2).HasDefaultValueSql(@"'NO'");
            modelBuilder.Entity<RefreshToken>().Property(x => x.FechaIngreso).HasColumnName(@"FECHA_INGRESO").HasColumnType(@"DATE").ValueGeneratedOnAdd().HasDefaultValueSql(@"TO_DATE(TO_CHAR(SYSDATE,'YYYY-MM-DD'),'YYYY-MM-DD')");
            modelBuilder.Entity<RefreshToken>().Property(x => x.HoraIngreso).HasColumnName(@"HORA_INGRESO").HasColumnType(@"VARCHAR2").ValueGeneratedOnAdd().HasMaxLength(12).HasDefaultValueSql(@"to_char(sysdate,'HH24:MI:SS')");
            modelBuilder.Entity<RefreshToken>().Property(x => x.UsuarioIngreso).HasColumnName(@"USUARIO_INGRESO").HasColumnType(@"VARCHAR2").ValueGeneratedNever().HasMaxLength(30);
            modelBuilder.Entity<RefreshToken>().HasKey(@"CodigoPersona", @"Sistema");

            // FK a Persona (sin navigation property en Persona para no tocar el archivo auto-generado)
            modelBuilder.Entity<RefreshToken>()
                .HasOne<Persona>()
                .WithMany()
                .HasForeignKey(x => x.CodigoPersona)
                .IsRequired(true);
        }
    }
}
