using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class BlockedLogsRepository : Repository<BlockedLogs>, IBlockedLogsRepository
    {
        public BlockedLogsRepository(DbContext context) : base(context)
        {
        }

        public void AddBlockedLog(ulong serverID, string blockedLog)
        {
            _set.Add(new BlockedLogs()
            {
                ServerID = serverID,
                BlockedString = blockedLog
            });
            _context.SaveChanges();
        }

        public void DeleteBlockedLog(int ID)
        {
            BlockedLogs BL = _set.FirstOrDefault(x => x.ID == ID);
            if (BL == null) return;

            _set.Remove(BL);
            _context.SaveChanges();
        }

        public void DeleteBlockedLog(ulong serverID, string blockedLog)
        {
            BlockedLogs BL = _set.FirstOrDefault(x => x.ServerID == serverID && x.BlockedString.ToLower() == blockedLog.ToLower());
            if (BL == null) return;

            _set.Remove(BL);
            _context.SaveChanges();
        }

        public List<BlockedLogs> GetServerBlockedLogs(ulong serverID)
        {
            List<BlockedLogs> BLs = _set.Where(x => x.ServerID == serverID).ToList();
            return BLs;
        }
    }
}
