using JobTracker.Core.Entities;

namespace JobTracker.Core.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id);
}
