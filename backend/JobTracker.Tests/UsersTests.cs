using System.Net.Http.Json;
using JobTracker.API.DTOs;
using FluentAssertions;

namespace JobTracker.Tests;

public class UsersTests : IClassFixture<JobTrackerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersTests(JobTrackerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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

        var result = await response.Content.ReadFromJsonAsync<LoginUserDto>();
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

        var users = await response.Content.ReadFromJsonAsync<List<LoginUserDto>>();
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task UpdateUser_PartialUpdate_EmailOnly()
    {
        // Arrange
        var users = await _client.GetFromJsonAsync<List<ReturnUserDto>>("/api/users");
        var user = users!.First();

        var updateRequest = new
        {
            email = "updated-email.test.com"
        };

        // Act 
        var response = await _client.PutAsJsonAsync($"/api/users/{user.Id}", updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await response.Content.ReadFromJsonAsync<ReturnUserDto>();
        updatedUser!.Email.Should().Be("updated-email.test.com");
    }

    [Fact]
    public async Task UpdateUser_PartialUpdate_PasswordOnly()
    {
        var users = await _client.GetFromJsonAsync<List<ReturnUserDto>>("/api/users");
        var user = users!.First();

        var updateRequest = new
        {
            password = "NewPassword123!"
        };

        // Act 
        var response = await _client.PutAsJsonAsync($"/api/users/{user.Id}", updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await response.Content.ReadFromJsonAsync<ReturnUserDto>();
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
        var users = await _client.GetFromJsonAsync<List<ReturnUserDto>>("/api/users");
        var user = users!.First();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // log
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DELETE response: {response.StatusCode}, content: {content}");
        // Verify user no longer exists
        var usersAfter = await _client.GetFromJsonAsync<List<ReturnUserDto>>("/api/users");
        usersAfter!.Any(u => u.Id == user.Id).Should().BeFalse();
    }
}