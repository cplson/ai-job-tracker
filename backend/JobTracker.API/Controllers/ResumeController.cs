/*
--- TO DOs ---

- file type validation
- file size limits
- file deletion from disk on delete
- ensure filepath doesnt expose server path
- text extraction

*/

using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using JobTracker.API.DTOs.Resumes;
using JobTracker.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(AppDbContext context, ILogger<ResumesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static ReturnResumeDto MapToDto(Resume resume)
    {
        return new ReturnResumeDto
        {
            Id = resume.Id,
            FileName = Path.GetFileName(resume.FilePath),
            UploadedAt = resume.UploadedAt
        };
    }

    // 📤 Upload Resume
    [HttpPost]
    public async Task<ActionResult<ReturnResumeDto>> UploadResume([FromForm] CreateResumeDto dto)
    {
        var userId = JwtHelper.GetUserId(User);

        if (!ModelState.IsValid || dto.File == null || dto.File.Length == 0)
            return BadRequest("Valid file is required.");

        var filePath = await FileHelper.SaveFileAsync(dto.File);

        var resume = new Resume
        {
            UserId = userId,
            FilePath = filePath,
            ExtractedText = ""
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        return Ok(MapToDto(resume));
    }

    // 📥 Get all resumes for current user
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ReturnResumeDto>>> GetMyResumes()
    {
        var userId = JwtHelper.GetUserId(User);

        var resumes = await _context.Resumes
            .Where(r => r.UserId == userId)
            .ToListAsync();

        return Ok(resumes.Select(MapToDto));
    }

    // 📄 Get single resume
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReturnResumeDto>> GetResume(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        return Ok(MapToDto(resume));
    }

    // ✏️ Update resume (re-upload file)
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateResume(Guid id, [FromForm] UpdateResumeDto dto)
    {
        var userId = JwtHelper.GetUserId(User);

        if (!ModelState.IsValid || dto.File == null || dto.File.Length == 0)
            return BadRequest("Valid file is required.");

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        // delete old file
        FileHelper.DeleteFile(resume.FilePath);

        // save new file
        resume.FilePath = await FileHelper.SaveFileAsync(dto.File);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ❌ Delete resume
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        _context.Resumes.Remove(resume);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadResume(Guid id)
    {
        
        var userId = JwtHelper.GetUserId(User);

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        if (!System.IO.File.Exists(resume.FilePath))
            return NotFound("File not found on server.");
            

        var fileBytes = await System.IO.File.ReadAllBytesAsync(resume.FilePath);
        var fileName = Path.GetFileName(resume.FilePath);

        return File(fileBytes, "application/octet-stream", fileName);
    }
}