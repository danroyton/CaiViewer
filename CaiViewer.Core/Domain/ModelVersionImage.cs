namespace CaiViewer.Core.Domain;

public class ModelVersionImage
{
    public int Id { get; set; }
    public int ModelVersionId { get; set; }
    public int SortIndex { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? NsfwLevel { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Hash { get; set; }
    public int? HasMeta { get; set; }
    public int? OnSite { get; set; }
    public string? Availability { get; set; }
    public string? Prompt { get; set; }
    public string? LocalPath { get; set; }
    public string? LocalFilename { get; set; }
}
