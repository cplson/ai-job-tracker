using JobTracker.Core.Interfaces;

public class AiAnalysisResultDto
{
    public string Summary { get; set; } = "";
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public int MatchScore { get; set; } // 0–100
}

public class AiAnalysisService : IAiAnalysisService
{
    private readonly IApplicationRepository _applicationRepository;

    public AiAnalysisService(IApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<AiAnalysisResultDto> AnalyzeApplicationAsync(Guid applicationId)
    {
        // 1. Load application + resume + job description
        // 2. Build prompt
        // 3. Call OpenAI
        // 4. Parse response into DTO
        // 5. Return DTO

        var application = await _applicationRepository.GetByIdAsync(applicationId);

        if (application == null)
            throw new Exception("Application not found");

        var resumeText = application.Resume?.ExtractedText ?? "";
        var jobDescription = application.JobDescription;

        return new AiAnalysisResultDto
        {
            Summary = "Mock summary",
            Strengths = new List<string> { "C#", "ASP.NET" },
            Weaknesses = new List<string> { "No cloud experience" },
            Suggestions = new List<string> { "Learn Azure" },
            MatchScore = 75
        };
    }
}