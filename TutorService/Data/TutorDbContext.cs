using Microsoft.EntityFrameworkCore;
using TutorService.Models;

namespace TutorService.Data
{
    public class TutorDbContext : DbContext
    {
        public TutorDbContext(DbContextOptions<TutorDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TutorSubject> TutorSubjects { get; set; }
        public DbSet<TutorAvailability> TutorAvailabilities { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tutor>().ToTable("Tutors");
            modelBuilder.Entity<TutorSubject>().ToTable("TutorSubjects");
            modelBuilder.Entity<TutorAvailability>().ToTable("TutorAvailability");
            modelBuilder.Entity<TimeSlot>().ToTable("TimeSlots");
        }
    }
}