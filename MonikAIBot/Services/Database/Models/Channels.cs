using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class Channels : DBEntity
    {
        public ulong ChannelID { get; set; }
        public bool State { get; set; }
        public TimeSpan CooldownTime { get; set; } 
        public int MaxPosts { get; set; }
    }
}
