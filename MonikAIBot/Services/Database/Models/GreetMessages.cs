using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class GreetMessages : DBEntity
    {
        public ulong ServerID { get; set; }
        public string Message { get; set; }
    }
}
