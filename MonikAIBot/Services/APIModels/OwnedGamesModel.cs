using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.APIModels
{
    internal class OwnedGamesResultContainer
    {
        [JsonProperty("response")]
        public OwnedGamesResult Result { get; set; }
    }

    internal class OwnedGamesResult
    {
        [JsonProperty("game_count")]
        public uint GameCount { get; set; }

        [JsonProperty("games")]
        public IList<Game> Games { get; set; }
    }

    internal class Game
    {
        [JsonProperty("appid")]
        public uint AppID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("playtime_forever")]
        public uint PlaytimeForever { get; set; }

        [JsonProperty("img_icon_url")]
        public string IconURL { get; set; }

        [JsonProperty("img_logo_url")]
        public string LogoURL { get; set; }

        [JsonProperty("has_community_visible_stats")]
        public bool CommunityStats { get; set; }

        [JsonProperty("playtime_2weeks")]
        public uint? Playtime2Weeks { get; set; }
    }
}
