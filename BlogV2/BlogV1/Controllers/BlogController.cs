using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("blogs")]
[ApiController]
public class BlogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public BlogController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet("allblogs")]
    public async Task<ActionResult<Blog>> AllBlogs()
    {
        //we have to add to include the Author
        var allblogs = await _context.Blogs.Include(b => b.Author).ThenInclude(a => a.Blogs).ToListAsync();
        return Ok(allblogs);
    }
    [HttpPost("addblog")]
    //add authorization
    [Authorize] 
    [AllowAnonymous]
    public async Task<ActionResult<Blog>> AddBlog([FromBody] Blog blog)
    {
        var userRoleFromToken = User.FindFirst(ClaimTypes.Role)?.Value;
        //allow only admins (0) and writers (1) to create blogs

        if(userRoleFromToken == null)
        {
            return StatusCode(403, new {Message = "you need to log in to create blogs"});
        }

        if(userRoleFromToken == "Reader")
        {
            return StatusCode(403, new{Message = "you need to log in to create a blog"}); 
        }

        if(userRoleFromToken != "Admin" && userRoleFromToken != "Writer")
        {
            return StatusCode(403, new{Message = "only admins or writers can create blogs"});
        } 

        await _context.Blogs.AddAsync(blog);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "blog has been added" });
    }
    //update
    [HttpPut("updateblog/{id}")]
    //only writer can update the blog
    [Authorize(Roles = "Writer")]
    public async Task<ActionResult> UpdateBlog(int id, [FromBody] Blog updatedBlog)
    {
        var existingBlog = await _context.Blogs.FindAsync(id);
        if (existingBlog == null)
        {
            return NotFound(new { Message = $"Blog with ID {id} is not found" });
        }

        var userIdFromToken = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        //check if logged in user is a writer
        if(existingBlog.AuthorId != userIdFromToken)
        {
            return StatusCode(403, new {Message = "you can update only your own blog"});
        }

        //update only provided fields
        existingBlog.Title = updatedBlog.Title ?? existingBlog.Title;
        existingBlog.Content = updatedBlog.Content ?? existingBlog.Content;

        await _context.SaveChangesAsync();
        return Ok(new { Message = $"Blog with ID {id} has been updated" });
    }
    //delete
    [HttpDelete("deleteblog/{id}")]
    //allow admins and writers only to delete
    //but writers can only delete their own blog
    [Authorize(Roles = "Admin, Writer")]
    public async Task<ActionResult> DeleteBlog(int id)
    {
        var existingBlog = await _context.Blogs.FindAsync(id);
        if (existingBlog == null)
        {
            return NotFound(new { Message = $"Blog with ID {id} is not found" });
        }

        //get the logged-in user ID from the token
        var userIdFromToken = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        //admin can delete all blogs, but writers can delete their own blog
        if(userRole != "Admin" && existingBlog.AuthorId !=userIdFromToken)
        {
            return StatusCode(403, new {Message = "you can delete your own blog"});
        }


        _context.Blogs.Remove(existingBlog);
        await _context.SaveChangesAsync();
        return Ok(new{Message = "blog has been deleted"});
    }
}