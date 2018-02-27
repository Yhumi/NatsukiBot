using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database.Models;
using System;
using System.Linq;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    public class UserRateRepository : Repository<UserRate>, IUserRateRepository
    {
        public UserRateRepository(DbContext context) : base(context)
        {
        }

        public UserRate GetOrCreateUserRate(int ChannelID, int UserID)
        {
            UserRate toReturn;
            toReturn = _set.FirstOrDefault(x => x.ChanneDBID == ChannelID && x.UserDBID == UserID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new UserRate()
                {
                    ChanneDBID = ChannelID,
                    UserDBID = UserID,
                    LastTrackingTime = DateTime.MinValue,
                    PostsSinceTracking = 0
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public DateTime GetLastTrackingTime(int ChannelID, int UserID) {
            UserRate UR = GetOrCreateUserRate(ChannelID, UserID);
            return UR.LastTrackingTime;
        }

        public int GetPostsSinceLastTrack(int ChannelID, int UserID)
        {
            UserRate UR = GetOrCreateUserRate(ChannelID, UserID);
            return UR.PostsSinceTracking;
        }

        public bool CanUserPostImages(int ChannelID, int UserID, TimeSpan Cooldown, int MaxImageCount)
        {
            UserRate UR = GetOrCreateUserRate(ChannelID, UserID);

            DateTime trackingNewTimeout = UR.LastTrackingTime + Cooldown;

            if (trackingNewTimeout < DateTime.Now)
            {
                UR.PostsSinceTracking = 0;
                UR.LastTrackingTime = DateTime.Now;
                _set.Update(UR);
                _context.SaveChanges();
            }

            if (UR.PostsSinceTracking > MaxImageCount)
                return false;

            return true;
        }

        public void AddUserPost(int ChannelID, int UserID, int Count = 1)
        {
            UserRate UR = GetOrCreateUserRate(ChannelID, UserID);
            UR.PostsSinceTracking += Count;

            _set.Update(UR);
            _context.SaveChanges();
        }

        public void SetUserPost(int ChannelID, int UserID, int Count = 0)
        {
            UserRate UR = GetOrCreateUserRate(ChannelID, UserID);
            UR.PostsSinceTracking = Count;

            _set.Update(UR);
            _context.SaveChanges();
        }
    }
}
