using System.ComponentModel.DataAnnotations;

namespace JobTracker.API.DTOs.Resumes;

public class CreateResumeDto()
{
    [Required]
    public IFormFile File { get; set; } = default!;
    
}