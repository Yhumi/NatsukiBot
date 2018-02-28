using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IUserRepository : IRepository<User>
    {
        User GetOrCreateUser(ulong UserID, bool Expemption = false, DateTime? dt = null);
        int GetBotUserID(ulong UserDiscordID);
        bool GetExemptionStatus(ulong UserDiscordID);
        void SetExemption(ulong UserDiscordID, bool Exemption);
        List<User> GetAllBirthdays(DateTime date);
        void SetUserBirthday(ulong id, DateTime date);
        void SetupAllBirthdays();
    }
}
