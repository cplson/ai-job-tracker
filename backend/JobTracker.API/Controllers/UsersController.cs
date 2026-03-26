using JobTracker.Infrastructure;
using JobTracker.Core.Entities;
using JobTracker.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Register(CreateUserDto dto, [FromServices] JwtHelper jwtHelper)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("Email already in use.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = hashedPassword
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user");
            return StatusCode(500, "Internal server error.");
        }

        var token = jwtHelper.GenerateToken(user);

        var returnedUser = new ReturnUserDto
        {
            Id = user.Id,
            Email = user.Email
        };
        
        return Ok(new { token, returnedUser });
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var loggedInUserId = JwtHelper.GetUserId(User);

        if (id != loggedInUserId)
            return Forbid();

        var user = await _context.Users.FindAsync(loggedInUserId);

        if (user == null)
            return NotFound($"User with id {id} not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        try
        {
            await _context.SaveChangesAsync();  
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user");
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var loggedInUserId = JwtHelper.GetUserId(User);

        if (id != loggedInUserId)
            return Forbid();

        var user = await _context.Users.FindAsync(loggedInUserId);

        if (user == null)
            return NotFound($"User with id {id} not found.");

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user");
            return StatusCode(500, "Internal server error");
        }

        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto dto, [FromServices] JwtHelper jwtHelper)
    {
        Console.WriteLine("incoming dto: ", dto);
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password");

        var token = jwtHelper.GenerateToken(user);

        var returnedUser = new ReturnUserDto
        {
            Id = user.Id,
            Email = user.Email
        };
        Console.WriteLine("token: ", token);
        Console.WriteLine("returnedUser: ", returnedUser);
        return Ok(new { token, returnedUser });
    }

    // TESTING
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { userId, email });
    }
}

