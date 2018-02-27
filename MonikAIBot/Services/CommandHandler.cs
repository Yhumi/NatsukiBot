using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly Configuration _config;
        private readonly MonikAIBotLogger _logger;
        private List<AsyncLazy<IDMChannel>> _ownerChannels;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            Configuration config,
            IServiceProvider provider,
            MonikAIBotLogger logger)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _logger = logger;
            _ownerChannels = new List<AsyncLazy<IDMChannel>>();

            //Set up DM channels for owners
            foreach (ulong ownerID in _config.Owners)
            {
                _ownerChannels.Add(new AsyncLazy<IDMChannel>(async () => await _discord.GetUser(ownerID).GetOrCreateDMChannelAsync()));
            }

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            var context = new SocketCommandContext(_discord, msg);

            int argPos = 0;
            if (msg.HasStringPrefix(_config.Prefix, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (!result.IsSuccess && !(result.Error.ToString() == "UnknownCommand"))
                    await context.Channel.SendErrorAsync(result.ToString());

                return;
            }
        }

        private async Task DMHandling(SocketCommandContext context)
        {
            foreach (var OwnerChannel in _ownerChannels)
            {
                IDMChannel ownerChannel = await OwnerChannel;
                await ownerChannel.SendSuccessAsync($"DM from [{context.User.Username}] | {context.User.Id}", context.Message.Content);
            }
        }
    }
}
