using Discord.WebSocket;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    public class BotStatusService
    {
        DiscordSocketClient _client;
        CancellationTokenSource m_ctSource;
        int timeout = 150000;

        public void StartBotStatuses(DiscordSocketClient client)
        {
            _client = client;
            using (var uow = DBHandler.UnitOfWork())
            {
                string status = uow.BotConfig.GetDefaultStatus(_client.CurrentUser.Id);
                bool rotation = uow.BotConfig.IsRotatingStatuses(_client.CurrentUser.Id);
                if (status != "" && !rotation)
                {
                    _client.SetGameAsync(status);
                }
                if (rotation)
                {
                    status = uow.BotStatuses.GetStatus().Status;
                    _client.SetGameAsync(status);
                }
            }

            Statuses();
        }

        public void Statuses()
        {
            m_ctSource = new CancellationTokenSource();

            Task.Delay(timeout).ContinueWith(async (x) =>
            {
                bool isRotating = false;
                using (var uow = DBHandler.UnitOfWork())
                {
                    isRotating = uow.BotConfig.IsRotatingStatuses(_client.CurrentUser.Id);
                }

                if (isRotating)
                {
                    BotStatuses status;
                    using (var uow = DBHandler.UnitOfWork())
                    {
                        status = uow.BotStatuses.GetStatus();
                    }

                    if (status == null) return;
                    if (String.IsNullOrWhiteSpace(status.Status)) return;

                    switch (status.Streaming)
                    {
                        default:
                        case false:
                            await _client.SetGameAsync(status.Status);
                            break;
                        case true:
                            await _client.SetGameAsync(status.Status, status.StreamURL, Discord.StreamType.Twitch);
                            break;
                    }
                }

                Statuses();
            }, m_ctSource.Token);
        }
    }
}
