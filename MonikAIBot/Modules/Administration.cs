using CoreRCON;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using MonikAIBot.Services;
using MonikAIBot.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private readonly Configuration _config;

        public Administration(Random random, DiscordSocketClient client, RCON rcon, Configuration config)
        {
            _random = random;
            _client = client;
            _rcon = rcon;
            _config = config;
        }

        [Command("Shutdown"), Summary("Kills the bot")]
        [Alias("die")]
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
            var user = (IGuildUser)Context.User;
            var vc = user.VoiceChannel;

            if (vc == null || vc.GuildId != user.GuildId)
            {
                await Context.Channel.SendErrorAsync("You must be in a voice channel.");
                return;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Channels.SetVCChannelLink(vc.Id, channel.Id);
            }

            await Context.Channel.SendSuccessAsync($"Set {vc.Name}'s VC Notify channel to: {channel.Name}");
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
            if (_rcon == null)
            {
                await Context.Channel.SendErrorAsync("No minecraft server specified.");
                return;
            }

            try
            {
                await _rcon.ConnectAsync();
                await _rcon.SendCommandAsync(command);
                await Context.Channel.SendSuccessAsync($"Command \"{command}\" executed successfully.");
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("Command failed.");
            }
        }

        [Command("ResetMinecraftName")]
        [Alias("ResetMC")]
        [OwnerOnly]
        public async Task ResetMinecraftName(IGuildUser user)
        {
            if (_rcon == null)
            {
                await Context.Channel.SendErrorAsync("No minecraft server specified.");
                return;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                string GetMCName = uow.User.GetMinecraftUsername(user.Id);
                if (GetMCName == "none") return;

                //Now unwhitelist that name
                try
                {
                    await _rcon.ConnectAsync();
                    await _rcon.SendCommandAsync($"whitelist remove {GetMCName}");
                }
                catch (Exception)
                {
                    return;
                }

                //Now set their MC name to none
                uow.User.SetMinecraftUsername(user.Id, "none");

                //Now we're done
                await Context.Channel.SendSuccessAsync("Reset the Minecraft name for that dummy~");
            }
        }

        [Command("AddWaifu")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddWaifu([Remainder] string waifu)
        {
            bool success = false;
            using (var uow = DBHandler.UnitOfWork())
            {
                success = uow.Waifus.AddWaifu(waifu);
            }

            if (success)
            {
                await Context.Channel.SendSuccessAsync($"Added waifu: {waifu}");
                return;
            }

            await Context.Channel.SendErrorAsync($"Waifu already exists.");
        }

        [Command("ListWaifus")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListWaifus(int page = 0)
        {
            if (page != 0)
                page -= 1;

            List<Waifus> Ws;
            using (var uow = DBHandler.UnitOfWork())
            {
                Ws = uow.Waifus.GetWaifus(page);
            }

            if (!Ws.Any())
            {
                await Context.Channel.SendErrorAsync($"No Waifus found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle($"Waifus").WithFooter(efb => efb.WithText($"Page: {page + 1}"));

            string desc = "";

            foreach (Waifus w in Ws)
            {
                desc += $"{w.ID}. {w.Waifu}\n";
            }

            embed.WithDescription(desc);

            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("DeleteWaifu")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteWaifu([Remainder] string waifu)
        {
            bool deleted = false;
            using (var uow = DBHandler.UnitOfWork())
            {
                deleted = uow.Waifus.DeleteWaifu(waifu);
            }

            if (deleted)
                await Context.Channel.SendSuccessAsync($"Deleted waifu: {waifu}");
        }

        [Command("DeleteWaifu")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteWaifu(int ID)
        {
            bool deleted = false;
            using (var uow = DBHandler.UnitOfWork())
            {
                deleted = uow.Waifus.DeleteWaifu(ID);
            }

            if (deleted)
                await Context.Channel.SendSuccessAsync($"Deleted waifu with ID: {ID}");
        }

        [Command("SearchWaifu")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SearchWaifu([Remainder] string waifu)
        {
            Waifus w;
            using (var uow = DBHandler.UnitOfWork())
            {
                w = uow.Waifus.SearchWaifus(waifu);
            }

            if (w == null)
            {
                await Context.Channel.SendErrorAsync("Waifu not found.");
                return;
            }

            await Context.Channel.SendSuccessAsync($"Waifu Found! #{w.ID}", $"{w.Waifu}");
        }

        [Command("AddStatus")]
        [OwnerOnly]
        public async Task AddStatus([Remainder] string status)
        {
            if (String.IsNullOrWhiteSpace(status)) return;
            if (status.Length > 24) return;

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotStatuses.AddStatus(status);
            }

            await Context.Channel.SendSuccessAsync($"Added status: {status}");
        }

        [Command("AddStream")]
        [OwnerOnly]
        public async Task AddStream(string url, [Remainder] string status)
        {
            if (String.IsNullOrWhiteSpace(status)) return;
            if (status.Length > 24) return;

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotStatuses.AddStatus(status, true, url);
            }

            await Context.Channel.SendSuccessAsync($"Added stream: {status} | url: {url}");
        }

        [Command("DeleteStatus")]
        [OwnerOnly]
        public async Task DeleteStatus(int ID)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotStatuses.DeleteStatus(ID);
            }

            await Context.Channel.SendSuccessAsync($"Deleted stauts: {ID}");
        }

        [Command("DeleteStatus")]
        [OwnerOnly]
        public async Task DeleteStatus([Remainder] string status)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotStatuses.DeleteStatus(status);
            }

            await Context.Channel.SendSuccessAsync($"Deleted stauts: {status}");
        }

        [Command("ListStatuses")]
        [OwnerOnly]
        public async Task ListStatus(int page = 0)
        {
            if (page != 0)
                page -= 1;

            List<BotStatuses> BS;
            using (var uow = DBHandler.UnitOfWork())
            {
                BS = uow.BotStatuses.GetBotStatuses(page);
            }

            if (!BS.Any())
            {
                await Context.Channel.SendErrorAsync($"No Bot Statuses found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle($"BotStatuses").WithFooter(efb => efb.WithText($"Page: {page + 1}"));

            string desc = "";

            foreach (BotStatuses B in BS)
            {
                desc += $"{B.ID}. {B.Status} | Is Stream: {B.Streaming.ToString()} | Url: {B.StreamURL}\n";
            }

            embed.WithDescription(desc);

            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("SetDefaultStatus")]
        [OwnerOnly]
        public async Task SetDefaultStatus([Remainder] string status)
        {
            if (String.IsNullOrWhiteSpace(status)) return;
            if (status.Length > 24) return;

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotConfig.SetDefaultStatus(Context.Client.CurrentUser.Id, status);
            }

            await Context.Channel.SendSuccessAsync($"Set default status: {status}");
            await SetGame(status);
        }

        [Command("SetRotatingStatus")]
        [OwnerOnly]
        public async Task SetRotatingStatus(bool rs)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.BotConfig.SetRotatingStatuses(Context.Client.CurrentUser.Id, rs);
            }

            await Context.Channel.SendSuccessAsync($"Set rotating statuses to: {rs.ToString()}");
        }

        [Command("ResetPersonalWaifu")]
        [OwnerOnly]
        public async Task ResetPersonalWaifu(IGuildUser user)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.User.SetPersonalWaifu(user.Id, "");
            }

            await Context.Channel.SendSuccessAsync($"Reset pwaifu for {user.NicknameUsername()}");
        }

        [Command("save"), Summary("Saves a given user's role")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Save([Summary("Discord User to Save")] IGuildUser user, bool verbose = true)
        {
            //User and Server ID
            ulong uID = user.Id;
            ulong sID = Context.Guild.Id;

            //Server directory path
            string directory = @"data/characters/" + sID;

            //Create the directory for the server if it does not exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //Path to storage
            string filePath = directory + @"/" + uID + ".chr";

            //User's roles
            List<ulong> roles = new List<ulong>(user.RoleIds);

            //String roles
            List<string> rolesString = new List<string>(roles.Count);

            //If they've only got @everyone and newbies
            if (roles.Count == 2 && roles.Contains(_config.DefaultRole))
            {
                if (verbose)
                    await Context.Channel.SendErrorAsync("I don't think you need to do that, dummy.");
                return;
            }

            //So it can be ported over easily
            foreach (ulong rID in roles)
            {
                rolesString.Add($"{rID}");
            }

            //Serialize JSON
            string json = JsonConvert.SerializeObject(rolesString);

            //Write json to file (overwriting)
            File.WriteAllText(filePath, json);

            //Now tell the user we did it! Yay
            if (verbose)
                await Context.Channel.SendSuccessAsync("Saved roles for " + user.Mention);
        }

        [Command("Restore"), Summary("Restores a user's roles")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Restore([Summary("Discord User to Restore")] IGuildUser user)
        {
            var sID = Context.Guild.Id;
            var uID = user.Id;

            //Server directory path
            string directory = @"data/characters/" + sID;

            //Path to storage
            string filePath = directory + @"/" + uID + ".chr";

            //Woah hold up there, they've not had roles saved
            if (!File.Exists(filePath)) return;

            //Get the roles
            List<string> SavedRoles = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));

            //Collection of roles
            List<IRole> roles = new List<IRole>();

            //Sort out the roles now
            foreach (string id in SavedRoles)
            {
                IRole role = Context.Guild.GetRole(ulong.Parse(id));
                if (!(role == Context.Guild.EveryoneRole))
                    roles.Add(role);
            }

            if (_config.DefaultRole != 0)
            {
                await user.RemoveRoleAsync(Context.Guild.GetRole(_config.DefaultRole));
            }

            //Add the roles they deserve
            await user.AddRolesAsync(roles);

            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Restored roles for " + user.Mention);
        }

        [Command("SaveAll"), Summary("Saves all user's roles")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveAll()
        {
            List<IGuildUser> users = new List<IGuildUser>(await Context.Guild.GetUsersAsync());

            foreach (IGuildUser user in users)
            {
                await Save(user, false);
            }

            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Saved all users");
        }

        [Command("Delete")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Delete(IGuildUser user)
        {
            var sID = Context.Guild.Id;
            var uID = user.Id;

            //Server directory path
            string directory = @"data/characters/" + sID;

            //Path to storage
            string filePath = directory + @"/" + uID + ".chr";

            //Woah hold up there, they've not had roles saved
            if (!File.Exists(filePath)) return;

            File.Delete(filePath);

            await Context.Channel.SendSuccessAsync($"{user.NicknameUsername()}.chr ({uID}.chr) deleted.");
        }

        [Command("Delete")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Delete(ulong user)
        {
            var sID = Context.Guild.Id;
            var uID = user;

            //Server directory path
            string directory = @"data/characters/" + sID;

            //Path to storage
            string filePath = directory + @"/" + uID + ".chr";

            //Woah hold up there, they've not had roles saved
            if (!File.Exists(filePath)) return;

            File.Delete(filePath);

            await Context.Channel.SendSuccessAsync($"{uID}.chr deleted.");
        }
    } 
}
