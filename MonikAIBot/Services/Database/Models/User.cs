using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class User : DBEntity
    {
        public ulong UserID { get; set; }
        public bool IsExempt { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string MinecraftUsername { get; set; }
        public string PersonalWaifu { get; set; }
        public ulong SteamID { get; set; }
    }
}
