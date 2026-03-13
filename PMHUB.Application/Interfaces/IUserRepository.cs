using PMHUB.Domain.Entities;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
namespace PMHUB.Infrastructure.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithRoleAsync(Guid id);
        Task<IEnumerable<User>> GetAllWithRoleAsync();
        Task<IEnumerable<NormalUser>> GetPendingUsersAsync();
        Task<NormalUser?> GetNormalUserByIdAsync(Guid id);
    }
}