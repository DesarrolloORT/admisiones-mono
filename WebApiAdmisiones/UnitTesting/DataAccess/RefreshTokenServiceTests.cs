using BusinessLogic.Entities;
using DataAccess;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UnitTesting.DataAccess
{
    /// <summary>
    /// Tests dedicados de <see cref="RefreshTokenService"/>.
    /// Usan el proveedor EF Core InMemory sobre el <see cref="ModelContext"/> real.
    /// El foco es la atomicidad de <see cref="RefreshTokenService.SaveRefreshTokenAsync"/>
    /// (fix rama fix/refresh-token-atomicity).
    /// </summary>
    public class RefreshTokenServiceTests
    {
        private const string Sistema = "ADMISIONESWEB";
        private const long CodigoPersona = 100L;

        private static DbContextOptions<ModelContext> InMemoryOptions(string dbName) =>
            new DbContextOptionsBuilder<ModelContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

        private static ModelContext CreateContext(string dbName) => new(InMemoryOptions(dbName));

        private static void SeedToken(
            string dbName,
            string tokenHash,
            DateTime expiresAt,
            string isActive = "SI",
            DateTime? revokedAt = null,
            long codigoPersona = CodigoPersona,
            string sistema = Sistema)
        {
            using var ctx = CreateContext(dbName);
            ctx.RefreshTokens.Add(new RefreshToken
            {
                CodigoPersona = codigoPersona,
                Sistema = sistema,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsActive = isActive,
                RevokedAt = revokedAt
            });
            ctx.SaveChanges();
        }

        /// <summary>
        /// Contexto que falla siempre al persistir, para simular un error de base de datos
        /// durante el guardado del nuevo token.
        /// </summary>
        private sealed class ThrowingSaveModelContext(DbContextOptions<ModelContext> options)
            : ModelContext(options)
        {
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
                throw new InvalidOperationException("Simulated DB failure on save.");
        }

        // -------- Caso 1: no existe token previo --------

        [Fact]
        public async Task SaveRefreshTokenAsync_WhenNoPreviousToken_AddsSingleActiveToken()
        {
            var dbName = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            using (var ctx = CreateContext(dbName))
            {
                var service = new RefreshTokenService(ctx);
                await service.SaveRefreshTokenAsync(CodigoPersona, Sistema, "hash-nuevo", expiresAt);
            }

            using var verify = CreateContext(dbName);
            var tokens = await verify.RefreshTokens
                .Where(rt => rt.CodigoPersona == CodigoPersona && rt.Sistema == Sistema)
                .ToListAsync();

            var token = Assert.Single(tokens);
            Assert.Equal("hash-nuevo", token.TokenHash);
            Assert.Equal(expiresAt, token.ExpiresAt);
            Assert.Equal("SI", token.IsActive);
            Assert.Null(token.RevokedAt);
            Assert.True(token.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
        }

        // -------- Caso 2: existe token previo --------

        [Fact]
        public async Task SaveRefreshTokenAsync_WhenPreviousTokenExists_ReplacesItKeepingSingleRow()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-viejo", DateTime.UtcNow.AddDays(1));

            var nuevoExpira = DateTime.UtcNow.AddDays(7);
            using (var ctx = CreateContext(dbName))
            {
                var service = new RefreshTokenService(ctx);
                await service.SaveRefreshTokenAsync(CodigoPersona, Sistema, "hash-nuevo", nuevoExpira);
            }

            using var verify = CreateContext(dbName);
            var tokens = await verify.RefreshTokens
                .Where(rt => rt.CodigoPersona == CodigoPersona && rt.Sistema == Sistema)
                .ToListAsync();

            var token = Assert.Single(tokens);
            Assert.Equal("hash-nuevo", token.TokenHash);
            Assert.Equal(nuevoExpira, token.ExpiresAt);
            Assert.Equal("SI", token.IsActive);
        }

        // -------- Caso 3: falla el guardado del nuevo token --------

        [Fact]
        public async Task SaveRefreshTokenAsync_WhenSaveFails_PreservesPreviousTokenAndThrows()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-viejo", DateTime.UtcNow.AddDays(1));

            // Contexto que lanza al persistir: simula fallo de BD al guardar el token nuevo.
            using (var failingCtx = new ThrowingSaveModelContext(InMemoryOptions(dbName)))
            {
                var service = new RefreshTokenService(failingCtx);

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    service.SaveRefreshTokenAsync(CodigoPersona, Sistema, "hash-nuevo", DateTime.UtcNow.AddDays(7)));
            }

            // El token anterior NO debe perderse: sigue existiendo y con su hash original.
            using var verify = CreateContext(dbName);
            var tokens = await verify.RefreshTokens
                .Where(rt => rt.CodigoPersona == CodigoPersona && rt.Sistema == Sistema)
                .ToListAsync();

            var token = Assert.Single(tokens);
            Assert.Equal("hash-viejo", token.TokenHash);
            Assert.Equal("SI", token.IsActive);
        }

        // -------- Caso 4: GetCodigoPersonaByRefreshTokenAsync --------

        [Fact]
        public async Task GetCodigoPersonaByRefreshTokenAsync_WhenValidActiveNotExpired_ReturnsCodigoPersona()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-ok", DateTime.UtcNow.AddDays(1), isActive: "SI");

            using var ctx = CreateContext(dbName);
            var service = new RefreshTokenService(ctx);

            var result = await service.GetCodigoPersonaByRefreshTokenAsync(Sistema, "hash-ok");

            Assert.Equal(CodigoPersona, result);
        }

        [Fact]
        public async Task GetCodigoPersonaByRefreshTokenAsync_WhenExpired_ReturnsNull()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-exp", DateTime.UtcNow.AddDays(-1), isActive: "SI");

            using var ctx = CreateContext(dbName);
            var service = new RefreshTokenService(ctx);

            var result = await service.GetCodigoPersonaByRefreshTokenAsync(Sistema, "hash-exp");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCodigoPersonaByRefreshTokenAsync_WhenRevoked_ReturnsNull()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-rev", DateTime.UtcNow.AddDays(1), isActive: "NO", revokedAt: DateTime.UtcNow);

            using var ctx = CreateContext(dbName);
            var service = new RefreshTokenService(ctx);

            var result = await service.GetCodigoPersonaByRefreshTokenAsync(Sistema, "hash-rev");

            Assert.Null(result);
        }

        // -------- Caso 5: RevokeRefreshTokenAsync --------

        [Fact]
        public async Task RevokeRefreshTokenAsync_MarksRevoked_WithoutPhysicalDelete()
        {
            var dbName = Guid.NewGuid().ToString();
            SeedToken(dbName, "hash-rev", DateTime.UtcNow.AddDays(1), isActive: "SI");

            using (var ctx = CreateContext(dbName))
            {
                var service = new RefreshTokenService(ctx);
                await service.RevokeRefreshTokenAsync(CodigoPersona, Sistema, "hash-rev");
            }

            using var verify = CreateContext(dbName);
            var tokens = await verify.RefreshTokens
                .Where(rt => rt.CodigoPersona == CodigoPersona && rt.Sistema == Sistema)
                .ToListAsync();

            // El registro NO se borra físicamente: sigue existiendo pero revocado.
            var token = Assert.Single(tokens);
            Assert.Equal("NO", token.IsActive);
            Assert.NotNull(token.RevokedAt);
        }
    }
}
