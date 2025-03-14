using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//add the security namespace
using System.Security.Claims;

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
    [Authorize(Roles = "Admin")] //only admins can access this now
    public async Task<ActionResult<User>> AllUsers()
    {
        var allUsers = await _context.Users.ToListAsync();
        return Ok(allUsers);
    }
    [HttpPost("adduser")]
    //add authorize
    public async Task<ActionResult<User>> AddUser([FromBody] User user)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "user already exists" });
        }

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "user added successfully" });
    }
    [HttpPut("updateuser/{id}")]
    [Authorize]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User updatedUser)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            return NotFound(new { Message = $"user with that ID {id} does not exist" });
        }

        //----- add code blog for authorization
        var userIdFromToken = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        //admin can update all users but also user can update their own account
        if(userIdFromToken != id && userRole !="Admin")
        {
            return StatusCode(403, new{Message = "you cannot update other people's accounts"});
        }

        //only update fields if new values are provided(keep old values otherwise)

        existingUser.UserName = updatedUser.UserName ?? existingUser.UserName;
        existingUser.Email = updatedUser.Email ?? existingUser.Email;
        existingUser.Password = updatedUser.Password ?? existingUser.Password;

        await _context.SaveChangesAsync();
        Console.WriteLine("testing");
        Console.WriteLine(userIdFromToken);
        Console.WriteLine(userRole);
        return Ok(new { Message = "user has been successfully updated" });
    }
    [HttpDelete("deleteuser/{id}")]
    //add authorization
    [Authorize]
    public async Task<ActionResult<User>> DeleteUser(int id)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            return NotFound(new { Message = $"user with ID {id} does not exist" });
        }

        var userIdFromToken = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRoleFromToken = User.FindFirst(ClaimTypes.Role)?.Value;

        //only allow deletion if:
        //- user is Admin (userRoleFromToken == "Admin")
        //- or the user is deleting their own account (userIsFromToken == id)
        if(userIdFromToken !=id && userRoleFromToken != "Admin")
        {
            return StatusCode(403, new{Message = "you can only delete your own account"});
        }

        _context.Users.Remove(existingUser);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "user has been deleted" });
    }
}