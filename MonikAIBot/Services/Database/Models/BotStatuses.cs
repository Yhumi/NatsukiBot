using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class BotStatuses : DBEntity
    {
        public string Status { get; set; }
        public bool Streaming { get; set; }
        public string StreamURL { get; set; }
    }
}
