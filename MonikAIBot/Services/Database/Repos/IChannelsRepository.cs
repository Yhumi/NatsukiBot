using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IChannelsRepository : IRepository<Channels>
    {
        Channels GetOrCreateChannel(ulong ChannelID, TimeSpan Cooldown, int MaxPosts = 3, bool State = false);
        int GetChannelID(ulong ChannelDiscordID);
        TimeSpan GetChannelTimeout(ulong ChannelDiscordID);
        int GetMaxImages(ulong ChannelDiscordID);
        bool DoesChannelExist(ulong ChannelID);
    }
}
