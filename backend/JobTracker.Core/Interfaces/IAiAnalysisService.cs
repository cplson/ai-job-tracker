using JobTracker.Core.Services;

namespace JobTracker.Core.Interfaces;

public interface IAiAnalysisService
{
    Task<AiAnalysisResultDto> AnalyzeApplicationAsync(Guid applicationId, Guid userId);
    Task<AiAnalysisResultDto?> GetSavedAnalysisAsync(Guid applicationId, Guid userId);
}