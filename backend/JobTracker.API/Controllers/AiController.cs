using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using JobTracker.API.DTOs.Resumes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JobTracker.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiAnalysisService _aiService;

    public AiController(IAiAnalysisService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("analyze/{applicationId:guid}")]
    public async Task<IActionResult> Analyze(Guid applicationId)
    {
        var result = await _aiService.AnalyzeApplicationAsync(applicationId);
        return Ok(result);
    }
}