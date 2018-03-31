using Discord;
using Discord.WebSocket;
using MonikAIBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Modules.Fun.Services
{
    public class PushButtonService
    {
        private ConcurrentDictionary<ulong, Common.PushButtonHandler> ActivePBG { get; } = new ConcurrentDictionary<ulong, Common.PushButtonHandler>();

        public async Task EndGameInChannel(IGuild guild, IMessageChannel ChannelID)
        {
            PushButtonGame game = StopPBG(ChannelID.Id);

            if (game != null)
            {
                //Pushed
                int total = game.Responses.Count;
                int pushed = game.Responses.Where(x => x.Pushed == true).Count();
                int notpushed = total - pushed;

                EmbedBuilder embed = new EmbedBuilder().WithOkColour()
                    .WithTitle("Push the Button Game Results.")
                    .WithDescription($"Would you push the button if...")
                    .AddField(new EmbedFieldBuilder().WithName($"Pro").WithValue($"{game.Benefit}").WithIsInline(true))
                    .AddField(new EmbedFieldBuilder().WithName("...").WithValue("*but*").WithIsInline(true))
                    .AddField(new EmbedFieldBuilder().WithName($"Con?").WithValue($"{game.Consequence}?").WithIsInline(true))
                    .AddField(new EmbedFieldBuilder().WithName("✅ Yes").WithValue(pushed).WithIsInline(true))
                    .AddField(new EmbedFieldBuilder().WithName("❌ No").WithValue(notpushed).WithIsInline(true));

                await ChannelID.BlankEmbedAsync(embed);
            }
        }

        public PushButtonGame StopPBG(ulong channelID)
        {
            if (ActivePBG.TryRemove(channelID, out var pbg))
            {
                pbg.OnResponse -= pbg_onresp;
                pbg.End();
                return pbg.Game;
            }
            return null;
        }

        public bool StartPBG(PushButtonGame game, DiscordSocketClient client)
        {
            var ph = new Common.PushButtonHandler(game, client);
            if (ActivePBG.TryAdd(game.Channel, ph))
            {
                ph.OnResponse += pbg_onresp;
                return true;
            }
            return false;
        }

        public async Task pbg_onresp(IUserMessage msg, IGuildUser user)
        {
            var toDelete = await msg.Channel.SendSuccessAsync($"{user.Username}: Response added!");
            toDelete.DeleteAfter(5);
            try { await msg.DeleteAsync(); } catch { }
        }
    }
}
