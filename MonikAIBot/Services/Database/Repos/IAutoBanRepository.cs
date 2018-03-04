using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IAutoBanRepository : IRepository<AutoBan>
    {
        AutoBan GetAutoBan(ulong UserID);
        void AddAutoBan(ulong UserID);
        void DeleteAutoBan(ulong UserID);
        void DeleteAutoBan(int ID);
    }
}
