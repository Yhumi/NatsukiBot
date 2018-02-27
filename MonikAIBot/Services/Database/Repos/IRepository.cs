using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IRepository<T> where T : DBEntity
    {
        T Get(int id);
        IEnumerable<T> GetAll();

        void Add(T obj);
        void AddRange(params T[] objs);

        void Remove(int id);
        void Remove(T obj);
        void RemoveRange(params T[] objs);

        void Update(T obj);
        void UpdateRange(params T[] objs);
    }
}
