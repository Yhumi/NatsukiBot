using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services
{
    public class MonikAIBotLogger
    {
        public void Log(string logmessage, string module)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            string type = "debug";
            Console.WriteLine($"{DateTime.Now,-19} [{type,8}] {module}: {logmessage}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
