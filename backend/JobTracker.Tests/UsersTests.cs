using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using JobTracker.API.DTOs;
using FluentAssertions;
using JobTracker.Infrastructure;
using JobTracker.Core.Entities;

public class UsersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersTests(WebApplicationFactory<Program> factory)
    {
        // Override the factory to use an in-memory database
        var webAppFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Register in-memory DbContext for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Ensure database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // Seed data
                if (!db.Users.Any())
                {
                    db.Users.AddRange(
                        new User { Email = "seed1@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!") },
                        new User { Email = "seed2@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password2!") }
                    );
                    db.SaveChanges();
                }
            });
        });

        _client = webAppFactory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnOk_WhenValid()
    {
        var request = new
        {
            email = "test@test.com",
            password = "123456"
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        response.IsSuccessStatusCode.Should().BeTrue();

        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenInvalid()
    {
        var request = new
        {
            email = "invalid-email",
            password = "123"
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUsers()
    {
        // Arrange - create a user first
        var createRequest = new
        {
            email = "test1@test.com",
            password = "123456"
        };

        await _client.PostAsJsonAsync("api/users", createRequest);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task UpdateUser_PartialUpdate_EmailOnly()
    {
        // Arrange
        var users = await _client.GetFromJsonAsync<List<UserDto>>("/api/users");
        var user = users!.First();

        var updateRequest = new
        {
            email = "updated-email.test.com"
        };

        // Act 
        var response = await _client.PutAsJsonAsync($"/api/users/{user.Id}", updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        updatedUser!.Email.Should().Be("updated-email.test.com");
    }

    [Fact]
    public async Task UpdateUser_PartialUpdate_PasswordOnly()
    {
        var users = await _client.GetFromJsonAsync<List<UserDto>>("/api/users");
        var user = users!.First();

        var updateRequest = new
        {
            password = "NewPassword123!"
        };

        // Act 
        var response = await _client.PutAsJsonAsync($"/api/users/{user.Id}", updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        updatedUser!.Email.Should().Be(user.Email);

        // var loginRequest = new
        // {
        //     email = user.Email,
        //     password = "NewPassword123!"
        // };

        // var loginResponse = await _client.PostAsJsonAsync("/api/users/login", loginRequest);
        // loginResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveUser()
    {
        // Arrange: get an existing user
        var users = await _client.GetFromJsonAsync<List<UserDto>>("/api/users");
        var user = users!.First();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // log
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DELETE response: {response.StatusCode}, content: {content}");
        // Verify user no longer exists
        var usersAfter = await _client.GetFromJsonAsync<List<UserDto>>("/api/users");
        usersAfter!.Any(u => u.Id == user.Id).Should().BeFalse();
    }
}