using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using Serilog;
using System.Globalization;
using System.Text;

namespace PsyApi.Services
{
    public class DataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;

        public DataSeeder(AppDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedItemsAsync()
        {
            // Check if Items table is empty
            if (await _context.Items.AnyAsync())
            {
                _logger.Information("Items table already contains data. Skipping seeding.");
                return;
            }

            var items = new List<Item>();
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed", "items_100.csv");

            try
            {
                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                var header = await reader.ReadLineAsync(); // Skip header line

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    var item = new Item
                    {
                        TextAr = values[1],
                        Type = values[2],
                        DimensionTags = values[3],
                        Difficulty = int.Parse(values[4]),
                        TimeLimitSeconds = int.Parse(values[5]),
                        MaxScore = int.Parse(values[6]),
                        CorrectAnswer = values.Length > 7 ? values[7] : null
                    };

                    items.Add(item);
                }

                await _context.Items.AddRangeAsync(items);
                await _context.SaveChangesAsync();

                _logger.Information("Successfully seeded {Count} items into the database.", items.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding items from CSV file.");
                throw;
            }
        }
    }
}
