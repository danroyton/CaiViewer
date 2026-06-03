namespace CaiViewer.Core.Domain;

public class Model
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Nsfw { get; set; }
    public int? NsfwLevel { get; set; }
    public int Poi { get; set; }
    public int Minor { get; set; }
    public int? AllowNoCredit { get; set; }
    public string? AllowCommercialUse { get; set; }
    public int? AllowDerivatives { get; set; }
    public int? AllowDifferentLicense { get; set; }
    public string? Cosmetic { get; set; }
    public string? CreatorUsername { get; set; }
    public int? StatDownloadCount { get; set; }
    public int? StatFavoriteCount { get; set; }
    public int? StatThumbsUp { get; set; }
    public int? StatThumbsDown { get; set; }
    public int? StatCommentCount { get; set; }
    public int? StatRatingCount { get; set; }
    public double? StatRating { get; set; }
    public int? StatTippedAmount { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
