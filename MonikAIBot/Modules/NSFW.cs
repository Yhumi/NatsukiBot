using Discord.Commands;
using MonikAIBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Modules
{
    [RequireContext(ContextType.Guild)]
    [NSFWOnly]
    public class NSFW : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;

        public NSFW(Random random, MonikAIBotLogger logger)
        {
            _random = random;
            _logger = logger;
        }
    }
}
