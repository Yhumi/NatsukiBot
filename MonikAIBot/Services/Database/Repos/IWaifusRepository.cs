using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IWaifusRepository : IRepository<Waifus>
    {
        bool AddWaifu(string waifu);
        string GetRandomWaifu();
        List<Waifus> GetWaifus(int page = 0);
        bool DeleteWaifu(string waifu);
        bool DeleteWaifu(int ID);
        Waifus SearchWaifus(string waifu);
    }
}
