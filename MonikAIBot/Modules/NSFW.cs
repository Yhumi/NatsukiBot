using Discord.Commands;
using MonikAIBot.Services;
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
    [NSFWOnly]
    public class NSFW : ModuleBase
    {
        private readonly Random _random;
        private readonly MonikAIBotLogger _logger;

        //API Stuff
        private readonly string APIUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={tags}&pid={page}&limit={limit}";
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
            if (imageURL == null) return;

            //We have the URL let us use it
            await Context.Channel.SendPictureAsync($"{waifu}", "", $"{imageURL}");
        }

        private async Task<string> GetImageURL(string tags)
        {
            string imageURL = null;
            XElement[] arr = new XElement[0];

            while (arr.Count() == 0)
            {
                int page = _random.Next(0, 20);

                //Format the URL
                string APIURLComplete = APIUrl.Replace("{page}", page.ToString()).Replace("{tags}", tags).Replace("{limit}", limit.ToString());

                //Response string
                string response = await APIResponse(APIURLComplete);

                //If we're here we have a response stirng
                arr = XDocument.Parse(response).Descendants().ToArray();
            }
            
            while (imageURL == null)
            {
                //We can get one of these elements at random
                XElement elm = arr.RandomItem();

                //Now lets use that element's fileurl
                imageURL = elm.Attributes().Where(x => x.Name.ToString().ToLower() == "file_url").FirstOrDefault()?.Value ?? null;
            }

            return imageURL;
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
