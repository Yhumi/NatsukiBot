using CoreRCON;
using Discord;
using Discord.Commands;
using MonikAIBot.Services;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using MonikAIBot.Services.APIModels;

namespace MonikAIBot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Interactions : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;
        private readonly RCON _rcon;
        private readonly Configuration _config;
        private readonly Cooldowns _cooldowns;

        //API Stuff
        private readonly string APIUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={tags}+-comic+-photo+rating%3asafe+-webm&pid={page}&limit={limit}";
        private int limit = 100;

        //Steam API Stuff
        private readonly string SteamAPIUrl = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={key}&steamid={id}&include_appinfo=1";
        private readonly string VanityURL = "https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={key}&vanityurl={url}";

        public Interactions(Random random, MonikAIBotLogger logger, RCON rcon, Configuration config, Cooldowns CD)
        {
            _random = random;
            _logger = logger;
            _rcon = rcon;
            _config = config;
            _cooldowns = CD;

            _cooldowns.GetOrSetupCommandCooldowns("Interactions");
        }

        [Command("PickRandomGame"), Summary("Picks a random game from the user's steam library.")]
        [Alias("SteamRandom")]
        public async Task PickRandomGame([Remainder] string options = null)
        {
            if (_config.SteamAPIKey == "" || _config.SteamAPIKey == null)
            {
                await Context.Channel.SendErrorAsync("Please set a valid Steam API Key!");
                return;
            }

            var userID = Context.User.Id;
            ulong SteamID = 0;

            using (var uow = DBHandler.UnitOfWork())
            {
                SteamID = uow.User.GetSteamID(userID);
            }

            if (SteamID == 0)
            {
                await Context.Channel.SendErrorAsync("You haven't set your SteamID yet dummy!");
                return;
            }

            var completeURL = SteamAPIUrl.Replace("{key}", _config.SteamAPIKey).Replace("{id}", SteamID.ToString());

            var response = await APIResponse(completeURL);
            var responseArray = JsonConvert.DeserializeObject<OwnedGamesResultContainer>(response);

            var gamesList = responseArray.Result.Games;

            switch (options.ToLower())
            {
                default:
                case null:
                    break;
                case "played":
                case "p":
                    gamesList = gamesList.Where(x => x.PlaytimeForever > 0).ToList();
                    break;
                case "not played":
                case "np":
                    gamesList = gamesList.Where(x => x.PlaytimeForever == 0).ToList();
                    break;
            }                

            var randomGame = gamesList.RandomItem();
            
            uint playtimeTwoWeeks = randomGame?.Playtime2Weeks ?? 0;            

            EmbedBuilder embed = new EmbedBuilder().WithOkColour().WithTitle("Random Game")
                //.WithUrl($"steam://run/{randomGame.AppID}")
                .AddField(new EmbedFieldBuilder().WithName("Game Title").WithValue(randomGame.Name))
                .AddField(new EmbedFieldBuilder().WithName("Total Playtime").WithValue(PlaytimeStringGen(randomGame.PlaytimeForever)).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Playtime - Last 2 Weeks").WithValue(PlaytimeStringGen(playtimeTwoWeeks)).WithIsInline(true));

            await Context.Channel.BlankEmbedAsync(embed);
        }

        private string PlaytimeStringGen(uint playtime)
        {
            if (playtime == 0)
                return "Never Played";
            else if (playtime < 60)
                return $"{playtime} minutes";
            else
                return $"{Math.Floor((double)playtime / 60)} hours";
        }

        [Command("LinkSteam"), Summary("Links steam acc")]
        public async Task LinkSteam(ulong id) => await LinkSteam(Context, id);

        [Command("LinkSteam"), Summary("Links steam acc")]
        public async Task LinkSteam(string id)
        {
            var response = await APIResponse(VanityURL.Replace("{key}", _config.SteamAPIKey).Replace("{url}", id));
            var responseArray = JsonConvert.DeserializeObject<VanityURLContainer>(response);

            if (responseArray.Result.Success != 1)
            {
                await Context.Channel.SendErrorAsync("Invalid steam URL!");
                return;
            }

            await LinkSteam(Context, responseArray.Result.SteamID);
        }

        private async Task LinkSteam(ICommandContext Context, ulong id)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                if (uow.User.GetSteamID(Context.User.Id) == 0)
                    uow.User.SetSteamID(Context.User.Id, id);
                else
                {
                    await Context.Channel.SendErrorAsync("You have already set your SteamID");
                    return;
                }
            }

            await Context.Channel.SendSuccessAsync($"Set steamd ID for {Context.User.Username} to {id}");
        }

        [Command("Hug"), Summary("Hug a given user")]
        public async Task Hug(IGuildUser user)
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;

            if (!AllowImage(Context.Channel.Id))
            {
                await Context.Channel.SendSuccessAsync("Hugging <3", $"{CurUser.NicknameUsername()} is giving {user.NicknameUsername()} a hug! <3");
                return;
            }

            string imageURL = await GetImageURL("hug+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Hugging <3", $"{CurUser.NicknameUsername()} is giving {user.NicknameUsername()} a hug! <3", $"{imageURL}");
        }

        [Command("GroupHug"), Summary("Hug the group")]
        [Alias("GHug", "HugAll", "HugA")]
        public async Task GroupHug()
        {
            if (!AllowImage(Context.Channel.Id))
            {
                await Context.Channel.SendSuccessAsync("Group Hug <3", "Share the love, everyone <3");
                return;
            }

            string imageURL = await GetImageURL("group_hug");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Group Hug <3", "Share the love, everyone <3", $"{imageURL}");
        }

        [Command("Pet"), Summary("Pet a given user")]
        [Alias("Pat")]
        public async Task Pat(IGuildUser user)
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;

            if (!AllowImage(Context.Channel.Id))
            {
                await Context.Channel.SendSuccessAsync("Patting <3", $"{CurUser.NicknameUsername()} is patting {user.NicknameUsername()}! <3");
                return;
            }

            string imageURL = await GetImageURL("petting+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Patting <3", $"{CurUser.NicknameUsername()} is patting {user.NicknameUsername()}! <3", $"{imageURL}");
        }

        [Command("Kiss"), Summary("Kiss a given user")]
        public async Task Kiss(IGuildUser user)
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;

            if (!AllowImage(Context.Channel.Id))
            {
                await Context.Channel.SendSuccessAsync("Kissing <3", $"{CurUser.NicknameUsername()} is giving {user.NicknameUsername()} a kiss! <3");
                return;
            }

            string imageURL = await GetImageURL("kiss+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Kissing <3", $"{CurUser.NicknameUsername()} is giving {user.NicknameUsername()} a kiss! <3", $"{imageURL}");
        }

        [Command("PickUser"), Summary("Raffle from a role, no role means everyone.")]
        [Alias("Raffle")]
        [RequireUserPermission(GuildPermission.MentionEveryone)]
        [RequireContext(ContextType.Guild)]
        public async Task PickUser(string item, IRole role = null)
        {
            IGuildUser user = null;
            var userList = await Context.Guild.GetUsersAsync();
            if (role == null)
            {
                user = userList.RandomItem();
            }
            else
            {
                user = userList.Where(x => x.RoleIds.Contains(role.Id)).RandomItem();
            }

            if (user == null) return;

            var msg = await Context.Channel.SendMessageAsync(user.Mention);
            msg.DeleteAfter(1);
            await Context.Channel.SendSuccessAsync("🎉 Raffle 🎉", $"{user.Username + "#" + user.Discriminator} has won {item}! Not like I wanted to give that to you though...");
        }

        private async Task<string> GetImageURL(string tags)
        {
            string imageURL = null;
            XElement[] arr = new XElement[0];

            //First we get the exact count
            int page = 0;
            arr = await SetupReponse(tags, page);
            if (arr == null) return null;

            //Using the count element
            XElement CountElm = arr.First();
            int imageCount = Int32.Parse(CountElm.Attributes().Where(x => x.Name.ToString().ToLower() == "count").FirstOrDefault().Value);

            int totalPages = (int)Math.Ceiling((double)(imageCount / limit));

            //It seems to break over 200, using 201 as upper is exclusive
            if (totalPages > 201) totalPages = 201;

            //Now lets get the actual thing
            page = _random.Next(0, totalPages);

            //If we're here we have a response stirng
            arr = await SetupReponse(tags, page);
            if (arr == null) return null;

            //Loop counter to stop infinite failure
            int loops = 0;

            //We do this in case it picks the first item at random... 
            while (imageURL == null && loops <= 4)
            {
                //We can get one of these elements at random
                XElement elm = arr.RandomItem();

                //Now lets use that element's fileurl
                imageURL = elm.Attributes().Where(x => x.Name.ToString().ToLower() == "file_url").FirstOrDefault()?.Value ?? null;

                loops++;
            }

            if (!String.IsNullOrEmpty(imageURL))
            {
                _logger.Log(imageURL, "ImageURL");
                return imageURL;
            }

            return null;
        }

        [Command("MCWhitelist"), Summary("Adds a user to the whitelist.")]
        [Alias("WhitelistMe", "MCWL")]
        public async Task MCWhitelist([Remainder] string username)
        {
            if (_rcon == null)
            {
                await Context.Channel.SendErrorAsync("No minecraft server specified.");
                return;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                string MCName = uow.User.GetMinecraftUsername(Context.User.Id);
                if (MCName != "none")
                {
                    await Context.Channel.SendErrorAsync("Only one username per Discord user.");
                    return;
                }

                //Add the username
                try
                {
                    //Connect
                    await _rcon.ConnectAsync();

                    //try to whitelist
                    await _rcon.SendCommandAsync($"whitelist add {username}");

                    //Add their username
                    uow.User.SetMinecraftUsername(Context.User.Id, username);

                    //Tell them it worked
                    await Context.Channel.SendSuccessAsync("You are now whitelisted!");
                }
                catch (Exception)
                {
                    await Context.Channel.SendErrorAsync("Error whitelisting you.");
                }
            }
        }

        private async Task<string> APIResponse(string fullURL)
        {
            //delay for 1/2 a second to help with API rate limiting
            await Task.Delay(500);

            //Make the request
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync(fullURL);
            }
        }

        private bool AllowImage(ulong ChID)
        {
            DateTime curTime = DateTime.Now;
            DateTime lastImageInChannel;

            lastImageInChannel = _cooldowns.GetUserCooldownsForCommand("Interactions", ChID);

            if (lastImageInChannel + new TimeSpan(0, 5, 0) > curTime)
                return false;

            _cooldowns.AddUserCooldowns("Interactions", ChID, curTime);

            return true;
        }

        private async Task<XElement[]> SetupReponse(string tags, int page)
        {
            string APIURLComplete = APIUrl.Replace("{page}", page.ToString()).Replace("{tags}", tags).Replace("{limit}", limit.ToString());

            //Response string
            string response = await APIResponse(APIURLComplete);

            if (response == null) return null;

            //If we're here we have a response stirng
            return XDocument.Parse(response).Descendants().ToArray();
        }
    }
}
