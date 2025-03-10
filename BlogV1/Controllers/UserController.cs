using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet("allusers")]
    public async Task<ActionResult<User>> AllUsers()
    {
        var allUsers = await _context.Users.ToListAsync();
        return Ok(allUsers);
    }
    [HttpPost("adduser")]
    public async Task<ActionResult<User>> AddUser([FromBody] User user)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if(existingUser != null)
        {
            return BadRequest(new{Message = "user already exists"});
        }
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return Ok(new{Message = "user added successfully"});
    }
    [HttpPut("updateuser/{id}")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User updatedUser)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if(existingUser == null)
        {
            return NotFound(new{Message = $"user with that ID {id} does not exist"});
        }
        existingUser.UserName = updatedUser.UserName ?? existingUser.UserName;
        existingUser.Email = updatedUser.Email ?? existingUser.Email;
        existingUser.Password = updatedUser.Password ?? existingUser.Password;

        await _context.SaveChangesAsync();
        return Ok(new{Message = "user has been successfully updated"});
    }
    [HttpDelete("deleteuser/{id}")]
    public async Task<ActionResult<User>> DeleteUser(int id)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if(existingUser == null)
        {
            return NotFound(new{Message = $"user with ID {id} does not exist"});
        }
        _context.Users.Remove(existingUser);
        await _context.SaveChangesAsync();
        return Ok(new{Message = "user has been deleted"});
    }
}