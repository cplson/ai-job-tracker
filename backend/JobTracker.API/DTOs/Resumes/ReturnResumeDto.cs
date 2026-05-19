namespace JobTracker.API.DTOs.Resumes;

public class ReturnResumeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}