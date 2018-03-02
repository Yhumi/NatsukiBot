using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class Guild : DBEntity
    {
        public ulong GuildID { get; set; }
        public ulong DeleteLogChannel { get; set; }
        public bool DeleteLogEnabled { get; set; }
        public ulong GreetMessageChannel { get; set; }
        public bool GreetMessageEnabled { get; set; }
        public ulong VCNotifyChannel { get; set; }
        public bool VCNotifyEnable { get; set; }
    }
}
