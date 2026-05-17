using JobTracker.Core.Interfaces;
using JobTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Persistance;
public class ApplicationRepository : IApplicationRepository
{
    private readonly AppDbContext _context;

    public ApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Application?> GetByIdForUserAsync(Guid applicationId, Guid userId)
    {
        return await _context.Applications
            .Include(a => a.Resume)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);
    }

    public async Task UpdateResumeExtractedTextAsync(Guid resumeId, string extractedText)
    {
        var resume = await _context.Resumes.FindAsync(resumeId);
        if (resume == null)
            return;

        resume.ExtractedText = extractedText;
        await _context.SaveChangesAsync();
    }
}