using Discord.WebSocket;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    enum Scheduler
    {
        EveryMinutes,
        EveryHour,
        EveryHalfDay,
        EveryDay,
        EveryWeek,
        EveryMonth,
        EveryYear,
    }

    public class BirthdayService
    {
        CancellationTokenSource m_ctSource;
        DiscordSocketClient _client;
        Configuration _config;
        Random _random;

        private string[] birthdayString = new string[5]
        {
            "{userName} is celebrating their bithday today! 🎂",
            "Happy Birthday {userName}, you're {age} now! 🎂",
            "How does it feel to be {age} today, {userName}? 🎂",
            "Happy Birthday {userName}! 🎂",
            "User {userName} is now level {age} in the real world. 🎂"
        };

        public void StartBirthdays(DateTime time, DiscordSocketClient client, Configuration config, Random random)
        {
            _client = client;
            _config = config;
            _random = random;

            var nextDay = getNextDate(time, Scheduler.EveryDay);

            birthdayHandler(time, Scheduler.EveryDay);
        }

        private void birthdayHandler(DateTime date, Scheduler scheduler)
        {
            m_ctSource = new CancellationTokenSource();

            var dateNow = DateTime.Now;
            TimeSpan ts;
            if (date > dateNow)
            {
                ts = date - dateNow;
            }
            else
            {
                date = getNextDate(date, scheduler);
                ts = date - dateNow;
            }

            Task.Delay(ts).ContinueWith(async (x) =>
            {
                List<User> todaysBirthdays = GetBirthdays();

                if (todaysBirthdays != null || todaysBirthdays.Count > 0)
                {
                    foreach (User birthday in todaysBirthdays)
                    {
                        ISocketMessageChannel channel = (ISocketMessageChannel)_client.GetChannel(_config.BirthdayChannel);
                        SocketUser user = _client.GetUser(birthday.UserID);

                        string useString = birthdayString[_random.Next(birthdayString.Length)];
                        var age = DateTime.Today.Year - birthday.DateOfBirth.Year;

                        useString = useString.Replace("{userName}", user.Mention).Replace("{age}", age.ToString());

                        await channel.SendMessageAsync(useString);
                    }
                }

                birthdayHandler(getNextDate(date, scheduler), scheduler);
            }, m_ctSource.Token);
        }

        private DateTime getNextDate(DateTime date, Scheduler scheduler)
        {
            switch (scheduler)
            {
                case Scheduler.EveryMinutes:
                    return date.AddMinutes(1);
                case Scheduler.EveryHour:
                    return date.AddHours(1);
                case Scheduler.EveryHalfDay:
                    return date.AddHours(12);
                case Scheduler.EveryDay:
                    return date.AddDays(1);
                case Scheduler.EveryWeek:
                    return date.AddDays(7);
                case Scheduler.EveryMonth:
                    return date.AddMonths(1);
                case Scheduler.EveryYear:
                    return date.AddYears(1);
                default:
                    throw new Exception("Invalid scheduler");
            }
        }

        private List<User> GetBirthdays()
        {
            var curDate = DateTime.Now;
            List<User> userBirthdays;

            using (var uow = DBHandler.UnitOfWork())
            {
                userBirthdays = uow.User.GetAllBirthdays(curDate);
            }

            return userBirthdays;
        }
    }
}
