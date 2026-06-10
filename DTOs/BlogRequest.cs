using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CreateBlogRequest
{
    [Required]
    public string BlogName { get; set; } = string.Empty;

    [Required]
    public string BlogContent { get; set; } = string.Empty;

    public string? Image { get; set; }
}

public class PatchBlogRequest
{
    public string? BlogName { get; set; }
    public string? BlogContent { get; set; }
    public string? Image { get; set; }
}
