using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IWaifusRepository : IRepository<Waifus>
    {
        void AddWaifu(string waifu);
        string GetRandomWaifu();
        List<Waifus> GetWaifus();
    }
}
