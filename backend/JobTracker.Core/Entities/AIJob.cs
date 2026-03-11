using JobTracker.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Core.Entities;

public class AIJob
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    [StringLength(128)]
    public string JobType { get; set; } = string.Empty;

    public AIJobStatus Status { get; set; } = AIJobStatus.Pending;

    [StringLength(4000)]
    public string Result { get; set; } = string.Empty;

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    [ForeignKey("ApplicationId")]
    public Application? Application { get; set; }
}