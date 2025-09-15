using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using Serilog;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

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
            var originalCsvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed", "items_100.csv");
            var cleanCsvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed", "items_100_clean.csv");

            // Use the clean CSV if it exists, otherwise use the original
            var csvPath = File.Exists(cleanCsvPath) ? cleanCsvPath : originalCsvPath;
            _logger.Information("Using CSV file: {CsvPath}", csvPath);

            try
            {
                // Configure CSV reader with specified settings
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    DetectDelimiter = false,
                    BadDataFound = null,
                    MissingFieldFound = null,
                    TrimOptions = TrimOptions.Trim
                };

                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                // Read header
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        // Check for required fields
                        if (string.IsNullOrWhiteSpace(csv.GetField<string>("item_id")) ||
                            string.IsNullOrWhiteSpace(csv.GetField<string>("text_ar")) ||
                            string.IsNullOrWhiteSpace(csv.GetField<string>("type")))
                        {
                            _logger.Warning("Skipping row with missing core fields: {Row}", csv.Parser.RawRecord);
                            continue;
                        }

                        // Get text_ar and handle unquoted commas
                        var textAr = csv.GetField<string>("text_ar") ?? string.Empty;
                        if (textAr.Contains(",") && !textAr.StartsWith("\""))
                        {
                            // Wrap with quotes if it contains a comma and is not already quoted
                            textAr = """ + textAr + """;
                        }

                        // Create item with mapped fields
                        var item = new Item
                        {
                            TextAr = textAr,
                            Type = csv.GetField<string>("type") ?? string.Empty,
                            DimensionTags = csv.GetField<string>("dimension_tags") ?? string.Empty,
                            Difficulty = csv.GetField<int>("difficulty"),
                            TimeLimitSeconds = csv.GetField<int>("time_limit_seconds"),
                            MaxScore = csv.GetField<int>("max_score"),
                            CorrectAnswer = csv.GetField<string?>("correct_answer")
                        };

                        items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Error processing row: {Row}", csv.Parser.RawRecord);
                        continue;
                    }
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

        public async Task SeedItemParametersAsync()
        {
            // Check if ItemParameters table is empty
            if (await _context.ItemParameters.AnyAsync())
            {
                _logger.Information("ItemParameters table already contains data. Skipping seeding.");
                return;
            }

            try
            {
                // Get all items that don't have parameters yet
                var itemsWithoutParameters = await _context.Items
                    .Where(i => !_context.ItemParameters.Any(ip => ip.ItemId == i.Id))
                    .ToListAsync();

                var itemParametersList = new List<ItemParameters>();

                foreach (var item in itemsWithoutParameters)
                {
                    var itemParameter = new ItemParameters
                    {
                        ItemId = item.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Set parameters based on item type
                    switch (item.Type.ToUpper())
                    {
                        case "MCQ":
                            itemParameter.ModelType = "3PL";
                            itemParameter.A = 1.0;
                            // Map difficulty (1..5) to B parameter (+2,+1,0,-1,-2)
                            itemParameter.B = 3 - item.Difficulty; // 1->2, 2->1, 3->0, 4->-1, 5->-2
                            itemParameter.C = 0.15;
                            break;

                        case "LIKERT":
                            itemParameter.ModelType = "GRM";
                            itemParameter.ThresholdsJson = "[-1.5,-0.5,0.5,1.5]";
                            break;

                        case "ORDERING":
                            itemParameter.ModelType = "PCM";
                            // No initial parameters for PCM
                            break;

                        case "TIMED_NUMERIC":
                            itemParameter.ModelType = "NUMERIC";
                            itemParameter.TimeAlpha = 1.0;
                            itemParameter.TimeBeta = 0.0;
                            break;

                        case "TEXT":
                            itemParameter.ModelType = "TEXT";
                            // No initial parameters for TEXT
                            break;

                        default:
                            _logger.Warning("Unknown item type: {Type} for item ID: {ItemId}", item.Type, item.Id);
                            continue;
                    }

                    itemParametersList.Add(itemParameter);
                }

                await _context.ItemParameters.AddRangeAsync(itemParametersList);
                await _context.SaveChangesAsync();

                _logger.Information("Successfully seeded {Count} item parameters into the database.", itemParametersList.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding item parameters.");
                throw;
            }
        }
    }
}