
namespace JobTracker.Core.Interfaces;

public interface IAiAnalysisService
{
    Task<AiAnalysisResultDto> AnalyzeApplicationAsync(Guid applicationId);
}