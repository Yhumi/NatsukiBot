using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    class Server : DBEntity
    {
        public ulong ServerID { get; set; }
        public ulong BirthdayChannelID { get; set; }
    }
}
