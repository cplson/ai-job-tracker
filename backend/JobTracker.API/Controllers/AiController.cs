using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using JobTracker.API.DTOs.Resumes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JobTracker.Core.Enums;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    [HttpPost("analyze/{applicationId:guid}")]
    public async Task<IActionResult> Analyze(Guid applicationId)
    {
        // TODO: fetch application + resume
        // TODO: call AI service
        // TODO: return result

        return Ok(new {
            score = 75,
            suggestions = new[] {
                "Add more experience with Docker",
                "Highlight REST API work"
            },
            missingKeywords = new[] {
                "Kubernetes",
                "CI/CD"
            }
        });
    }
}