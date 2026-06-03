namespace CaiViewer.Core.Search;

public class SearchQuery
{
    public string? FullText { get; set; }
    public List<string>? Types { get; set; }
    public List<string>? BaseModels { get; set; }
    public int? MaxNsfwLevel { get; set; }
    public List<string>? Tags { get; set; }
    public bool TagsMatchAll { get; set; } = true;
    public string? CreatorUsername { get; set; }
    public string? Status { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    public bool? HasZip { get; set; }
    public string? Availability { get; set; }
    public SortOption Sort { get; set; } = SortOption.NameAsc;
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 50;
}

public enum SortOption
{
    NameAsc,
    NameDesc,
    NewestFirst,
    OldestFirst,
    MostDownloaded,
    HighestRated,
    NsfwLevelAsc
}
