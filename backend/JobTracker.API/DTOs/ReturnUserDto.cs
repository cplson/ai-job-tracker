namespace JobTracker.API.DTOs;

public class ReturnUserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;
}