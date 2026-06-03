namespace CaiViewer.Core.Domain;

public class ModelVersionFile
{
    public int Id { get; set; }
    public int ModelVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? SizeKb { get; set; }
    public string? Type { get; set; }
    public int PrimaryFile { get; set; }
    public string? Format { get; set; }
    public string? SizeClass { get; set; }
    public string? Fp { get; set; }
    public string? DownloadUrl { get; set; }
    public string? HashAutov1 { get; set; }
    public string? HashAutov2 { get; set; }
    public string? HashAutov3 { get; set; }
    public string? HashSha256 { get; set; }
    public string? HashCrc32 { get; set; }
    public string? HashBlake3 { get; set; }
    public string? PickleScanResult { get; set; }
    public string? VirusScanResult { get; set; }
    public DateTime? ScannedAt { get; set; }
    public string? LocalPath { get; set; }
}
