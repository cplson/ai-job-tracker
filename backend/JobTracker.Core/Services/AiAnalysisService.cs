using System.Text.Json;
using JobTracker.Core.Entities;
using JobTracker.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobTracker.Core.Services;

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
    private readonly IResumeTextExtractor _resumeTextExtractor;
    private readonly ILogger<AiAnalysisService> _logger;

    public AiAnalysisService(
        IApplicationRepository applicationRepository,
        IOpenAiClient openAiClient,
        IResumeTextExtractor resumeTextExtractor,
        ILogger<AiAnalysisService> logger)
    {
        _applicationRepository = applicationRepository;
        _openAiClient = openAiClient;
        _resumeTextExtractor = resumeTextExtractor;
        _logger = logger;
    }

    public async Task<AiAnalysisResultDto> AnalyzeApplicationAsync(Guid applicationId, Guid userId)
    {
        var application = await _applicationRepository.GetByIdForUserAsync(applicationId, userId);

        if (application == null)
            throw new KeyNotFoundException("Application not found");

        var resumeText = await ResolveResumeTextAsync(application);

        _logger.LogInformation(
            "Analyzing application {ApplicationId}: ResumeId={ResumeId}, ResumeTextLength={Length}, JobDescriptionLength={JobLength}",
            application.Id,
            application.ResumeId,
            resumeText.Length,
            application.JobDescription?.Length ?? 0);

        var jobDescription = string.IsNullOrWhiteSpace(application.JobDescription)
            ? "No job description provided"
            : application.JobDescription;

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

        aiResponse = aiResponse.Trim()
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "");

        _logger.LogDebug("Raw AI response for application {ApplicationId}: {Response}", applicationId, aiResponse);

        var result = JsonSerializer.Deserialize<AiAnalysisResultDto>(
            aiResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result == null)
            throw new InvalidOperationException("Failed to parse AI response.");

        return result;
    }

    private async Task<string> ResolveResumeTextAsync(Application application)
    {
        if (application.Resume == null)
        {
            _logger.LogWarning(
                "Application {ApplicationId} has no linked resume (ResumeId={ResumeId})",
                application.Id,
                application.ResumeId);
            return "No resume content provided";
        }

        if (!string.IsNullOrWhiteSpace(application.Resume.ExtractedText))
            return application.Resume.ExtractedText;

        if (!File.Exists(application.Resume.FilePath))
        {
            _logger.LogWarning(
                "Resume file missing for application {ApplicationId} at {FilePath}",
                application.Id,
                application.Resume.FilePath);
            return "No resume content provided";
        }

        try
        {
            var extracted = await _resumeTextExtractor.ExtractTextAsync(application.Resume.FilePath);
            if (string.IsNullOrWhiteSpace(extracted))
            {
                _logger.LogWarning(
                    "Text extraction returned empty for resume {ResumeId}",
                    application.Resume.Id);
                return "No resume content provided";
            }

            await _applicationRepository.UpdateResumeExtractedTextAsync(application.Resume.Id, extracted);
            application.Resume.ExtractedText = extracted;

            _logger.LogInformation(
                "Extracted and saved {Length} characters for resume {ResumeId}",
                extracted.Length,
                application.Resume.Id);

            return extracted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract resume text for application {ApplicationId}", application.Id);
            return "No resume content provided";
        }
    }
}
