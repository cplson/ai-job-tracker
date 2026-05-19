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
using JobTracker.API;

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

    [HttpGet("analyze/{applicationId:guid}")]
    public async Task<IActionResult> GetSavedAnalysis(Guid applicationId)
    {
        var userId = JwtHelper.GetUserId(User);
        var result = await _aiService.GetSavedAnalysisAsync(applicationId, userId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("analyze/{applicationId:guid}")]
    public async Task<IActionResult> Analyze(Guid applicationId)
    {
        try
        {
            var userId = JwtHelper.GetUserId(User);
            var result = await _aiService.AnalyzeApplicationAsync(applicationId, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, ex.Message);
        }
    }
}