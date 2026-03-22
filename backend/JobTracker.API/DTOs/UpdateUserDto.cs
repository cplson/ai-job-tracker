using System.ComponentModel.DataAnnotations;

namespace JobTracker.API.DTOs;

public class UpdateUserDto
{
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}