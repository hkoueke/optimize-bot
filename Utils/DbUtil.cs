using OptimizeBot.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OptimizeBot.Utils
{
    public static class DbUtil
    {
        private static OptimizeBotContext dbContext;

        public static async Task<T> FindOrCreateAsync<T>(Func<T, bool> predicate, T toCreate = default) where T : class
        {
            //Get user from database, and return if it exists
            T item = await FindAsync(predicate);

            if (item != null)
            {
                Program.log.Info($"Item found in DB. Returning it to CacheMgr");
                return item;
            }

            if (toCreate == null)
                throw new ArgumentNullException(nameof(toCreate));

            return await UpdateOrInsertAsync(toCreate);
        }

        public static async Task<T> FindAsync<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            using (dbContext = new())
            {
                var item = dbContext.Set<T>().SingleOrDefault(predicate);
                return await Task.FromResult(item);
            }
        }

        public static async Task<T> UpdateOrInsertAsync<T>(T item) where T : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            using (dbContext = new())
            {
                dbContext.Set<T>().Update(item);
                await dbContext.SaveChangesAsync();
                return item;
            }
        }

        public static async Task<bool> DeleteAsync<T>(T item) where T : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            using (dbContext = new())
            {
                dbContext.Set<T>().Remove(item);
                return await dbContext.SaveChangesAsync() > 0 ;
            }
        }

        public static async Task<List<T>> FindAll<T, TKey>(Func<T, bool> predicate,
                                                           Func<T, TKey> orderBy = default) where T: class
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            using (dbContext = new())
            {
                var query = dbContext.Set<T>().Where(predicate);

                if (orderBy != null) 
                    query = query.OrderBy(orderBy); 

                var res = query.ToList();
                return await Task.FromResult(res);
            }
        }
    }
}
