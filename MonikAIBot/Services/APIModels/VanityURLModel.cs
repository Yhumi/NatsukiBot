using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.APIModels
{
    internal class VanityURLContainer
    {
        [JsonProperty("response")]
        public VanityUrlResult Result { get; set; }
    }

    internal class VanityUrlResult
    {
        [JsonProperty("steamid")]
        public ulong SteamID { get; set; }

        [JsonProperty("success")]
        public uint Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
