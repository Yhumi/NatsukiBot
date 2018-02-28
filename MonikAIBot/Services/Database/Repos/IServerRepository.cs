using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    interface IServerRepository : IRepository<Server>
    {
        Server GetOrCreateServer(ulong ID, ulong BCh = 0);
        ulong GetServerBirthdayChannel(ulong ID);
    }
}
