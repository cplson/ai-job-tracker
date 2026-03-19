using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;

    public TestController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser()
    {
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword"
        };

        _context.Users.Add(user);

        Console.WriteLine("Before SaveChanges");
        await _context.SaveChangesAsync();
        Console.WriteLine("After SaveChanges");

        return Ok(user);
    }
}