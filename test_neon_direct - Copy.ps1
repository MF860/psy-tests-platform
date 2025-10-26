# Test direct Neon connection (non-pooled)
Write-Host "Testing Neon database connection..." -ForegroundColor Cyan

# Try direct endpoint (without -pooler)
$directEndpoint = "ep-holy-glitter-a40pexde.us-east-1.aws.neon.tech"
$connectionString = "Host=$directEndpoint;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"

Write-Host "`nTrying DIRECT endpoint: $directEndpoint" -ForegroundColor Yellow

cd backend/PsyApi
$env:ConnectionStrings__DefaultConnection = $connectionString
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starting backend with direct connection..." -ForegroundColor Green
dotnet run
