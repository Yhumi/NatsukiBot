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
        public ulong DefaultRole { get; set; }
        public string SteamAPIKey { get; set; }
        public bool Shutdown { get; set; }
    }
}
