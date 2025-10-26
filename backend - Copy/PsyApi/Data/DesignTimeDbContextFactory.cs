using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PsyApi.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                     ?? "Data Source=psy_dev.db";
            
            // Check if the connection string is for SQLite or PostgreSQL
            if (cs.StartsWith("Data Source=") || cs.Contains(".db"))
            {
                optionsBuilder.UseSqlite(cs);
            }
            else
            {
                optionsBuilder.UseNpgsql(cs);
            }
            
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

