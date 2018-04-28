using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    public class DisconnectHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _provider;
        private readonly Configuration _config;
        private readonly MonikAIBotLogger _logger;

        public DisconnectHandler(DiscordSocketClient discord,
            Configuration config,
            IServiceProvider provider,
            MonikAIBotLogger logger)
        {
            _discord = discord;
            _config = config;
            _provider = provider;
            _logger = logger;

            _discord.Disconnected += _discord_Disconnected;
        }

        private async Task _discord_Disconnected(Exception arg)
        {
            if (_config.Shutdown)
                return;

            await _discord.StopAsync();

            int attempts = 0;

            while (_discord.ConnectionState == Discord.ConnectionState.Disconnected && attempts < 10)
            {
                await Task.Delay(30000);
                attempts++;
                _logger.Log("Attempting to reconnect.", "Disconnected");
                await _discord.StartAsync();
            }

            if (_discord.ConnectionState == Discord.ConnectionState.Disconnected)
            {
                _logger.Log("Error.", "Disconnected");
            }

            return;
        }
    }
}
