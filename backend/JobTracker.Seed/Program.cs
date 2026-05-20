using JobTracker.Infrastructure;
using JobTracker.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);
var connectionArg = GetConnectionFromArgs(args);

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString =
    connectionArg
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? config.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "No database connection string. Set ConnectionStrings__DefaultConnection, " +
        "pass --connection \"Host=...\", or add ConnectionStrings:DefaultConnection to appsettings.json.");
    return 1;
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new AppDbContext(options);

try
{
    return await DemoDataSeeder.RunAsync(db, force);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Seed failed: " + ex.Message);
    Console.Error.WriteLine(ex.InnerException?.Message);
    return 1;
}

static string? GetConnectionFromArgs(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i].Equals("--connection", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            return args[i + 1];
    }

    return null;
}
