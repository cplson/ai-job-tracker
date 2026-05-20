using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using JobTracker.API.DTOs;
using JobTracker.API.DTOs.Resumes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JobTracker.Core.Enums;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly AppDbContext _context;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(AppDbContext context, ILogger<ApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var userId = JwtHelper.GetUserId(User);

            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Applications.Where(a => a.UserId == userId);
            query = ApplySearch(query, search);
            query = ApplySort(query, sortBy, sortDescending);

            var totalCount = await query.CountAsync();

            var apps = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ApplicationDetailDto
                {
                    Id = a.Id,
                    Company = a.Company,
                    JobTitle = a.JobTitle,
                    JobDescription = a.JobDescription,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt,
                    ResumeId = a.ResumeId,
                    ResumeFileName = a.Resume != null ? a.Resume.Name : null
                })
                .ToListAsync();

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new PagedResultDto<ApplicationDetailDto>
            {
                Items = apps,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list applications");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);

        var app = await _context.Applications
            .Where(a => a.Id == id && a.UserId == userId)
            .Select(a => new ApplicationDetailDto
            {
                Id = a.Id,
                Company = a.Company,
                JobTitle = a.JobTitle,
                JobDescription = a.JobDescription,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                ResumeId = a.ResumeId
            })
            .FirstOrDefaultAsync();

        if (app == null)
            return NotFound();

        return Ok(app);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = JwtHelper.GetUserId(User);

        if (dto.ResumeId.HasValue)
        {
            var resumeOwned = await _context.Resumes
                .AnyAsync(r => r.Id == dto.ResumeId.Value && r.UserId == userId);
            if (!resumeOwned)
                return BadRequest("Resume not found.");
        }

        var app = new Application
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Company = dto.Company,
            JobTitle = dto.JobTitle,
            JobDescription = dto.JobDescription,
            Status = dto.Status ?? ApplicationStatus.Draft,
            ResumeId = dto.ResumeId
        };

        _context.Applications.Add(app);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create application");
            return StatusCode(500, "Internal server error");
        }

        return CreatedAtAction(nameof(GetById), new { id = app.Id }, new { app.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateApplicationDto dto)
    {
        Console.WriteLine("inside Put endpoint.");
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = JwtHelper.GetUserId(User);

        var app = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (app == null)
            return NotFound();

        if (dto.ResumeId.HasValue)
        {
            var resumeOwned = await _context.Resumes
                .AnyAsync(r => r.Id == dto.ResumeId.Value && r.UserId == userId);
            if (!resumeOwned)
                return BadRequest("Resume not found.");
        }

        if (dto.Company != null) app.Company = dto.Company;
        if (dto.JobTitle != null) app.JobTitle = dto.JobTitle;
        if (dto.JobDescription != null) app.JobDescription = dto.JobDescription;
        if (dto.Status.HasValue) app.Status = dto.Status.Value;
        if (dto.ResumeId != null) app.ResumeId = dto.ResumeId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update application");
            return StatusCode(500, "Internal server error");
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);

        var app = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (app == null)
            return NotFound();

        _context.Applications.Remove(app);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete application");
            return StatusCode(500, "Internal server error");
        }

        return NoContent();
    }

    private static IQueryable<Application> ApplySearch(IQueryable<Application> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLower();
        var matchingStatuses = Enum.GetValues<ApplicationStatus>()
            .Where(s => s.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return query.Where(a =>
            a.Company.ToLower().Contains(term) ||
            a.JobTitle.ToLower().Contains(term) ||
            matchingStatuses.Contains(a.Status) ||
            (a.Resume != null && a.Resume.Name.ToLower().Contains(term)));
    }

    private static IQueryable<Application> ApplySort(
        IQueryable<Application> query,
        string? sortBy,
        bool sortDescending)
    {
        return (sortBy?.ToLowerInvariant(), sortDescending) switch
        {
            ("company", false) => query.OrderBy(a => a.Company),
            ("company", true) => query.OrderByDescending(a => a.Company),
            ("jobtitle", false) => query.OrderBy(a => a.JobTitle),
            ("jobtitle", true) => query.OrderByDescending(a => a.JobTitle),
            ("status", false) => query.OrderBy(a => a.Status),
            ("status", true) => query.OrderByDescending(a => a.Status),
            ("resumefilename", false) => query.OrderBy(a => a.Resume != null ? a.Resume.Name : ""),
            ("resumefilename", true) => query.OrderByDescending(a => a.Resume != null ? a.Resume.Name : ""),
            ("createdat", false) => query.OrderBy(a => a.CreatedAt),
            ("createdat", true) => query.OrderByDescending(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt),
        };
    }
}