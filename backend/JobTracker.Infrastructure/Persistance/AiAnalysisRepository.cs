using System.Text.Json;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Persistance;

public class AiAnalysisRepository : IAiAnalysisRepository
{
    public const string ApplicationAnalysisJobType = "ApplicationAnalysis";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _context;

    public AiAnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveAnalysisAsync(Guid applicationId, AiAnalysisResultDto result)
    {
        var json = JsonSerializer.Serialize(result, JsonOptions);

        var existing = await _context.AIJobs
            .Where(j => j.ApplicationId == applicationId && j.JobType == ApplicationAnalysisJobType)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Result = json;
            existing.Status = AIJobStatus.Completed;
        }
        else
        {
            _context.AIJobs.Add(new Core.Entities.AIJob
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                JobType = ApplicationAnalysisJobType,
                Status = AIJobStatus.Completed,
                Result = json
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<AiAnalysisResultDto?> GetLatestAnalysisAsync(Guid applicationId, Guid userId)
    {
        var ownsApplication = await _context.Applications
            .AnyAsync(a => a.Id == applicationId && a.UserId == userId);

        if (!ownsApplication)
            return null;

        var job = await _context.AIJobs
            .Where(j =>
                j.ApplicationId == applicationId &&
                j.JobType == ApplicationAnalysisJobType &&
                j.Status == AIJobStatus.Completed &&
                j.Result != "")
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync();

        if (job == null)
            return null;

        return JsonSerializer.Deserialize<AiAnalysisResultDto>(job.Result, JsonOptions);
    }
}
