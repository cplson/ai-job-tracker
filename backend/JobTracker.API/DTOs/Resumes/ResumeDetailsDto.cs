namespace JobTracker.API.DTOs.Resumes;

public class ResumeDetailsDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    // future use
    public string? ExtractedText { get; set; }
}