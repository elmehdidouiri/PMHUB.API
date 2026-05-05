using Microsoft.EntityFrameworkCore;
using PMHUB.Application.IRepositories;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly PMHubDbContext _context;

        public RefreshTokenRepository(PMHubDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            await _context.RefreshTokens
                .FirstOrDefaultAsync(
                    r => r.TokenHash == tokenHash
                        && r.RevokedAtUtc == null
                        && r.ExpiresAtUtc > DateTime.UtcNow,
                    cancellationToken);

        public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var t in tokens)
            {
                t.RevokedAtUtc = now;
            }
        }

        public async Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default) =>
            await _context.RefreshTokens.AddAsync(entity, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        public void Revoke(RefreshToken entity) =>
            entity.RevokedAtUtc = DateTime.UtcNow;
    }
}