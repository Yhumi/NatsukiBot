using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services
{
    public class Configuration
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public ulong[] Owners { get; set; }
        public ulong BirthdayChannel { get; set; }
        public ulong[] NSFWChannels { get; set; }
        public string RconIP { get; set; }
        public ushort RconPort { get; set; }
        public string RCONPassword { get; set; }
        public ulong DefaultRole { get; set; }
    }
}
