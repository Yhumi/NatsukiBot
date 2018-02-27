using Microsoft.EntityFrameworkCore;
using MonikAIBot.Services.Database;

namespace MonikAIBot.Services
{
    public class DBHandler
    {
        private static DBHandler _instance = null;
        public static DBHandler Instance = _instance ?? (_instance = new DBHandler());
        private readonly DbContextOptions options;

        private string connectionString { get; }

        static DBHandler() { }

        private DBHandler()
        {
            connectionString = "Filename=data/nat.db";
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(connectionString);
            options = optionsBuilder.Options;
        }

        public DBContext GetFloraContext()
        {
            var context = new DBContext(options);
            context.Database.Migrate();

            return context;
        }

        private IUnitOfWork GetUnitOfWork() =>
            new UnitOfWork(GetFloraContext());

        public static IUnitOfWork UnitOfWork() =>
            DBHandler.Instance.GetUnitOfWork();
    }
}
