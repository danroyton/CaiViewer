namespace CaiViewer.Core.Domain;

public class ModelVersion
{
    public int Id { get; set; }
    public int CivitaiVersionId { get; set; }
    public int SnapshotIndex { get; set; }
    public int ModelId { get; set; }
    public int? IndexNr { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Availability { get; set; }
    public string? BaseModel { get; set; }
    public string? BaseModelType { get; set; }
    public int? NsfwLevel { get; set; }
    public string? Description { get; set; }
    public string? UploadType { get; set; }
    public string? Air { get; set; }
    public string? DownloadUrl { get; set; }
    public string? TrainingStatus { get; set; }
    public string? TrainingDetails { get; set; }
    public DateTime? EarlyAccessEndsAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? StatDownloadCount { get; set; }
    public int? StatThumbsUp { get; set; }
    public int? StatRatingCount { get; set; }
    public double? StatRating { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
