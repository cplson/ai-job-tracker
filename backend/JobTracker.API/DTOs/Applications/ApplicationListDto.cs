using JobTracker.Core.Enums;

public class ApplicationListDto
{
    public Guid Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}