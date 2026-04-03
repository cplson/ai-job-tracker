using JobTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Application> Applications { get; set; } = null!;
        public DbSet<Resume> Resumes { get; set; } = null!;
        public DbSet<AIJob> AIJobs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Application>()
                .HasOne(a => a.Resume)
                .WithMany()
                .HasForeignKey(a => a.ResumeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}