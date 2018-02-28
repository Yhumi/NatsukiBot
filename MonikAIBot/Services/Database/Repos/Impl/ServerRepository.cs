using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    class ServerRepository : Repository<Server>, IServerRepository
    {
        public Server GetOrCreateServer(ulong ID, ulong BCh = 0)
        {
            Server toReturn;
            toReturn = _set.FirstOrDefault(x => x.ServerID == ID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new Server()
                {
                    ServerID = ID,
                    BirthdayChannelID = BCh
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public ulong GetServerBirthdayChannel(ulong ID)
        {
            Server s = GetOrCreateServer(ID);
            return s.BirthdayChannelID;
        }
    }
}
