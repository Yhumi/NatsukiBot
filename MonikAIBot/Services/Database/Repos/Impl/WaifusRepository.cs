using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class WaifusRepository : Repository<Waifus>, IWaifusRepository
    {
        public WaifusRepository(DBContext context) : base(context)
        {
        }

        public bool AddWaifu(string waifu)
        {
            if (_set.FirstOrDefault(x => x.Waifu.ToLower() == waifu.ToLower()) != null) return false;

            _set.Add(new Waifus()
            {
                Waifu = waifu
            });
            _context.SaveChanges();

            return true;
        }

        public bool DeleteWaifu(string waifu)
        {
            Waifus w = _set.FirstOrDefault(x => x.Waifu.ToLower() == waifu.ToLower());
            if (w == null) return false;

            _set.Remove(w);
            _context.SaveChanges();
            return true;
        }

        public bool DeleteWaifu(int ID)
        {
            Waifus w = _set.FirstOrDefault(x => x.ID == ID);
            if (w == null) return false;

            _set.Remove(w);
            _context.SaveChanges();
            return true;
        }

        public string GetRandomWaifu()
        {
            return _set.RandomItem().Waifu;
        }

        public List<Waifus> GetWaifus(int page = 0)
        {
            int offset = page * 9;
            return _set.OrderBy(x => x.ID).Skip(offset).Take(9).ToList();
        }

        public Waifus SearchWaifus(string waifu)
        {
            return _set.FirstOrDefault(x => x.Waifu.ToLower() == waifu.ToLower());
        }
    }
}
