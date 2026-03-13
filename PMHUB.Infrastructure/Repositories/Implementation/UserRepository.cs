using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;

namespace PMHUB.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(PMHubDbContext context) : base(context) { }

         public async Task<User?> GetByEmailAsync(string email) =>
            await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

         public async Task<User?> GetByIdWithRoleAsync(Guid id) =>
            await _context.Users
                .OfType<NormalUser>()
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

         public async Task<IEnumerable<User>> GetAllWithRoleAsync() =>
            await _context.Users
                .OfType<NormalUser>()
                .Include(u => u.Role)
                .AsNoTracking()
                .ToListAsync();

         public async Task<IEnumerable<NormalUser>> GetPendingUsersAsync() =>
            await _context.Users
                .OfType<NormalUser>()
                .Include(u => u.Role)
                .Where(u => !u.IsApproved)
                .AsNoTracking()
                .ToListAsync();
        public async Task<NormalUser?> GetNormalUserByIdAsync(Guid id) =>
    await _context.Users
        .OfType<NormalUser>()
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Id == id);
    }

}