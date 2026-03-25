using JobTracker.Core.Enums;
using System.ComponentModel.DataAnnotations;


public class UpdateApplicationDto
{
    [MaxLength(256)]
    public string? Company { get; set; }

    [MaxLength(256)]
    public string? JobTitle { get; set; }

    [MaxLength(2000)]
    public string? JobDescription { get; set; }

    public ApplicationStatus? Status { get; set; }
}