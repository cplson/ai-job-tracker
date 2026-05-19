using System.ComponentModel.DataAnnotations;

namespace JobTracker.API.DTOs.Resumes;

public class CreateResumeDto()
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = default!;
}