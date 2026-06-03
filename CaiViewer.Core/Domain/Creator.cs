namespace CaiViewer.Core.Domain;

public class Creator
{
    public string Username { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
