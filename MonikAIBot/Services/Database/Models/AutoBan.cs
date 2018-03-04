using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class AutoBan : DBEntity
    {
        public ulong UserID { get; set; }
    }
}
