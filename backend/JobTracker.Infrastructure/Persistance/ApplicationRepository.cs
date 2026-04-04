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

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        return await _context.Applications
            .Include(a => a.Resume)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}