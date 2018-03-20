using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class BotConfig : DBEntity
    {
        public ulong BotID { get; set; }
        public bool AutoRotateStatuses { get; set; }
        public string DefaultStatus { get; set; }
    }
}
