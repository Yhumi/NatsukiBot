using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(DbContext context) : base(context)
        {
        }

        public User GetOrCreateUser(ulong UserID, bool Exemption = false)
        {
            User toReturn;
            toReturn = _set.FirstOrDefault(x => x.UserID == UserID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new User()
                {
                    UserID = UserID,
                    IsExempt = Exemption
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public int GetBotUserID(ulong UserDiscordID)
        {
            User u = GetOrCreateUser(UserDiscordID);
            return u.ID;
        }

        public bool GetExemptionStatus(ulong UserDiscordID)
        {
            User u = GetOrCreateUser(UserDiscordID);
            return u.IsExempt;
        }

        public void SetExemption(ulong UserDiscordID, bool Exemption)
        {
            User u = GetOrCreateUser(UserDiscordID);
            u.IsExempt = Exemption;

            _set.Update(u);
            _context.SaveChanges();
        }
    }
}
