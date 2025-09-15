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
                     ?? "Host=localhost;Database=dev;Username=dev;Password=dev";
            optionsBuilder.UseNpgsql(cs);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

