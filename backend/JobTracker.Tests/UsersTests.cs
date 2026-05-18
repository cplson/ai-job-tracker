using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = "test@test.com",
            password = "123456"
        });

        response.IsSuccessStatusCode.Should().BeTrue();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.ReturnedUser.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = "invalid-email",
            password = "123"
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsValid()
    {
        await _client.PostAsJsonAsync("/api/users", new
        {
            email = "login-user@test.com",
            password = "123456"
        });

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email = "login-user@test.com",
            password = "123456"
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.ReturnedUser.Email.Should().Be("login-user@test.com");
    }

    [Fact]
    public async Task Me_ShouldReturnCurrentUser_WhenAuthenticated()
    {
        var auth = await RegisterAsync("me-user@test.com", "123456");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeTrue();
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        me!.Email.Should().Be("me-user@test.com");
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenAuthenticated()
    {
        var auth = await RegisterAsync("update-user@test.com", "123456");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{auth.ReturnedUser.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        request.Content = JsonContent.Create(new { password = "NewPassword123!" });

        var response = await _client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeTrue();

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email = "update-user@test.com",
            password = "NewPassword123!"
        });
        loginResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveUser()
    {
        var auth = await RegisterAsync("delete-user@test.com", "123456");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{auth.ReturnedUser.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email = "delete-user@test.com",
            password = "123456"
        });
        loginResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    private async Task<AuthResponse> RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private sealed class AuthResponse
    {
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("returnedUser")]
        public ReturnUserDto ReturnedUser { get; set; } = null!;
    }

    private sealed class MeResponse
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
    }
}
