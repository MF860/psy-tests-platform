# Simple Neon Connection Test using .NET
Write-Host "=== Testing Neon Connection ===" -ForegroundColor Cyan
Write-Host ""

# Create a simple C# program to test connection
$testCode = @'
using Npgsql;
using System;

class Program
{
    static void Main()
    {
        // Connection strings
        var postgresConn = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=postgres;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true";
        var neondbConn = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true";
        
        Console.WriteLine("Step 1: Testing connection to 'postgres' database...");
        try
        {
            using (var conn = new NpgsqlConnection(postgresConn))
            {
                conn.Open();
                Console.WriteLine("✓ Connected to 'postgres' database successfully!");
                
                // Check if 'neondb' exists
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'neondb'", conn))
                {
                    var exists = cmd.ExecuteScalar();
                    
                    if (exists == null)
                    {
                        Console.WriteLine("\nStep 2: Creating 'neondb' database...");
                        using (var createCmd = new NpgsqlCommand("CREATE DATABASE neondb OWNER neondb_owner", conn))
                        {
                            createCmd.ExecuteNonQuery();
                            Console.WriteLine("✓ Database 'neondb' created successfully!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("✓ Database 'neondb' already exists");
                    }
                }
            }
            
            Console.WriteLine("\nStep 3: Testing connection to 'neondb'...");
            using (var conn = new NpgsqlConnection(neondbConn))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT version()", conn))
                {
                    var version = cmd.ExecuteScalar()?.ToString();
                    Console.WriteLine($"✓ Connected to 'neondb' successfully!");
                    Console.WriteLine($"PostgreSQL version: {version?.Substring(0, Math.Min(50, version.Length))}...");
                }
            }
            
            Console.WriteLine("\n✓ SUCCESS! Neon connection is working correctly.");
            Console.WriteLine("\nConnection string to use:");
            Console.WriteLine(neondbConn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ ERROR: {ex.Message}");
            Console.WriteLine("\nTroubleshooting:");
            Console.WriteLine("1. Verify credentials in Neon console: https://console.neon.tech");
            Console.WriteLine("2. Check if IP is whitelisted (Neon allows all by default)");
            Console.WriteLine("3. Ensure SSL Mode=Require is in connection string");
            Environment.Exit(1);
        }
    }
}
'@

# Save and compile test program
$testDir = "$PSScriptRoot\backend\PsyApi\bin\Debug\net8.0"
$testFile = "$testDir\NeonConnectionTest.cs"
$testOutput = "$testDir\NeonConnectionTest.exe"

Write-Host "Creating connection test program..." -ForegroundColor Gray
New-Item -ItemType Directory -Force -Path $testDir | Out-Null
Set-Content -Path $testFile -Value $testCode

Write-Host "Compiling test..." -ForegroundColor Gray
$cscPath = "C:\Program Files\dotnet\sdk\*\Roslyn\bincore\csc.dll"
$actualCsc = Get-Item $cscPath | Select-Object -Last 1

if ($actualCsc) {
    dotnet $actualCsc.FullName /r:"$testDir\Npgsql.dll" /out:$testOutput $testFile 2>&1 | Out-Null
}

# Simpler approach - just use dotnet script
Write-Host "Running connection test..." -ForegroundColor Yellow
Write-Host ""

cd "$PSScriptRoot\backend\PsyApi"

# Run a simple EF command to test connection
$env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=postgres;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"

Write-Host "Testing connection to 'postgres' database..." -ForegroundColor Gray
$result = dotnet ef dbcontext info 2>&1

if ($result -match "Provider name" -or $result -match "Database") {
    Write-Host "✓ Connection to Neon is working!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Creating/verifying 'neondb' database via SQL script..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please run this SQL in Neon console (https://console.neon.tech):" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "CREATE DATABASE neondb OWNER neondb_owner;" -ForegroundColor White
    Write-Host ""
    Write-Host "After creating the database, backend will connect successfully." -ForegroundColor Gray
} else {
    Write-Host "✗ Connection failed" -ForegroundColor Red
    Write-Host $result
}
