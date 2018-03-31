using Discord;
using Discord.WebSocket;
using MonikAIBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonikAIBot.Modules.Fun.Common
{
    class PushButtonHandler
    {
        public event Func<IUserMessage, IGuildUser, Task> OnResponse;
        public PushButtonGame Game { get; }
        private DiscordSocketClient _client;

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        public PushButtonHandler(PushButtonGame game, DiscordSocketClient client)
        {
            Game = game;
            _client = client;
            _client.MessageReceived += TryResponse;
        }

        public async Task<bool> TryResponse(SocketMessage msg)
        {
            ButtonResponse BR;
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                if (msg == null || msg.Author.IsBot || msg.Channel.Id != Game.Channel)
                    return false;

                int response = ResponseParse(msg.Content);
                if (response == -1)
                    return false;

                var usr = msg.Author as IGuildUser;
                if (usr == null)
                    return false;

                BR = new ButtonResponse()
                {
                    UserID = msg.Author.Id,
                    Pushed = Convert.ToBoolean(response)
                };

                if (Game.Responses.Any(x => x.UserID == msg.Author.Id))
                    return false;

                if (!Game.Responses.Add(BR))
                    return false;

                var _ = OnResponse?.Invoke(msg as IUserMessage, usr);
            }
            finally { _locker.Release(); }
            return true;
        }

        public void End()
        {
            _client.MessageReceived -= TryResponse;
            OnResponse = null;
        }

        private short ResponseParse(string convert)
        {
            switch (convert.ToLower())
            {
                case "push":
                case "press":
                case "p":
                case "yes":
                case "y":
                case "1":
                case "true":
                    return 1;
                case "don't push":
                case "don't press":
                case "dp":
                case "no":
                case "n":
                case "0":
                case "false":
                    return 0;
                default:
                    return -1;
            }
        }
    }
}
