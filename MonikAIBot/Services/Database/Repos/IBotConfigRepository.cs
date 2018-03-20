using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IBotConfigRepository : IRepository<BotConfig>
    {
        BotConfig GetOrCreateConfig(ulong BotID);
        bool IsRotatingStatuses(ulong BotID);
        void SetRotatingStatuses(ulong BotID, bool State);
        string GetDefaultStatus(ulong BotID);
        void SetDefaultStatus(ulong BotID, string Status);
    }
}
