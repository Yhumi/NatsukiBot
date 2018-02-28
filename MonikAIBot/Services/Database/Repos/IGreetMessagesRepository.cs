using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IGreetMessagesRepository : IRepository<GreetMessages>
    {
        void CreateGreatMessage(ulong sID, string message);
        GreetMessages GetRandomGreetMessage(ulong sID);
        void DeleteGreetMessage(int ID);
        void DeleteGreetMessage(string message, ulong serverID);
        List<GreetMessages> FetchGreetMessages(ulong sID, int page = 0);
    }
}
