using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class BlockedLogs : DBEntity
    {
        public ulong ServerID { get; set; }
        public string BlockedString { get; set; }
    }
}
