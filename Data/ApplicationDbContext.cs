using Microsoft.EntityFrameworkCore;
using psy_tests_platform.Models;

namespace psy_tests_platform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestItem> TestItems { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<SessionItem> SessionItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure TestItem entity
            modelBuilder.Entity<TestItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Store ScoringParameters as JSON
                entity.Property(e => e.ScoringParameters)
                    .HasColumnType("nvarchar(max)");
            });

            // Configure Answer entity
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Store Value as JSON
                entity.Property(e => e.Value)
                    .HasColumnType("nvarchar(max)");
            });

            // Configure SessionItem entity
            modelBuilder.Entity<SessionItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.TestItem)
                    .WithMany()
                    .HasForeignKey(e => e.TestItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Answer)
                    .WithMany()
                    .HasForeignKey(e => e.AnswerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
