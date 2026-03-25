using JobTracker.Core.Enums;


public class ApplicationDetailDto
{
    public Guid Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}