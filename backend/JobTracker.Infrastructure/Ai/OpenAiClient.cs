
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
public class OpenAiClient : IOpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OPEN_AI_KEY"]!;
    }

    public async Task<string> GetCompletionAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new UnauthorizedAccessException("No valid OpenAI API key configured.");

        var requestBody = new
        {
            model = "gpt-4.1-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"OpenAI request failed ({(int)response.StatusCode}): {content}");

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(content);

        var result = doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return result ?? "";
    }
}