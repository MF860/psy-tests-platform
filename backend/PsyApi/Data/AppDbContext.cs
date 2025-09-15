using Microsoft.EntityFrameworkCore;
using PsyApi.Models;

namespace PsyApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemParameters> ItemParameters { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionItem> SessionItems { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AIJob> AIJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NationalId).IsUnique();
                entity.Property(e => e.NationalId).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            });

            // Configure Admin entity
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Configure Item entity
            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TextAr).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DimensionTags).IsRequired();
                entity.Property(e => e.Difficulty).IsRequired();
                entity.Property(e => e.TimeLimitSeconds).IsRequired();
                entity.Property(e => e.MaxScore).IsRequired();
            });

            // Configure ItemParameters entity
            modelBuilder.Entity<ItemParameters>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ItemId).IsUnique();
                entity.Property(e => e.ItemId).IsRequired();
                entity.Property(e => e.ModelType).IsRequired().HasMaxLength(16);
                entity.Property(e => e.ThresholdsJson).HasColumnType("text");
                entity.Property(e => e.PcmStepsJson).HasColumnType("text");
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Item)
                    .WithOne()
                    .HasForeignKey<ItemParameters>(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Session entity
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("InProgress");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Result)
                    .WithOne(r => r.Session)
                    .HasForeignKey<Result>(r => r.SessionId);
            });

            // Configure SessionItem entity
            modelBuilder.Entity<SessionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired();
                entity.Property(e => e.ItemId).IsRequired();

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.SessionItems)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.SessionItems)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Result entity
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired();
                entity.Property(e => e.TotalScore).IsRequired();
                entity.Property(e => e.PdfPath).HasMaxLength(255);
                entity.Property(e => e.DimensionScoresJson).HasColumnType("text");
                entity.Property(e => e.CompositeScoresJson).HasColumnType("text");
                entity.Property(e => e.ScoringModelVersion).HasMaxLength(32);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.Session)
                    .WithOne(s => s.Result)
                    .HasForeignKey<Result>(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(64);
                entity.Property(e => e.Details).HasColumnType("text");
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure AIJob entity
            modelBuilder.Entity<AIJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ResultId).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");

                entity.HasOne(e => e.Result)
                    .WithMany(r => r.AIJobs)
                    .HasForeignKey(e => e.ResultId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
