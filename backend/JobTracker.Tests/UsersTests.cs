using System.Net.Http.Json;
using FluentAssertions;
using JobTracker.API.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JobTracker.Tests;

public class UsersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersTests(WebApplicationFactory<Program> factory)
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
}