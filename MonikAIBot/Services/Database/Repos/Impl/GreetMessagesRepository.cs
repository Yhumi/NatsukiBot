using MonikAIBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;

namespace MonikAIBot.Services.Database.Repos.Impl
{
    class GreetMessagesRepository : Repository<GreetMessages>, IGreetMessagesRepository
    {
        public GreetMessagesRepository(DBContext context) : base (context)
        {
        }

        public void CreateGreatMessage(ulong sID, string message)
        {
            _set.Add(new GreetMessages()
            {
                ServerID = sID,
                Message = message
            });
            _context.SaveChanges();
        }

        public void DeleteGreetMessage(int ID)
        {
            GreetMessages GM = _set.Where(x => x.ID == ID).FirstOrDefault();
            if (GM == null) return;

            _set.Remove(GM);
            _context.SaveChanges();
        }

        public void DeleteGreetMessage(string message, ulong serverID)
        {
            GreetMessages GM = _set.Where(x => x.Message.ToLower() == message.ToLower() && x.ServerID == serverID).FirstOrDefault();
            if (GM == null) return;

            _set.Remove(GM);
            _context.SaveChanges();
        }

        public List<GreetMessages> FetchGreetMessages(ulong sID, int page = 0)
        {
            int offset = page * 9;
            return _set.Where(x => x.ServerID == sID).OrderBy(x => x.ID).Skip(offset).Take(9).ToList();
        }

        public GreetMessages GetRandomGreetMessage(ulong sID)
        {
            return _set.Where(x => x.ServerID == sID).ToList().RandomItem();
        }
    }
}
