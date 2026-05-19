using JobTracker.Core.Services;

namespace JobTracker.Core.Interfaces;

public interface IAiAnalysisRepository
{
    Task SaveAnalysisAsync(Guid applicationId, AiAnalysisResultDto result);
    Task<AiAnalysisResultDto?> GetLatestAnalysisAsync(Guid applicationId, Guid userId);
}
