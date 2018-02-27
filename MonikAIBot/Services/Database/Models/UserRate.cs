using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Models
{
    public class UserRate : DBEntity
    {
        public int UserDBID { get; set; }
        public int ChanneDBID { get; set; }
        public DateTime LastTrackingTime { get; set; }
        public int PostsSinceTracking { get; set; }
    }
}
