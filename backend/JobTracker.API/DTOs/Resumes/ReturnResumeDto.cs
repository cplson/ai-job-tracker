namespace JobTracker.API.DTOs.Resumes;

public class ReturnResumeDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}