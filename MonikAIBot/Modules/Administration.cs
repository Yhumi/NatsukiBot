using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using MonikAIBot.Services;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Administration : ModuleBase
    {
        private readonly TimeSpan defaultNull = TimeSpan.FromSeconds(1);
        private readonly DiscordSocketClient _client;
        private readonly Random _random;

        public Administration(Random random, DiscordSocketClient client)
        {
            _random = random;
            _client = client;
        }

        [Command("Shutdown"), Summary("Kills the bot")]
        [Alias("die")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Shutdown()
        {
            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Bye-bye!");

            //Safely stop the bot
            await Context.Client.StopAsync();

            //Close the client
            Environment.Exit(1);
        }

        [Command("SetGame"), Summary("Sets the game the bot is currently playing")]
        [Alias("sgm")]
        [OwnerOnly]
        public async Task SetGame([Remainder] string gameName)
        {
            await _client.SetGameAsync(gameName);
        }

        [Command("SetName"), Summary("Sets the name of the bot.")]
        [Alias("sbn")]
        [OwnerOnly]
        public async Task SetName([Remainder] string newName)
        {
            try
            {
                await _client.CurrentUser.ModifyAsync(u => u.Username = newName);
            }
            catch (RateLimitedException)
            {
            }
        }

        [Command("SetImageChannel"), Summary("Set an image channel up.")]
        [Alias("SIC")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetImageChannel(IGuildChannel channel, int CooldownMinutes, int MaxPosts)
        {
            TimeSpan ts = TimeSpan.FromMinutes(CooldownMinutes);

            using (var uow = DBHandler.UnitOfWork())
            {
                if (uow.Channels.DoesChannelExist(channel.Id))
                {
                    //Get and update it cause it does
                    Channels C = uow.Channels.GetOrCreateChannel(channel.Id, ts);
                    C.CooldownTime = ts;
                    C.MaxPosts = MaxPosts;
                    C.State = true;

                    uow.Channels.Update(C);
                }
                else
                {
                    //Add it cause it doesn't
                    uow.Channels.GetOrCreateChannel(channel.Id, ts, MaxPosts, true);
                }

                //Save it all
                await uow.CompleteAsync();
            }

            await Context.Channel.SendSuccessAsync($"Enabled image post rate limiting in {channel.Name}. Cooldown: {CooldownMinutes}m | Max Posts: {MaxPosts}");
        }

        [Command("RemoveImageChannel"), Summary("Remove an image channel.")]
        [Alias("RIC")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveImageChannel(IGuildChannel channel)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                if (uow.Channels.DoesChannelExist(channel.Id))
                {
                    //Get and update it cause it does
                    Channels C = uow.Channels.GetOrCreateChannel(channel.Id, defaultNull);
                    C.State = false;

                    uow.Channels.Update(C);
                }
                else
                {
                    //Add it cause it doesn't
                    uow.Channels.GetOrCreateChannel(channel.Id, defaultNull);
                }

                //Save it all
                await uow.CompleteAsync();
            }

            await Context.Channel.SendSuccessAsync($"Disabled image post rate limiting in {channel.Name}.");
        }

        [Command("SetUserExemption"), Summary("Remove an image channel.")]
        [Alias("SUE")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetUserExemption(IGuildUser user, bool exemption)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.User.SetExemption(user.Id, exemption);

                //Save it all
                await uow.CompleteAsync();
            }

            string ex = "";
            if (exemption)
                ex = "above the law";
            else
                ex = "bound by limitations";

            await Context.Channel.SendSuccessAsync($"Set {user.Username} to be {ex}.");
        }
    }
}
