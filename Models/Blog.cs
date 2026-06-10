namespace PharmeasyAPI.Models;

public class Blog
{
    public int Id { get; set; }
    public string BlogName { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
