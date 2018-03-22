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

        //API Stuff
        private readonly string APIUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={tags}+-comic+-lolicon+-loli+-photo+-webm+-furry&pid={page}&limit={limit}";
        private int limit = 100;

        public NSFW(Random random, MonikAIBotLogger logger)
        {
            _random = random;
            _logger = logger;
        }

        [Command("Waifu")]
        public async Task Waifu()
        {
            string waifu = null;
            using (var uow = DBHandler.UnitOfWork())
            {
                waifu = uow.Waifus.GetRandomWaifu();
            }

            if (waifu == null) return;

            string imageURL = await GetImageURL(waifu.Replace(' ', '_').ToLower());

            //Big issue?!
            if (imageURL == null)
            {
                await Context.Channel.SendErrorAsync("No image URL found. Most likely a mistake.");
                return;
            }

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync($"{waifu}", "", $"{imageURL}");
        }

        [Command("PersonalWaifu")]
        [Alias("PWaifu")]
        public async Task PersonalWaifu()
        {
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

            string imageURL = await GetImageURL(pwaifu.Replace(' ', '_').ToLower());

            //Big issue?!
            if (imageURL == null)
            {
                await Context.Channel.SendErrorAsync("No image URL found. Most likely a mistake.");
                return;
            }

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync($"{pwaifu}", "", $"{imageURL}");
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
            //Lets start by perfecting the tags
            string ParsedTags = tags.ParseBooruTags();

            string imageURL = await GetImageURL(ParsedTags.ToLower());

            //Big issue?!
            if (imageURL == null)
            {
                await Context.Channel.SendErrorAsync("No image URL found. Most likely a mistake with your tags.");
                return;
            }

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync($"{tags}", "", $"{imageURL}");
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

            //It seems to break over 200
            if (totalPages > 200) totalPages = 200;

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
