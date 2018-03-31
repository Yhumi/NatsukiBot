using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MonikAIBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Modules.Fun
{
    public class Fun : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;
        private Services.PushButtonService _pushButtonService = new Services.PushButtonService();

        public Fun(Random random, MonikAIBotLogger logger)
        {
            _random = random;
            _logger = logger;
        }

        [Command("PushTheButton")]
        public async Task PushTheButton(string benefit, string consquence, int timeout = 30)
        {
            benefit = benefit.FirstCharToLower().Trim();
            consquence = consquence.FirstCharToLower().Trim();

            PushButtonGame Game = new PushButtonGame
            {
                Channel = Context.Channel.Id,
                Benefit = benefit,
                Consequence = consquence,
                Responses = new HashSet<ButtonResponse>()
            };

            if (timeout > 300)
                timeout = 0;

            if (timeout > 0)
            {
                timeout = timeout * 1000;
            }
            else
            {
                timeout = 30000;
            }

            if (_pushButtonService.StartPBG(Game, (DiscordSocketClient)Context.Client))
            {
                EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle("Push The Button Game.").WithDescription($"Would you push the button if **{benefit}** but **{consquence}**?")
                    .WithFooter(new EmbedFooterBuilder().WithText($"Type yes/no now! You have {timeout / 1000} seconds."));

                await Context.Channel.BlankEmbedAsync(embed);
                await Task.Delay(timeout);
                await _pushButtonService.EndGameInChannel(Context.Guild, Context.Channel);
            }
        }
    }
}
