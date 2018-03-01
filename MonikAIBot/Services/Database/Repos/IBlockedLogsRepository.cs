using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IBlockedLogsRepository : IRepository<BlockedLogs>
    {
        void AddBlockedLog(ulong serverID, string blockedLog);
        void DeleteBlockedLog(int ID);
        void DeleteBlockedLog(ulong serverID, string blockedLog);
        List<BlockedLogs> GetServerBlockedLogs(ulong serverID);
    }
}
