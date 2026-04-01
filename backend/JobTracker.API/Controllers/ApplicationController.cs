using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
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
    private readonly AppDbContext _context;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(AppDbContext context, ILogger<ApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetAll()
    {
        var userId = JwtHelper.GetUserId(User);

        var apps = await _context.Applications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationListDto
            {
                Id = a.Id,
                Company = a.Company,
                JobTitle = a.JobTitle,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(apps);
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
                CreatedAt = a.CreatedAt
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

        var app = new Application
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Company = dto.Company,
            JobTitle = dto.JobTitle,
            JobDescription = dto.JobDescription,
            Status = dto.Status ?? ApplicationStatus.Draft
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

        if (dto.Company != null) app.Company = dto.Company;
        if (dto.JobTitle != null) app.JobTitle = dto.JobTitle;
        if (dto.JobDescription != null) app.JobDescription = dto.JobDescription;
        if (dto.Status.HasValue) app.Status = dto.Status.Value;

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
}