using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JobTracker.Core.Entities;
using JobTracker.Infrastructure;

namespace JobTracker.Tests;

/// <summary>
/// Supplies configuration required by Program.cs (e.g. JWT) so tests run without a local .env file.
/// </summary>
public class JobTrackerWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "IntegrationTestJwtSigningKey_MustBe32CharsOrMore!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting / Testing env apply before Program.cs reads Configuration (ConfigureAppConfiguration does not).
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", "JobTrackerAPI");
        builder.UseSetting("Jwt:Audience", "JobTrackerClient");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any())
            {
                db.Users.AddRange(
                    new User { Email = "seed1@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!") },
                    new User { Email = "seed2@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password2!") }
                );
                db.SaveChanges();
            }
        });
    }
}
