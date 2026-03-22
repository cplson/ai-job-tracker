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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResumesController(AppDbContext context)
    {
        _context = context;
    }

    // 🔧 Helper: Get logged-in user ID
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    // 🔄 Helper: Map entity → DTO
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
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReturnResumeDto>> UploadResume([FromForm] CreateResumeDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!ModelState.IsValid || dto.File == null || dto.File.Length == 0)
            return BadRequest("Valid file is required.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

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
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ReturnResumeDto>>> GetMyResumes()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resumes = await _context.Resumes
            .Where(r => r.UserId == userId)
            .ToListAsync();

        var result = resumes.Select(MapToDto);

        return Ok(result);
    }

    // 📄 Get single resume
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<ReturnResumeDto>> GetResume(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        return Ok(MapToDto(resume));
    }

    // ✏️ Update resume (re-upload file)
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResume(Guid id, [FromForm] UpdateResumeDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!ModelState.IsValid || dto.File == null || dto.File.Length == 0)
            return BadRequest("Valid file is required.");

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        // (Optional later) delete old file from disk

        resume.FilePath = filePath;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ❌ Delete resume
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (resume == null)
            return NotFound();

        _context.Resumes.Remove(resume);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// using JobTracker.Infrastructure;
// using JobTracker.Core.Entities;
// using JobTracker.API.DTOs.Resumes;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.EntityFrameworkCore;
// using System.Security.Claims;


// namespace JobTracker.API.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class ResumesController : ControllerBase
// {
//     private readonly AppDbContext _context;

//     public ResumesController(AppDbContext context)
//     {
//         _context = context;
//     }

//     // 📤 Upload Resume
//     [Authorize]
//     [HttpPost]
//     public async Task<IActionResult> UploadResume([FromForm] CreateResumeDto dto)
//     {
//         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//         if (userId == null || !Guid.TryParse(userId, out var parsedUserId))
//             return Unauthorized();

//         if (dto.File == null || dto.File.Length == 0)
//             return BadRequest("File is required.");

//         // Create uploads directory if it doesn't exist
//         var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
//         if (!Directory.Exists(uploadsFolder))
//             Directory.CreateDirectory(uploadsFolder);

//         // Generate unique file name
//         var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
//         var filePath = Path.Combine(uploadsFolder, fileName);

//         // Save file
//         using (var stream = new FileStream(filePath, FileMode.Create))
//         {
//             await dto.File.CopyToAsync(stream);
//         }

//         var resume = new Resume
//         {
//             UserId = parsedUserId,
//             FilePath = filePath,
//             ExtractedText = ""
//         };

//         _context.Resumes.Add(resume);
//         await _context.SaveChangesAsync();

//         return Ok(new
//         {
//             resume.Id,
//             resume.FilePath,
//             resume.UploadedAt
//         });
//     }

//     // 📥 Get all resumes for current user
//     [Authorize]
//     [HttpGet("me")]
//     public async Task<IActionResult> GetMyResumes()
//     {
//         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//         if (userId == null || !Guid.TryParse(userId, out var parsedUserId))
//             return Unauthorized();

//         var resumes = await _context.Resumes
//             .Where(r => r.UserId == parsedUserId)
//             .Select(r => new
//             {
//                 r.Id,
//                 r.FilePath,
//                 r.UploadedAt
//             })
//             .ToListAsync();

//         return Ok(resumes);
//     }

//     // 📄 Get single resume
//     [Authorize]
//     [HttpGet("{id}")]
//     public async Task<IActionResult> GetResume(Guid id)
//     {
//         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//         if (userId == null || !Guid.TryParse(userId, out var parsedUserId))
//             return Unauthorized();

//         var resume = await _context.Resumes
//             .Where(r => r.Id == id && r.UserId == parsedUserId)
//             .Select(r => new
//             {
//                 r.Id,
//                 r.FilePath,
//                 r.UploadedAt
//             })
//             .FirstOrDefaultAsync();

//         if (resume == null)
//             return NotFound();

//         return Ok(resume);
//     }

//     // ✏️ Update resume (re-upload file)
//     [Authorize]
//     [HttpPut("{id}")]
//     public async Task<IActionResult> UpdateResume(Guid id, [FromForm] CreateResumeDto dto)
//     {
//         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//         if (userId == null || !Guid.TryParse(userId, out var parsedUserId))
//             return Unauthorized();

//         var resume = await _context.Resumes
//             .FirstOrDefaultAsync(r => r.Id == id && r.UserId == parsedUserId);

//         if (resume == null)
//             return NotFound();

//         if (dto.File == null || dto.File.Length == 0)
//             return BadRequest("File is required.");

//         var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

//         var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
//         var filePath = Path.Combine(uploadsFolder, fileName);

//         using (var stream = new FileStream(filePath, FileMode.Create))
//         {
//             await dto.File.CopyToAsync(stream);
//         }

//         resume.FilePath = filePath;

//         await _context.SaveChangesAsync();

//         return Ok();
//     }

//     // ❌ Delete resume
//     [Authorize]
//     [HttpDelete("{id}")]
//     public async Task<IActionResult> DeleteResume(Guid id)
//     {
//         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//         if (userId == null || !Guid.TryParse(userId, out var parsedUserId))
//             return Unauthorized();

//         var resume = await _context.Resumes
//             .FirstOrDefaultAsync(r => r.Id == id && r.UserId == parsedUserId);

//         if (resume == null)
//             return NotFound();

//         _context.Resumes.Remove(resume);
//         await _context.SaveChangesAsync();

//         return NoContent();
//     }
// }