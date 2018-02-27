using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services.Database.Repos
{
    public interface IUserRateRepository : IRepository<UserRate>
    {
        UserRate GetOrCreateUserRate(int ChannelID, int UserID);
        DateTime GetLastTrackingTime(int ChannelID, int UserID);
        int GetPostsSinceLastTrack(int ChannelID, int UserID);
        bool CanUserPostImages(int ChannelID, int UserID, TimeSpan Cooldown, int MaxImageCount);
        void AddUserPost(int ChannelID, int UserID, int count = 1);
        void SetUserPost(int ChannelID, int UserID, int count = 0);
    }
}
