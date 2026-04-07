public interface IOpenAiClient
{
    Task<string> GetCompletionAsync(string prompt);
}