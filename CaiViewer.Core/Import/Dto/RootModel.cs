using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaiViewer.Core.Import.Dto;

public class RootModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("nsfw")]
    public bool Nsfw { get; set; }

    [JsonPropertyName("nsfwLevel")]
    public int? NsfwLevel { get; set; }

    [JsonPropertyName("poi")]
    public bool Poi { get; set; }

    [JsonPropertyName("minor")]
    public bool Minor { get; set; }

    [JsonPropertyName("allowNoCredit")]
    public bool? AllowNoCredit { get; set; }

    [JsonPropertyName("allowCommercialUse")]
    public JsonElement? AllowCommercialUse { get; set; }

    [JsonPropertyName("allowDerivatives")]
    public bool? AllowDerivatives { get; set; }

    [JsonPropertyName("allowDifferentLicense")]
    public bool? AllowDifferentLicense { get; set; }

    [JsonPropertyName("cosmetic")]
    public string? Cosmetic { get; set; }

    [JsonPropertyName("creator")]
    public CreatorDto? Creator { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("stats")]
    public ModelStatsDto? Stats { get; set; }

    [JsonPropertyName("modelVersions")]
    public List<ModelVersionSummaryDto>? ModelVersions { get; set; }
}

public class CreatorDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}

public class ModelStatsDto
{
    [JsonPropertyName("downloadCount")]
    public int? DownloadCount { get; set; }

    [JsonPropertyName("favoriteCount")]
    public int? FavoriteCount { get; set; }

    [JsonPropertyName("thumbsUpCount")]
    public int? ThumbsUpCount { get; set; }

    [JsonPropertyName("thumbsDownCount")]
    public int? ThumbsDownCount { get; set; }

    [JsonPropertyName("commentCount")]
    public int? CommentCount { get; set; }

    [JsonPropertyName("ratingCount")]
    public int? RatingCount { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("tippedAmountCount")]
    public int? TippedAmountCount { get; set; }
}

public class ModelVersionSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int? Index { get; set; }
}
