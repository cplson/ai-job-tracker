using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Core.Entities;

public class Resume
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(512)]
    public string FilePath { get; set; } = string.Empty;

    public string ExtractedText { get; set; } = string.Empty;

    public DateTime UploadedAt { get; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }
}