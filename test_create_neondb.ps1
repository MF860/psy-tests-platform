# Create neondb database using psql command-line approach
Write-Host "Creating neondb database via command line..." -ForegroundColor Cyan

# First, try connecting to postgres database and creating neondb
$postgresConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=postgres;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"

Write-Host "`nAttempting to create database using EF Core migrations..." -ForegroundColor Yellow

cd backend/PsyApi

# Use postgres database to run the CREATE DATABASE command
$env:ConnectionStrings__DefaultConnection = $postgresConnection

Write-Host "Running custom database creation script..." -ForegroundColor Green

# Create a temporary C# program to execute raw SQL
$tempScript = @"
using Npgsql;

var connString = "$postgresConnection";
try
{
    using var conn = new NpgsqlConnection(connString);
    conn.Open();
    Console.WriteLine("Connected to postgres database");
    
    // Create neondb database
    using var cmd = new NpgsqlCommand("CREATE DATABASE neondb OWNER neondb_owner;", conn);
    cmd.ExecuteNonQuery();
    Console.WriteLine("SUCCESS: neondb database created!");
}
catch (Exception ex)
{
    if (ex.Message.Contains("already exists"))
    {
        Console.WriteLine("Database neondb already exists - this is OK!");
    }
    else
    {
        Console.WriteLine(`$"ERROR: {ex.Message}`");
    }
}
"@

$tempScript | Out-File -FilePath "create_db.csx" -Encoding UTF8

Write-Host "`nExecuting database creation script with dotnet-script..." -ForegroundColor Cyan
dotnet script create_db.csx

Write-Host "`nCleaning up..." -ForegroundColor Gray
Remove-Item create_db.csx -ErrorAction SilentlyContinue

Write-Host "`nDone! Now starting backend with neondb..." -ForegroundColor Green
$env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"
dotnet run
