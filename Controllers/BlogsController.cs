using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Blog/article content management.</summary>
[ApiController]
[Route("blogs")]
[Produces("application/json")]
public class BlogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlogsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a new blog post. (Admin only)</summary>
    /// <response code="200">Blog post created and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateBlogRequest req)
    {
        var blog = new Blog { BlogName = req.BlogName, BlogContent = req.BlogContent, Image = req.Image };
        _db.Blogs.Add(blog);
        await _db.SaveChangesAsync();
        return Ok(blog);
    }

    /// <summary>List all blog posts, newest first.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of blog posts.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.Blogs.OrderByDescending(b => b.CreatedAt).PaginateAsync(page, limit));
    }

    /// <summary>Retrieve a single blog post by ID.</summary>
    /// <param name="id">Blog post ID.</param>
    /// <response code="200">Blog post found and returned.</response>
    /// <response code="404">No blog post with the given ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var blog = await _db.Blogs.FindAsync(id);
        if (blog is null)
            return NotFound(new { message = "Blog not found." });
        return Ok(blog);
    }

    /// <summary>Partially update a blog post (only supplied fields are changed). (Admin only)</summary>
    /// <param name="id">Blog post ID.</param>
    /// <response code="200">Blog post updated and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No blog post with the given ID.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchBlogRequest req)
    {
        var blog = await _db.Blogs.FindAsync(id);
        if (blog is null)
            return NotFound(new { message = "Blog not found." });

        if (!string.IsNullOrWhiteSpace(req.BlogName)) blog.BlogName = req.BlogName;
        if (!string.IsNullOrWhiteSpace(req.BlogContent)) blog.BlogContent = req.BlogContent;
        if (req.Image != null) blog.Image = req.Image;
        blog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(blog);
    }

    /// <summary>Delete a blog post by ID. (Admin only)</summary>
    /// <param name="id">Blog post ID.</param>
    /// <response code="200">Blog post deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No blog post with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var blog = await _db.Blogs.FindAsync(id);
        if (blog is null)
            return NotFound(new { message = "Blog not found." });
        _db.Blogs.Remove(blog);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Blog deleted." });
    }
}
