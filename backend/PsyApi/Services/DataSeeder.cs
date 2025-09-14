using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly Serilog.ILogger _logger;

        public DataSeeder(AppDbContext context, Serilog.ILogger logger)
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

                    // Simple approach: split by comma and handle basic cases
                    var values = line.Split(',');

                    // If we have more than 8 values, we need to merge some columns that contained commas
                    if (values.Length > 8)
                    {
                        // Merge the text_ar column (index 1) which might contain commas
                        var textAr = values[1];
                        for (int i = 2; i < values.Length - 6; i++)
                        {
                            textAr += "," + values[i];
                        }

                        // Create a new values array with the correct number of elements
                        var newValues = new string[8];
                        newValues[0] = values[0];
                        newValues[1] = textAr;
                        newValues[2] = values[values.Length - 6];
                        newValues[3] = values[values.Length - 5];
                        newValues[4] = values[values.Length - 4];
                        newValues[5] = values[values.Length - 3];
                        newValues[6] = values[values.Length - 2];
                        newValues[7] = values[values.Length - 1];

                        values = newValues;
                    }
                    else if (values.Length < 8)
                    {
                        _logger.Warning("Skipping malformed line: {Line}", line);
                        continue;
                    }

                    // Parse numeric values with error handling
                    if (!int.TryParse(values[4], out var difficulty))
                    {
                        _logger.Warning("Invalid difficulty value in line: {Line}", line);
                        continue;
                    }

                    if (!int.TryParse(values[5], out var timeLimit))
                    {
                        _logger.Warning("Invalid time limit value in line: {Line}", line);
                        continue;
                    }

                    if (!int.TryParse(values[6], out var maxScore))
                    {
                        _logger.Warning("Invalid max score value in line: {Line}", line);
                        continue;
                    }

                    var item = new Item
                    {
                        TextAr = values[1],
                        Type = values[2],
                        DimensionTags = values[3],
                        Difficulty = difficulty,
                        TimeLimitSeconds = timeLimit,
                        MaxScore = maxScore,
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
