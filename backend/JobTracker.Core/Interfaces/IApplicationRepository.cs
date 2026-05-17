using JobTracker.Core.Entities;

namespace JobTracker.Core.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdForUserAsync(Guid applicationId, Guid userId);
    Task UpdateResumeExtractedTextAsync(Guid resumeId, string extractedText);
}
