using System;
using System.ComponentModel.DataAnnotations;

namespace MonikAIBot.Services.Database.Models
{
    public class DBEntity
    {
        [Key]
        public int ID { get; set; }
        public DateTime? DateAdded { get; set; } = DateTime.UtcNow;
    }
}
