using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Student>().ToTable("Students");

            // ── FK FIX ────────────────────────────────────────────────────────
            // Without this, EF Core doesn't know Student.UserID → Users.UserID.
            // It cannot guarantee insert order, so it batches both INSERTs in one
            // round-trip and SQL Server rejects the FK constraint.
            // Declaring the relationship explicitly forces correct insert ordering.
            modelBuilder.Entity<Student>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}