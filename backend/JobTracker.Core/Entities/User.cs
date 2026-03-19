using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Core.Entities;

public class User
{
    public User()
    {
        Applications = new List<Application>();
        Resumes = new List<Resume>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; private set; }

    public ICollection<Resume> Resumes { get; private set; }
}