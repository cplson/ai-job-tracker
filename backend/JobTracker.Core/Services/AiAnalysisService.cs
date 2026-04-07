using JobTracker.Core.Interfaces;
using System.Text.Json;
public class AiAnalysisResultDto
{
    public string Summary { get; set; } = "";
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public int MatchScore { get; set; } = 0;
}

public class AiAnalysisService : IAiAnalysisService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IOpenAiClient _openAiClient;

    public AiAnalysisService(IApplicationRepository applicationRepository, IOpenAiClient openAiClient)
    {
        _applicationRepository = applicationRepository;
        _openAiClient = openAiClient;
    }

public async Task<AiAnalysisResultDto> AnalyzeApplicationAsync(Guid applicationId)
{
    var application = await _applicationRepository.GetByIdAsync(applicationId);

    if (application == null)
        throw new Exception("Application not found");

    Console.WriteLine("extracted text: ", application.Resume?.ExtractedText);
    var resumeText = application.Resume?.ExtractedText ?? "No resume content provided";
    var jobDescription = application.JobDescription ?? "No job description provided";

    var prompt = $@"
You are a job application assistant.

Analyze the resume against the job description.

Return ONLY valid JSON in this format:
{{
""summary"": string,
""strengths"": string[],
""weaknesses"": string[],
""suggestions"": string[],
""matchScore"": number
}}

Resume:
{resumeText}

Job Description:
{jobDescription}
";

    var aiResponse = await _openAiClient.GetCompletionAsync(prompt);

    // Clean response
    aiResponse = aiResponse.Trim()
                           .Replace("```json", "")
                           .Replace("```", "");

    Console.WriteLine("Raw AI Response:");
    Console.WriteLine(aiResponse);

    var result = JsonSerializer.Deserialize<AiAnalysisResultDto>(
        aiResponse,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    if (result == null)
        throw new Exception("Failed to parse AI response.");

    return result;
}
}