using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MonikAIBot.Services.Database.Models;

namespace MonikAIBot.Services.Database
{
    public class DBContextFactory : IDbContextFactory<DBContext>
    {
        private readonly MonikAIBotLogger _logger = new MonikAIBotLogger();
        public DBContext Create(DbContextFactoryOptions options)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite("Filename=data/nat.db");
            return new DBContext(optionsBuilder.Options);
        }
    }

    public class DBContext : DbContext
    {
        public DbSet<Channels> Channels { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<UserRate> UserRate { get; set; }
        public DbSet<Guild> Guild { get; set; }
        public DbSet<GreetMessages> GreetMessages { get; set; }

        public DBContext() : base()
        {
        }

        public DBContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Channels
            var channelsEntity = modelBuilder.Entity<Channels>();
            channelsEntity
                .HasIndex(d => d.ChannelID)
                .IsUnique();
            #endregion

            #region User
            var userEntity = modelBuilder.Entity<User>();
            userEntity
                .HasIndex(d => d.UserID)
                .IsUnique();
            #endregion

            #region UserRate
            var userRateEntity = modelBuilder.Entity<UserRate>();
            userRateEntity
                .HasIndex(d => d.ID)
                .IsUnique();
            #endregion

            #region Guild
            var guildEntity = modelBuilder.Entity<Guild>();
            guildEntity
                .HasIndex(d => d.GuildID)
                .IsUnique();
            #endregion

            #region GreetMessages
            var greetMessagesEntity = modelBuilder.Entity<GreetMessages>();
            greetMessagesEntity
                .HasIndex(d => d.ServerID)
                .IsUnique();
            #endregion
        }
    }
}
