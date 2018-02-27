using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    public class OwnerOnly : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var config = (Configuration)services.GetService(typeof(Configuration));

            return Task.FromResult((config.Owners.Contains(context.User.Id) || context.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be a bot owner to run this command.")));
        }
    }
}
