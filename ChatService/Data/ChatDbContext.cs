using Microsoft.EntityFrameworkCore;
using ChatService.Models;

namespace ChatService.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatRoom>().ToTable("ChatRooms");

            modelBuilder.Entity<ChatMessage>().ToTable("ChatMessages");
            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.SenderName)
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty)
                .IsRequired(false);   // allows NULL in DB for backward compat
        }
    }
}
