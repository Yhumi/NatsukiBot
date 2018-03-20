using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class BotStatusesRepository : Repository<BotStatuses>, IBotStatusesRepository
    {
        public BotStatusesRepository(DBContext context) : base(context)
        {
        }

        public BotStatuses AddStatus(string Status, bool streaming = false, string url = "")
        {
            BotStatuses toReturn;
            _set.Add(toReturn = new BotStatuses()
            {
                Status = Status,
                Streaming = streaming,
                StreamURL = url
            });
            _context.SaveChanges();

            return toReturn;
        }

        public void DeleteStatus(string Status)
        {
            BotStatuses BS = _set.FirstOrDefault(x => x.Status.ToLower() == Status.ToLower());
            if (BS == null) return;

            _set.Remove(BS);
            _context.SaveChanges();
        }

        public void DeleteStatus(int ID)
        {
            BotStatuses BS = _set.FirstOrDefault(x => x.ID == ID);
            if (BS == null) return;

            _set.Remove(BS);
            _context.SaveChanges();
        }

        public List<BotStatuses> GetBotStatuses(int page = 0)
        {
            int offset = page * 9;
            return _set.OrderBy(x => x.ID).Skip(offset).Take(9).ToList();
        }

        public BotStatuses GetStatus()
        {
            return _set.RandomItem();
        }
    }
}
