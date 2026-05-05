using PMHUB.Domain.Entities;

namespace PMHUB.Application.IRepositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Revoke(RefreshToken entity);
    }
}