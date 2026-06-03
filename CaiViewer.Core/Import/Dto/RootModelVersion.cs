using System.Text.Json.Serialization;

namespace CaiViewer.Core.Import.Dto;

public class RootModelVersion
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("modelId")]
    public int ModelId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("baseModel")]
    public string? BaseModel { get; set; }

    [JsonPropertyName("baseModelType")]
    public string? BaseModelType { get; set; }

    [JsonPropertyName("trainedWords")]
    public List<string>? TrainedWords { get; set; }

    [JsonPropertyName("trainingStatus")]
    public string? TrainingStatus { get; set; }

    [JsonPropertyName("trainingDetails")]
    public string? TrainingDetails { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("uploadType")]
    public string? UploadType { get; set; }

    [JsonPropertyName("air")]
    public string? Air { get; set; }

    [JsonPropertyName("earlyAccessEndsAt")]
    public string? EarlyAccessEndsAt { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("nsfwLevel")]
    public int? NsfwLevel { get; set; }

    [JsonPropertyName("stats")]
    public VersionStatsDto? Stats { get; set; }

    [JsonPropertyName("files")]
    public List<VersionFileDto>? Files { get; set; }

    [JsonPropertyName("images")]
    public List<VersionImageDto>? Images { get; set; }
}

public class VersionStatsDto
{
    [JsonPropertyName("downloadCount")]
    public int? DownloadCount { get; set; }

    [JsonPropertyName("thumbsUpCount")]
    public int? ThumbsUpCount { get; set; }

    [JsonPropertyName("ratingCount")]
    public int? RatingCount { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }
}

public class VersionFileDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sizeKB")]
    public double? SizeKb { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("pickleScanResult")]
    public string? PickleScanResult { get; set; }

    [JsonPropertyName("virusScanResult")]
    public string? VirusScanResult { get; set; }

    [JsonPropertyName("scannedAt")]
    public DateTime? ScannedAt { get; set; }

    [JsonPropertyName("metadata")]
    public FileMetadataDto? Metadata { get; set; }

    [JsonPropertyName("hashes")]
    public FileHashesDto? Hashes { get; set; }

    [JsonPropertyName("localPath")]
    public string? LocalPath { get; set; }
}

public class FileMetadataDto
{
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("fp")]
    public string? Fp { get; set; }
}

public class FileHashesDto
{
    [JsonPropertyName("AutoV1")]
    public string? AutoV1 { get; set; }

    [JsonPropertyName("AutoV2")]
    public string? AutoV2 { get; set; }

    [JsonPropertyName("AutoV3")]
    public string? AutoV3 { get; set; }

    [JsonPropertyName("SHA256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("CRC32")]
    public string? Crc32 { get; set; }

    [JsonPropertyName("BLAKE3")]
    public string? Blake3 { get; set; }
}

public class VersionImageDto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("nsfwLevel")]
    public int? NsfwLevel { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("hasMeta")]
    public bool? HasMeta { get; set; }

    [JsonPropertyName("onSite")]
    public bool? OnSite { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("meta")]
    public ImageMetaDto? Meta { get; set; }
}

public class ImageMetaDto
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}
