
using MonikAIBot.Services.Database.Repos;
using MonikAIBot.Services.Database.Repos.Impl;
using System;
using System.Threading.Tasks;

namespace MonikAIBot.Services.Database
{
    class UnitOfWork : IUnitOfWork
    {
        public DBContext _context { get; }
        private readonly MonikAIBotLogger logger = new MonikAIBotLogger();

        private IChannelsRepository _channels;
        public IChannelsRepository Channels => _channels ?? (_channels = new ChannelsRepository(_context));

        private IUserRepository _user;
        public IUserRepository User => _user ?? (_user = new UserRepository(_context));

        private IUserRateRepository _userRate;
        public IUserRateRepository UserRate => _userRate ?? (_userRate = new UserRateRepository(_context));

        private IGuildRepository _guild;
        public IGuildRepository Guild => _guild ?? (_guild = new GuildRepository(_context));

        private IGreetMessagesRepository _greetMessages;
        public IGreetMessagesRepository GreetMessages => _greetMessages ?? (_greetMessages = new GreetMessagesRepository(_context));

        public UnitOfWork(DBContext context)
        {
            _context = context;
        }

        public int Complete() =>
            _context.SaveChanges();

        public Task<int> CompleteAsync() =>
            _context.SaveChangesAsync();

        private bool disposed = false;

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
                if (disposing)
                    _context.Dispose();
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
