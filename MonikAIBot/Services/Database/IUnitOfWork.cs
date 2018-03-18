using MonikAIBot.Services.Database.Repos;
using System;
using System.Threading.Tasks;

namespace MonikAIBot.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        DBContext _context { get; }

        IChannelsRepository Channels { get; }
        IUserRepository User { get; }
        IUserRateRepository UserRate { get; }
        IGuildRepository Guild { get; }
        IGreetMessagesRepository GreetMessages { get; }
        IBlockedLogsRepository BlockedLogs { get; }
        IAutoBanRepository AutoBan { get; }
        IWaifusRepository Waifus { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
