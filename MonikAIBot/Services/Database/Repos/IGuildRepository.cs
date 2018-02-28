using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IGuildRepository : IRepository<Guild>
    {
        bool IsDeleteLoggingEnabled(ulong GuildID);
        Guild GetOrCreateGuild(ulong GuildID, ulong DelChannelID = 0, bool enabled = false);
        void SetGuildDelChannel(ulong GuildID, ulong ChannelID);
        void SetGuildDelLogEnabled(ulong GuildID, bool isEnabled);
    }
}
