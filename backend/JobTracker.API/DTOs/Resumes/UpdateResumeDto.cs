using System.ComponentModel.DataAnnotations;

namespace JobTracker.API.DTOs.Resumes;

public class UpdateResumeDto
{
    [Required]
    public IFormFile File { get; set; } = default!;
}