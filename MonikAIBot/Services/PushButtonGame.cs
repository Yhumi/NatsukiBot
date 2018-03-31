using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services
{
    public class PushButtonGame
    {
        public ulong Channel { get; set; }
        public string Benefit { get; set; }
        public string Consequence { get; set; }
        public HashSet<ButtonResponse> Responses { get; set; }
    }

    public class ButtonResponse
    {
        public ulong UserID { get; set; }
        public bool Pushed { get; set; }
    }
}
