using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace chatbot.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(): base("DefaultConnection") { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
    }
}