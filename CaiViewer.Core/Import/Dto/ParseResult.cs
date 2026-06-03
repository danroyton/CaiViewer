namespace CaiViewer.Core.Import.Dto;

public class ParseResult
{
    public RootModel ModelDto { get; set; } = null!;
    public RootModelVersion VersionDto { get; set; } = null!;
    public int ImageCount { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string ModelJson { get; set; } = string.Empty;
    public string VersionJson { get; set; } = string.Empty;
}
