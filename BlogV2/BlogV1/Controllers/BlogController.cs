using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [Authorize(Roles = "Writer")]
    public async Task<ActionResult<Blog>> AddBlog([FromBody] Blog blog)
    {
        await _context.Blogs.AddAsync(blog);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "blog has been added" });
    }
    //update
    [HttpPut("updateblog/{id}")]
    public async Task<ActionResult> UpdateBlog(int id, [FromBody] Blog updatedBlog)
    {
        var existingBlog = await _context.Blogs.FindAsync(id);
        if (existingBlog == null)
        {
            return NotFound(new { Message = $"Blog with ID {id} is not found" });
        }
        //update only provided fields
        existingBlog.Title = updatedBlog.Title ?? existingBlog.Title;
        existingBlog.Content = updatedBlog.Content ?? existingBlog.Content;

        await _context.SaveChangesAsync();
        return Ok(new { Message = $"Blog with ID {id} has been updated" });
    }
    //delete
    [HttpDelete("deleteblog/{id}")]
    public async Task<ActionResult> DeleteBlog(int id)
    {
        var existingBlog = await _context.Blogs.FindAsync(id);
        if (existingBlog == null)
        {
            return NotFound(new { Message = $"Blog with ID {id} is not found" });
        }
        _context.Blogs.Remove(existingBlog);
        await _context.SaveChangesAsync();
        return Ok(new{Message = "blog has been deleted"});
    }
}