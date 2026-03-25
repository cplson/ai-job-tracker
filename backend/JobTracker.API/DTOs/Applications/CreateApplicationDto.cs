using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;


public class CreateApplicationDto
{
    [Required]
    [MaxLength(256)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string JobDescription { get; set; } = string.Empty;

    public ApplicationStatus? Status { get; set; } // optional override
}