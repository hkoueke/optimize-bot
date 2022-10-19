using OptimizeBot.Model;
using System.Threading.Tasks;

namespace OptimizeBot.Contracts.Persistance
{
    public interface IUserRepository
    {
        Task<User?> GetUserAsync(long id, bool trackChanges = default);
        void CreateUser(User user);
        void UpdateUser(User user);
    }
}
