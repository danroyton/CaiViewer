namespace CaiViewer.Core.Search;

public class SearchResultRow
{
    public int ModelVersionId { get; set; }
    public int CivitaiVersionId { get; set; }
    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string VersionName { get; set; } = string.Empty;
    public string? BaseModel { get; set; }
    public string? CreatorUsername { get; set; }
    public int? NsfwLevel { get; set; }
    public string? PrimaryZipPath { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? StatDownloadCount { get; set; }
    public double? StatRating { get; set; }
}
