using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace MonikAIBot.Services
{
    public class Cooldowns
    {
        //Holds the per-command cooldowns, these reset on bot-restart
        private ConcurrentDictionary<string, ConcurrentDictionary<ulong, DateTime>> _cooldowns = new ConcurrentDictionary<string, ConcurrentDictionary<ulong, DateTime>>();

        public void SetupCommandCooldowns(string Command)
        {
            _cooldowns.AddOrUpdate(Command, new ConcurrentDictionary<ulong, DateTime>(), (i, d) => new ConcurrentDictionary<ulong, DateTime>());
        }

        public ConcurrentDictionary<ulong, DateTime> GetOrSetupCommandCooldowns(string Command)
        {
            ConcurrentDictionary<ulong, DateTime> returnDict = null;
            _cooldowns.TryGetValue(Command, out returnDict);

            if (returnDict != null)
                return returnDict;
            else
            {
                SetupCommandCooldowns(Command);
                return GetOrSetupCommandCooldowns(Command);
            }
        }

        public void UpdateCommandCooldowns(string Command, ConcurrentDictionary<ulong, DateTime> cooldowns)
        {
            _cooldowns.AddOrUpdate(Command, cooldowns, (i, d) => cooldowns);
        }

        public DateTime GetUserCooldownsForCommand(string Command, ulong userID)
        {
            ConcurrentDictionary<ulong, DateTime> cooldowns = null;
            _cooldowns.TryGetValue(Command, out cooldowns);

            if (cooldowns == null)
                return DateTime.MinValue;

            DateTime lastMessage;
            cooldowns.TryGetValue(userID, out lastMessage);

            return lastMessage;
        }

        public void AddUserCooldowns(string Command, ulong userID, DateTime dateTime)
        {
            ConcurrentDictionary<ulong, DateTime> cooldowns = null;
            _cooldowns.TryGetValue(Command, out cooldowns);

            if (cooldowns == null)
                return;

            cooldowns.AddOrUpdate(userID, dateTime, (i, d) => dateTime);
        }
    }
}
