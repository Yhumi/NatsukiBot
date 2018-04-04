using Discord;
using Discord.Commands;
using MonikAIBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MonikAIBot.Modules
{
    [RequireContext(ContextType.Guild)]
    [NSFWOnly]
    public class NSFW : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;
        private readonly Cooldowns _cooldowns;

        //API Stuff
        private readonly string APIUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={tags}+-shota+-shotacon+-comic+-lolicon+-loli+-photo+-webm+-furry&pid={page}&limit={limit}";
        private int limit = 100;

        public NSFW(Random random, MonikAIBotLogger logger, Cooldowns cooldowns)
        {
            _random = random;
            _logger = logger;
            _cooldowns = cooldowns;

            _cooldowns.GetOrSetupCommandCooldowns("NSFW");
        }

        [Command("Waifuta")]
        public async Task Waifuta() => await Waifu("futa");

        [Command("Futa")]
        public async Task Futa() => await GelbooruSearch("futa");

        [Command("Waifu")]
        public async Task Waifu([Remainder] string extraTags = null)
        {
            DateTime curTime = DateTime.Now;
            DateTime lastMessage;

            lastMessage = _cooldowns.GetUserCooldownsForCommand("NSFW", Context.User.Id);

            if (lastMessage + new TimeSpan(0, 2, 0) > curTime)
            {
                var msg = await Context.Channel.SendErrorAsync("You may only use one of these NSFW commands every two minutes.");
                Context.Message.DeleteAfter(3);
                msg.DeleteAfter(3);
                return;
            }

            string waifu = null;
            using (var uow = DBHandler.UnitOfWork())
            {
                waifu = uow.Waifus.GetRandomWaifu();
            }

            if (waifu == null) return;

            if (extraTags != null)
            {
                if (extraTags.StartsWith("+"))
                {
                    extraTags = extraTags.Substring(1);
                }

                waifu += " + " + extraTags; 
            }

            string parsedWaifu = waifu.ParseBooruTags();

            var message = await Context.Channel.SendSuccessAsync("Searching...");

            string imageURL = await GetImageURL(parsedWaifu);

            EmbedBuilder eb = new EmbedBuilder();

            //Big issue?!
            if (imageURL == null)
            {
                await message.ModifyAsync(x => x.Embed = eb.EmbedErrorAsync($"No images found. The specified tags probably don't exist: {waifu}."));
                return;
            }

            _cooldowns.AddUserCooldowns("NSFW", Context.User.Id, curTime);

            //We have the URL let us use it
            await message.ModifyAsync(x => x.Embed = eb.PictureEmbed($"{waifu}", "", $"{imageURL}"));
        }

        [Command("PersonalWaifu")]
        [Alias("PWaifu")]
        public async Task PersonalWaifu()
        {
            DateTime curTime = DateTime.Now;
            DateTime lastMessage;

            lastMessage = _cooldowns.GetUserCooldownsForCommand("NSFW", Context.User.Id);

            if (lastMessage + new TimeSpan(0, 2, 0) > curTime)
            {
                var msg = await Context.Channel.SendErrorAsync("You may only use one of these NSFW commands every two minutes.");
                Context.Message.DeleteAfter(3);
                msg.DeleteAfter(3);
                return;
            }

            string pwaifu = null;
            using (var uow = DBHandler.UnitOfWork())
            {
                pwaifu = uow.User.GetPersonalWaifu(Context.User.Id);
            }

            if (pwaifu == "")
            {
                using (var uow = DBHandler.UnitOfWork())
                {
                    string waifuToAttribute = uow.Waifus.GetRandomWaifu();
                    pwaifu = uow.User.SetPersonalWaifu(Context.User.Id, waifuToAttribute);
                }
            }

            var message = await Context.Channel.SendSuccessAsync("Searching...");

            string imageURL = await GetImageURL(pwaifu.Replace(' ', '_').ToLower());

            EmbedBuilder eb = new EmbedBuilder();

            //Big issue?!
            if (imageURL == null)
            {
                await message.ModifyAsync(x => x.Embed = eb.EmbedErrorAsync("No images found. Most likely a mistake with your tags."));
                return;
            }

            _cooldowns.AddUserCooldowns("NSFW", Context.User.Id, curTime);

            //We have the URL let us use it
            await message.ModifyAsync(x => x.Embed = eb.PictureEmbed($"{pwaifu}", "", $"{imageURL}"));
        }

        [Command("Lick")]
        public async Task Lick(IGuildUser user)
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;
            string imageURL = await GetImageURL("licking+animated+rating%3asafe");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Licking <3", $"{CurUser.NicknameUsername()} is licking {user.NicknameUsername()}! <3", $"{imageURL}");
        }

        [Command("Suck")]
        public async Task Suck(IGuildUser user, string type = "both")
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;
            string imageURL = null;

            switch (type.ToLower())
            {
                default:
                case "both":
                case "b":
                    imageURL = await GetImageURL("fellatio+animated");
                    break;
                case "straight":
                case "s":
                    imageURL = await GetImageURL("fellatio+animated+-yaoi");
                    break;
                case "gay":
                case "g":
                case "yaoi":
                    imageURL = await GetImageURL("fellatio+animated+yaoi");
                    break;
            }

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Sucking <3", $"{CurUser.NicknameUsername()} is sucking {user.NicknameUsername()}...", $"{imageURL}");
        }

        [Command("Fuck")]
        public async Task Fuck(IGuildUser user, string type = "all")
        {
            IGuildUser CurUser = (IGuildUser)Context.User;
            if (Context.User.Id == user.Id) return;
            string imageURL = null;

            switch (type.ToLower())
            {
                default:
                case "all":
                case "a":
                    imageURL = await GetImageURL("sex+animated");
                    break;
                case "straight":
                case "s":
                    imageURL = await GetImageURL("sex+animated+-yaoi+-yuri");
                    break;
                case "gay":
                case "g":
                case "yaoi":
                    imageURL = await GetImageURL("sex+animated+yaoi+-yuri");
                    break;
                case "lesbian":
                case "l":
                case "yuri":
                    imageURL = await GetImageURL("sex+animated+yuri+-yaoi");
                    break;
            }

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Fucking <3", $"{CurUser.NicknameUsername()} is fucking {user.NicknameUsername()}...", $"{imageURL}");
        }

        [Command("GelbooruSearch")]
        [Alias("Gelbooru", "GBS")]
        public async Task GelbooruSearch([Remainder] string tags)
        {
            DateTime curTime = DateTime.Now;
            DateTime lastMessage;

            lastMessage = _cooldowns.GetUserCooldownsForCommand("NSFW", Context.User.Id);

            if (lastMessage + new TimeSpan(0, 2, 0) > curTime)
            {
                var msg = await Context.Channel.SendErrorAsync("You may only use one of these NSFW commands every two minutes.");
                Context.Message.DeleteAfter(3);
                msg.DeleteAfter(3);
                return;
            }

            //Lets start by perfecting the tags
            string ParsedTags = tags.ParseBooruTags();

            var message = await Context.Channel.SendSuccessAsync("Searching...");

            string imageURL = await GetImageURL(ParsedTags.ToLower());

            EmbedBuilder eb = new EmbedBuilder();

            //Big issue?!
            if (imageURL == null)
            {
                await message.ModifyAsync(x => x.Embed = eb.EmbedErrorAsync("No images found. Most likely a mistake with your tags."));
                return;
            }

            _cooldowns.AddUserCooldowns("NSFW", Context.User.Id, curTime);

            //We have the URL let us use it
            await message.ModifyAsync(x => x.Embed = eb.PictureEmbed($"{tags}", "", $"{imageURL}"));
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

            int totalPages = (int) Math.Ceiling((double)(imageCount / limit));

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
                return imageURL;
            }

            return null;
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

        private async Task<XElement[]> SetupReponse(string tags, int page)
        {
            string APIURLComplete = APIUrl.Replace("{page}", page.ToString()).Replace("{tags}", tags).Replace("{limit}", limit.ToString());

            _logger.Log(APIURLComplete, "Debug");

            //Response string
            string response = await APIResponse(APIURLComplete);

            if (response == null) return null;

            //If we're here we have a response stirng
            return XDocument.Parse(response).Descendants().ToArray();
        }
    }
}
