namespace CaiViewer.Core.Domain;

public class ZipLocation
{
    public int Id { get; set; }
    public int ModelVersionId { get; set; }
    public string ZipPath { get; set; } = string.Empty;
    public string? ZipSha256 { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public int IsPrimary { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
