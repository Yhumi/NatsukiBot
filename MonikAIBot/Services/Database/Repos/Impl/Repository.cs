using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class Repository<T> : IRepository<T> where T : DBEntity
    {
        protected DbContext _context;
        protected DbSet<T> _set;

        public Repository(DbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public void Add(T obj) =>
            _set.Add(obj);

        public void AddRange(params T[] objs) =>
            _set.AddRange(objs);

        public T Get(int id) =>
            _set.FirstOrDefault(e => e.ID == id);

        public IEnumerable<T> GetAll() =>
            _set.ToList();

        public void Remove(int id) =>
            _set.Remove(this.Get(id));

        public void Remove(T obj) =>
            _set.Remove(obj);

        public void RemoveRange(params T[] objs) =>
            _set.RemoveRange(objs);

        public void Update(T obj) =>
            _set.Update(obj);

        public void UpdateRange(params T[] objs) =>
            _set.UpdateRange(objs);
    }
}
