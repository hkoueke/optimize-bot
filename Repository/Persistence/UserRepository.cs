using Microsoft.EntityFrameworkCore;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Model;
using System.Threading.Tasks;

namespace OptimizeBot.Repository.Persistence
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(DbContext context) : base(context)
        {
        }

        public void CreateUser(User user) => Create(user);

        public async Task<User?> GetUserAsync(long id, bool trackChanges = false)
            => await FindByCondition(user => user.TelegramId == id, trackChanges).SingleOrDefaultAsync();

        public void UpdateUser(User user) => Update(user);
    }
}
