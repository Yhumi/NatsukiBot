using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class AutoBanRepository : Repository<AutoBan>, IAutoBanRepository
    {
        public AutoBanRepository(DbContext context) : base(context)
        {
        }

        public void AddAutoBan(ulong UserID)
        {
            _set.Add(new AutoBan()
            {
                UserID = UserID
            });
            _context.SaveChanges();
        }

        public void DeleteAutoBan(ulong UserID)
        {
            AutoBan AB = _set.FirstOrDefault(x => x.UserID == UserID);
            if (AB == null) return;

            _set.Remove(AB);
            _context.SaveChanges();
        }

        public void DeleteAutoBan(int ID)
        {
            AutoBan AB = _set.FirstOrDefault(x => x.ID == ID);
            if (AB == null) return;

            _set.Remove(AB);
            _context.SaveChanges();
        }

        public AutoBan GetAutoBan(ulong UserID)
        {
            return _set.FirstOrDefault(x => x.UserID == UserID);
        }
    }
}
