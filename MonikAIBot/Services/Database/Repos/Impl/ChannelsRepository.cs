using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class ChannelsRepository : Repository<Channels>, IChannelsRepository
    {
        public ChannelsRepository(DbContext context) : base(context)
        {
        }

        public bool DoesChannelExist(ulong ChannelID)
        {
            if (_set.FirstOrDefault(x => x.ChannelID == ChannelID) == null) return false;
            return true;
        }

        public Channels GetOrCreateChannel(ulong ChannelID, TimeSpan Cooldown, int MaxPosts = 3, bool State = false, ulong vcChannel = 0)
        {
            Channels toReturn;

            toReturn = _set.FirstOrDefault(x => x.ChannelID == ChannelID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new Channels()
                {
                    ChannelID = ChannelID,
                    State = State,
                    CooldownTime = Cooldown,
                    MaxPosts = MaxPosts,
                    VoiceChannelLink = vcChannel
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public int GetChannelID(ulong ChannelDiscordID)
        {
            Channels C = GetOrCreateChannel(ChannelDiscordID, TimeSpan.FromMinutes(5));
            return C.ID;
        }

        public TimeSpan GetChannelTimeout(ulong ChannelDiscordID)
        {
            Channels C = GetOrCreateChannel(ChannelDiscordID, TimeSpan.FromMinutes(5));
            return C.CooldownTime;
        }

        public int GetMaxImages(ulong ChannelDiscordID)
        {
            Channels C = GetOrCreateChannel(ChannelDiscordID, TimeSpan.FromMinutes(5));
            return C.MaxPosts;
        }

        public void SetVCChannelLink(ulong ChannelDiscordID, ulong VCChannelLink)
        {
            Channels C = GetOrCreateChannel(ChannelDiscordID, TimeSpan.FromMinutes(5));
            C.VoiceChannelLink = VCChannelLink;

            _set.Update(C);
            _context.SaveChanges();
        }

        public ulong GetVCChannelLink(ulong ChannelDiscordID)
        {
            Channels C = GetOrCreateChannel(ChannelDiscordID, TimeSpan.FromMinutes(5));
            return C.VoiceChannelLink;
        }
    }
}
