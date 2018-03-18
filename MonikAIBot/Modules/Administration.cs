using CoreRCON;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using MonikAIBot.Services;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private readonly RCON _rcon;

        public Administration(Random random, DiscordSocketClient client, RCON rcon)
        {
            _random = random;
            _client = client;
            _rcon = rcon;
        }

        [Command("Shutdown"), Summary("Kills the bot")]
        [Alias("die")]
        [RequireContext(ContextType.DM)]
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

        [Command("Say"), Summary("Makes the bot say shit")]
        [OwnerOnly]
        public async Task Say(IMessageChannel ch, [Remainder] string content)
        {
            await ch.SendMessageAsync(content);
        }

        [Command("Say"), Summary("Makes the bot say shit")]
        [OwnerOnly]
        public async Task Say(IUser u, [Remainder] string content)
        {
            await u.SendMessageAsync(content);
        }

        [Command("Say"), Summary("Makes the bot say shit")]
        [OwnerOnly]
        public async Task Say(string location, [Remainder] string content)
        {
            string loc = String.Empty;
            if (location.StartsWith("c") || location.StartsWith("C"))
            {
                location = location.Substring(1);
                ulong channelID;
                if (!UInt64.TryParse(location, out channelID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                IMessageChannel channel = (IMessageChannel)await Context.Client.GetChannelAsync(channelID);
                await channel.SendMessageAsync(content);

                loc = channel.Name;
            }

            if (location.StartsWith("u") || location.StartsWith("U"))
            {
                location = location.Substring(1);
                ulong userID;
                if (!UInt64.TryParse(location, out userID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                IUser User = await Context.Client.GetUserAsync(userID);
                IDMChannel iDMChannel = await User.GetOrCreateDMChannelAsync();
                await iDMChannel.SendMessageAsync(content);

                loc = User.Username + "#" + User.Discriminator;
            }

            if (location.StartsWith("g") || location.StartsWith("G"))
            {
                location = location.Substring(1);
                ulong serverID;
                if (!UInt64.TryParse(location, out serverID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                IGuild Guild = await Context.Client.GetGuildAsync(serverID);
                IMessageChannel channel = await Guild.GetDefaultChannelAsync();
                await channel.SendMessageAsync(content);

                loc = Guild.Name + "/" + channel.Name;
            }

            await Context.Channel.SendSuccessAsync($"Message sent to {loc}");
        }

        [Command("SetUserBirthday"), Summary("Adds a user's birthday.")]
        [Alias("SetBday", "Birthday", "SetDOB")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddUserBirthday(IGuildUser user, string birthday)
        {
            DateTime dt;
            if (!DateTime.TryParseExact(birthday, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                await Context.Channel.SendErrorAsync("That is not a valid date.");
                return;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.User.SetUserBirthday(user.Id, dt);
            }

            await Context.Channel.SendSuccessAsync($"Added Birthday for {user.Username}.");
        }

        [Command("TestBirthdays"), Summary("Tests the birthdays")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task TestBirthdays()
        {
            List<User> todaysBirthdays = GetBirthdays();

            if (todaysBirthdays != null)
            {
                foreach (User uBd in todaysBirthdays)
                {
                    IUser user = _client.GetUser(uBd.UserID);
                    var age = DateTime.Today.Year - uBd.DateOfBirth.Year;
                    await Context.Channel.SendMessageAsync($"Testing Birthdays. {user.Username} is celebrating their birthday, they are {age}!");
                }
            }
        }

        [Command("MinValAllBDays")]
        [OwnerOnly]
        public async Task MinValAllBDays([Remainder] string s)
        {
            //Just to stop somehow activating the command by mistake
            if (s != "validation") return;

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.User.SetupAllBirthdays();
            }

            await Context.Channel.SendSuccessAsync($"Done.");
        }

        private List<User> GetBirthdays()
        {
            var curDate = DateTime.Now;
            List<User> userBirthdays;

            using (var uow = DBHandler.UnitOfWork())
            {
                userBirthdays = uow.User.GetAllBirthdays(curDate);
            }

            return userBirthdays;
        }

        [Command("ShowBirthdays")]
        [Alias("SBD")]
        [OwnerOnly]
        public async Task ShowBirthdays(int page = 0, [Remainder] string f = @"dd'/'MM'/'yyyy")
        {
            if (page != 0)
                page -= 1;

            List<User> Users;
            using (var uow = DBHandler.UnitOfWork())
            {
                Users = uow.User.GetNine(page);
            }

            if (!Users.Any())
            {
                await Context.Channel.SendErrorAsync($"No users found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle($"Birthdays").WithFooter(efb => efb.WithText($"Page: {page + 1} | FormatString: {f}"));

            foreach (User u in Users)
            {
                IGuildUser user = await Context.Guild.GetUserAsync(u.UserID);
                string username = user?.Username ?? u.UserID.ToString();
                EmbedFieldBuilder efb = new EmbedFieldBuilder().WithName(username).WithValue(u.DateOfBirth.ToString(f)).WithIsInline(true);

                embed.AddField(efb);
            }

            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("SetDeletedLogChannel")]
        [Alias("SDLC")]
        [OwnerOnly]
        public async Task SetDeletedLogChannel(IGuildChannel channel)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetGuildDelChannel(Context.Guild.Id, channel.Id);
            }

            await Context.Channel.SendSuccessAsync($"Set guild's delete log channel to: {channel.Name}");
        }

        [Command("SetDeleteLog")]
        [Alias("SDL")]
        [OwnerOnly]
        public async Task SetDeleteLog(bool t)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetGuildDelLogEnabled(Context.Guild.Id, t);
            }

            string ret = "";
            if (t)
                ret = "Turned on deletion logging for this server.";
            else
                ret = "Turned off deletion logging for this server.";

            await Context.Channel.SendSuccessAsync(ret);
        }

        [Command("IsDeleteLoggingEnabled")]
        [Alias("IDLE")]
        [OwnerOnly]
        public async Task IsDeleteLoggingEnabled()
        {
            Guild G = null;
            string Status = "Disabled.";
            
            using (var uow = DBHandler.UnitOfWork())
            {
                G = uow.Guild.GetOrCreateGuild(Context.Guild.Id);
            }

            if (G.DeleteLogEnabled)
                Status = "Eanbled.";

            await Context.Channel.SendSuccessAsync("Logging", $"State: {Status} | ChannelID: {G.DeleteLogChannel}");
        }

        [Command("Greet")]
        [OwnerOnly]
        public async Task Greet(bool t)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetGuildGreetings(Context.Guild.Id, t);
            }

            string ret = "Turned off greetings for this server.";
            if (t)
                ret = "Turned on greetings for this server.";

            await Context.Channel.SendSuccessAsync(ret);
        }

        [Command("SetGreetChannel")]
        [Alias("SGRC")]
        [OwnerOnly]
        public async Task SetGreetChannel(IGuildChannel channel)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetGuildGreetChannel(Context.Guild.Id, channel.Id);
            }

            await Context.Channel.SendSuccessAsync($"Set guild's greeings channel to: {channel.Name}");
        }

        [Command("AddGreeting")]
        [Alias("AGR")]
        [OwnerOnly]
        public async Task AddGreeting([Remainder] string message)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.GreetMessages.CreateGreatMessage(Context.Guild.Id, message);
            }

            await Context.Channel.SendSuccessAsync("Added greet message!");
        }

        [Command("DeleteGreeting")]
        [Alias("DGR")]
        [OwnerOnly]
        public async Task DeleteGreeting([Remainder] string message)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.GreetMessages.DeleteGreetMessage(message, Context.Guild.Id);
            }

            await Context.Channel.SendSuccessAsync("Deleted greet message!");
        }

        [Command("DeleteGreeting")]
        [Alias("DGR")]
        [OwnerOnly]
        public async Task DeleteGreeting(int ID)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.GreetMessages.DeleteGreetMessage(ID);
            }

            await Context.Channel.SendSuccessAsync("Deleted greet message!");
        }

        [Command("ShowGreetings")]
        [Alias("SG")]
        [OwnerOnly]
        public async Task ShowGreetings(int page = 0)
        {
            if (page != 0)
                page -= 1;

            List<GreetMessages> GMs;
            using (var uow = DBHandler.UnitOfWork())
            {
                GMs = uow.GreetMessages.FetchGreetMessages(Context.Guild.Id, page);
            }

            if (!GMs.Any())
            {
                await Context.Channel.SendErrorAsync($"No GMs found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle($"GMs").WithFooter(efb => efb.WithText($"Page: {page + 1}"));

            foreach (GreetMessages gm in GMs)
            {
                EmbedFieldBuilder efb = new EmbedFieldBuilder().WithName(gm.ID.ToString()).WithValue(gm.Message);

                embed.AddField(efb);
            }

            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("AddBL")]
        [Alias("ABL")]
        [OwnerOnly]
        public async Task AddBL([Remainder] string s)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BlockedLogs.AddBlockedLog(Context.Guild.Id, s);
            }

            await Context.Channel.SendSuccessAsync("Added blocked log filter!");
        }

        [Command("DeleteBL")]
        [Alias("DBL")]
        [OwnerOnly]
        public async Task DeleteBL([Remainder] string message)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BlockedLogs.DeleteBlockedLog(Context.Guild.Id, message);
            }

            await Context.Channel.SendSuccessAsync("Deleted blocked log filter!");
        }

        [Command("DeleteBL")]
        [Alias("DBL")]
        [OwnerOnly]
        public async Task DeleteBL(int ID)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BlockedLogs.DeleteBlockedLog(ID);
            }

            await Context.Channel.SendSuccessAsync("Deleted blocked log filter!");
        }

        [Command("VCNotify")]
        [Alias("VCN")]
        [OwnerOnly]
        public async Task VCNotify(bool t)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetVCNotifyEnabled(Context.Guild.Id, t);
            }

            string ret = "Turned off VC notification for this server.";
            if (t)
                ret = "Turned on VC notification for this server.";

            await Context.Channel.SendSuccessAsync(ret);
        }

        [Command("SetVCNotifyChannel")]
        [Alias("SVCNC")]
        [OwnerOnly]
        public async Task SetVCNotifyChannel(IGuildChannel channel)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Guild.SetVCNotifyChannel(Context.Guild.Id, channel.Id);
            }

            await Context.Channel.SendSuccessAsync($"Set guild's VC Notify channel to: {channel.Name}");
        }

        [Command("AddAutoBan")]
        [Alias("AAB")]
        [OwnerOnly]
        public async Task AddAutoBan(ulong ID)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.AutoBan.AddAutoBan(ID);
            }

            await Context.Channel.SendSuccessAsync($"Added ID to AutoBan List: {ID}");
        }

        [Command("RemoveAutoBan")]
        [Alias("RAB")]
        [OwnerOnly]
        public async Task RemoveAutoBan(ulong ID)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.AutoBan.DeleteAutoBan(ID);
            }

            await Context.Channel.SendSuccessAsync($"Removed ID from AutoBan List: {ID}");
        }

        [Command("ExecuteMCRCON")]
        [Alias("ExecuteRCON", "MC")]
        [OwnerOnly]
        public async Task ExecuteMCRCON([Remainder] string command)
        {
            try
            {
                await _rcon.ConnectAsync();
                await _rcon.SendCommandAsync(command);
                await Context.Channel.SendSuccessAsync($"Command \"{command}\" executed successfully.");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync("Command failed.");
            }
        }
    }
}
