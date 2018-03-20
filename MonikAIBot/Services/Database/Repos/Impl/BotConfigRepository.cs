using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class BotConfigRepository : Repository<BotConfig>, IBotConfigRepository
    {
        public BotConfigRepository(DBContext context) : base(context)
        {
        }

        public string GetDefaultStatus(ulong BotID)
        {
            BotConfig CF = GetOrCreateConfig(BotID);
            return CF.DefaultStatus;
        }

        public BotConfig GetOrCreateConfig(ulong BotID)
        {
            BotConfig toReturn;
            toReturn = _set.FirstOrDefault(x => x.BotID == BotID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new BotConfig()
                {
                    BotID = BotID,
                    AutoRotateStatuses = false,
                    DefaultStatus = ""
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public bool IsRotatingStatuses(ulong BotID)
        {
            BotConfig CF = GetOrCreateConfig(BotID);
            return CF.AutoRotateStatuses;
        }

        public void SetDefaultStatus(ulong BotID, string Status)
        {
            BotConfig CF = GetOrCreateConfig(BotID);
            CF.DefaultStatus = Status;

            _set.Update(CF);
            _context.SaveChanges();
        }

        public void SetRotatingStatuses(ulong BotID, bool State)
        {
            BotConfig CF = GetOrCreateConfig(BotID);
            CF.AutoRotateStatuses = State;

            _set.Update(CF);
            _context.SaveChanges();
        }
    }
}
