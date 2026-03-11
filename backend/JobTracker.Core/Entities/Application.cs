using JobTracker.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Core.Entities;

public class Application
{
    public Application()
    {
        AIJobs = new List<AIJob>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string JobTitle { get; set; } = string.Empty;

    [StringLength(2000)]
    public string JobDescription { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ICollection<AIJob> AIJobs { get; private set; }
}