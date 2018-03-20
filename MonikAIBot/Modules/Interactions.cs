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

namespace MonikAIBot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Interactions : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;
        private readonly RCON _rcon;

        //API Stuff
        private readonly string APIUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={tags}+-comic+-photo+rating%3asafe&pid={page}&limit={limit}";
        private int limit = 100;

        public Interactions(Random random, MonikAIBotLogger logger, RCON rcon)
        {
            _random = random;
            _logger = logger;
            _rcon = rcon;
        }

        [Command("Hug"), Summary("Hug a given user")]
        public async Task Hug(IGuildUser user)
        {
            string imageURL = await GetImageURL("hug+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Hugging <3", $"{Context.User.Username} is giving {user.Username} a hug! <3", $"https:{imageURL}");
        }

        [Command("Pet"), Summary("Pet a given user")]
        public async Task Pat(IGuildUser user)
        {
            string imageURL = await GetImageURL("petting+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Petting <3", $"{Context.User.Username} is petting {user.Username}! <3", $"https:{imageURL}");
        }

        [Command("Kiss"), Summary("Kiss a given user")]
        public async Task Kiss(IGuildUser user)
        {
            string imageURL = await GetImageURL("kiss+animated");

            //Big issue?!
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync("Kissing <3", $"{Context.User.Username} is giving {user.Username} a kiss! <3", $"https:{imageURL}");
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

            //Now lets get the actual thing
            page = _random.Next(0, (int)Math.Ceiling((double)(imageCount / limit)));

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

        [Command("MCWhitelist"), Summary("Adds a user to the whitelist.")]
        [Alias("WhitelistMe", "MCWL")]
        public async Task MCWhitelist([Remainder] string username)
        {
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
