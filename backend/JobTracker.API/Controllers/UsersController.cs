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

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    // [HttpGet]
    // public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    // {
    //     var users = await _context.Users
    //         .Select(u => new UserDto
    //         {
    //             Id = u.Id,
    //             Email = u.Email
    //         })
    //         .ToListAsync();

    //     return Ok(users);
    // }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<CreateUserDto>> Register(UserDto dto)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = hashedPassword
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = new UserDto
        {
            Id = user.Id,
            Email = user.Email
        };

        return Ok(result);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized("Could not get users id from JWT");
        
        Console.WriteLine($"inside put endpoint: {userIdClaim.Value}");

        if (!Guid.TryParse(userIdClaim.Value, out var loggedInUserId))
            return Unauthorized();

        if (id != loggedInUserId)
            return Unauthorized("Unauthorized due to id mismatch");

        var user = await _context.Users.FindAsync(loggedInUserId);

        if (user == null)
            return NotFound($"User with id {id} not found.");
           
        // Update email path
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                return Conflict("Email already in use.");
            user.Email = dto.Email;
        }

        // Update password path
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        _context.Update(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, UserDto dto)
    {
        if (id != dto.Id)
            return Unauthorized("Unauthorized request");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound($"User with id {id} not found.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent(); // 204
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto dto, [FromServices] JwtHelper jwtHelper)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password");

        var token = jwtHelper.GenerateToken(user);

        return Ok(new { token });
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

