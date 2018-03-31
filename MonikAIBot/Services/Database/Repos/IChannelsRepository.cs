using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IChannelsRepository : IRepository<Channels>
    {
        Channels GetOrCreateChannel(ulong ChannelID, TimeSpan Cooldown, int MaxPosts = 3, bool State = false, ulong vcChannel = 0);
        int GetChannelID(ulong ChannelDiscordID);
        TimeSpan GetChannelTimeout(ulong ChannelDiscordID);
        int GetMaxImages(ulong ChannelDiscordID);
        bool DoesChannelExist(ulong ChannelID);
        void SetVCChannelLink(ulong ChannelDiscordID, ulong VCChannelLink);
        ulong GetVCChannelLink(ulong ChannelDiscordID);
    }
}
