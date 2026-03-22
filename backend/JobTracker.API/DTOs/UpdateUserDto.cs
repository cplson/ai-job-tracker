using System.ComponentModel.DataAnnotations;

namespace JobTracker.API.DTOs;

public class UpdateUserDto
{
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }
    [MinLength(6)]
    public string? Password { get; set; }
}