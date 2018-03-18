using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    class NSFWOnly : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var config = (Configuration)services.GetService(typeof(Configuration));

            return Task.FromResult((config.NSFWChannels.Contains(context.Channel.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("This doesn't work here.")));
        }
    }
}
