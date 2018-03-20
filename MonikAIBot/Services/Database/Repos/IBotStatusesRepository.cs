using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IBotStatusesRepository : IRepository<BotStatuses>
    {
        BotStatuses AddStatus(string Status, bool streaming = false, string url = "");
        BotStatuses GetStatus();
        void DeleteStatus(string Status);
        void DeleteStatus(int ID);
        List<BotStatuses> GetBotStatuses(int page = 0);
    }
}
