# Neon PostgreSQL Deployment Guide

## Overview
This guide shows how to configure your backend to connect to a Neon PostgreSQL database for production deployment.

## Step 1: Create Neon Database

1. Go to [Neon Console](https://console.neon.tech/)
2. Click **Create Project**
3. Name your project: `psy-tests-platform-prod`
4. Select region closest to your Render deployment (e.g., `US East (Ohio)`)
5. Click **Create Project**

## Step 2: Get Connection String

After creating the project, Neon provides connection strings. Use the **Pooled Connection** for better performance:

```
postgres://[user]:[password]@[endpoint]/[dbname]?sslmode=require
```

### Example Connection String Format:
```
postgres://psy_owner:ABC123xyz@ep-cool-waterfall-12345678.us-east-2.aws.neon.tech/psy_prod?sslmode=require
```

### Important Notes:
- **SSL is REQUIRED** by Neon - always include `?sslmode=require` or `?sslmode=verify-full`
- Use the **pooled connection** endpoint (faster, handles more concurrent connections)
- Connection string contains sensitive credentials - never commit to git

## Step 3: Connection String for Npgsql

The ASP.NET Core backend uses Npgsql which accepts the standard PostgreSQL connection string format. Your connection string should look like:

```
Host=ep-cool-waterfall-12345678.us-east-2.aws.neon.tech;
Database=psy_prod;
Username=psy_owner;
Password=ABC123xyz;
SSL Mode=Require;
Trust Server Certificate=true
```

**Alternative format (URL-style):**
```
postgres://psy_owner:ABC123xyz@ep-cool-waterfall-12345678.us-east-2.aws.neon.tech/psy_prod?sslmode=require
```

Both formats work with Npgsql. Choose based on preference.

## Step 4: Set Environment Variable in Render

In your Render dashboard, set the following environment variable:

**Key:** `ConnectionStrings__DefaultConnection`  
**Value:** Your Neon connection string (use either format above)

Example:
```
ConnectionStrings__DefaultConnection=Host=ep-cool-waterfall-12345678.us-east-2.aws.neon.tech;Database=psy_prod;Username=psy_owner;Password=ABC123xyz;SSL Mode=Require;Trust Server Certificate=true
```

## Step 5: Verify Database Detection

The backend automatically detects PostgreSQL when the connection string contains `Host=` or `Server=`:

```csharp
// From Program.cs - auto-detection logic
if (connectionString.Contains("Host=") || connectionString.Contains("Server=")) {
    // Uses PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options => 
        options.UseNpgsql(connectionString));
}
```

## Step 6: Apply Migrations

On first deployment, the backend will automatically create tables using:
```csharp
await appDb.Database.EnsureCreatedAsync();
```

For production with migrations:
```csharp
await appDb.Database.MigrateAsync(); // Recommended for production
```

## Troubleshooting

### SSL Certificate Issues
If you see SSL errors, ensure `Trust Server Certificate=true` is in your connection string, or use:
```
SSL Mode=Require;Trust Server Certificate=true
```

### Connection Timeouts
Add timeout parameters:
```
Host=your-endpoint.neon.tech;Database=psy_prod;Username=user;Password=pass;SSL Mode=Require;Timeout=30;Command Timeout=30
```

### Pooling Configuration
Neon recommends using pooled connections. If using direct connections, configure connection pooling:
```
Host=your-endpoint.neon.tech;Database=psy_prod;Username=user;Password=pass;SSL Mode=Require;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100
```

## Testing Connection Locally

Before deploying, test your Neon connection locally:

1. Create `.env` file (don't commit):
```bash
ConnectionStrings__DefaultConnection="Host=your-endpoint.neon.tech;Database=psy_prod;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true"
USE_SQLITE=0
```

2. Run backend:
```bash
cd backend/PsyApi
dotnet run
```

3. Check logs for:
```
Using PostgreSQL with connection string: Host=your-endpoint.neon.tech...
```

## Production Checklist

- [ ] Neon project created
- [ ] Database connection string copied
- [ ] `ConnectionStrings__DefaultConnection` set in Render
- [ ] `USE_SQLITE` NOT set (or set to 0)
- [ ] Backend logs show "Using PostgreSQL"
- [ ] Tables created successfully on first run
- [ ] Seeding completes without errors

## Security Best Practices

1. **Never commit credentials** - use environment variables only
2. **Use Neon's branch feature** for staging/development databases
3. **Enable connection pooling** in Neon settings
4. **Monitor query performance** via Neon dashboard
5. **Set up read replicas** if needed for high traffic

## Cost Optimization

Neon Free Tier includes:
- 0.5 GB storage
- 1 compute unit (shared)
- 10 GB data transfer/month

For production:
- Upgrade to **Scale** plan for dedicated compute
- Enable **Autoscaling** to handle traffic spikes
- Use **branch databases** for testing

## Support

- [Neon Documentation](https://neon.tech/docs)
- [Npgsql Connection Strings](https://www.npgsql.org/doc/connection-string-parameters.html)
- [ASP.NET Core with PostgreSQL](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
