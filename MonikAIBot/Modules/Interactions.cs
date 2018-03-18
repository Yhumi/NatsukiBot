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
        private readonly string APIUrl = "https://safebooru.org/index.php?page=dapi&s=post&q=index&tags={tags}&pid={page}&limit={limit}";
        private int limit = 25;

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
            //Get page for image
            int page = _random.Next(0, 2);

            //Format the URL
            string APIURLComplete = APIUrl.Replace("{tags}", tags).Replace("{page}", page.ToString()).Replace("{limit}", limit.ToString());

            //Response string
            string response = await APIResponse(APIURLComplete);

            //Now handle it, if it's null we return otherwise the task is awaited.
            if (response == null) return null;

            string imageURL = null;

            while (imageURL == null)
            {
                //If we're here we have a response stirng
                XElement[] arr = XDocument.Parse(response).Descendants().ToArray();

                //We can get one of these elements at random
                XElement elm = arr.RandomItem();

                //Now lets use that element's fileurl
                imageURL = elm.Attributes().Where(x => x.Name.ToString().ToLower() == "file_url").FirstOrDefault()?.Value ?? null;
            }

            return imageURL;
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
            //Make the request
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync(fullURL);
            }
        }
    }
}
