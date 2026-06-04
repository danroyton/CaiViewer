using System.Collections.ObjectModel;
using System.IO.Compression;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;
using CaiViewer.Core.Search;

namespace CaiViewer.Frontend.ViewModels;

public record SortItem(string Label, string Value)
{
    public override string ToString() => Label;
}

public partial class ModelVersionRowViewModel : ViewModelBase
{
    public int ModelVersionId { get; init; }
    public int CivitaiVersionId { get; init; }
    public int ModelId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public string VersionName { get; init; } = string.Empty;
    public string? BaseModel { get; init; }
    public string? CreatorUsername { get; init; }
    public int? NsfwLevel { get; init; }
    public string? PrimaryZipPath { get; init; }
    public double? StatRating { get; init; }
    public bool HasZip => !string.IsNullOrEmpty(PrimaryZipPath);

    // Spec 4: up to 3 preview images (one per model version) shown side-by-side
    public ObservableCollection<Bitmap> PreviewImages { get; } = [];

    // Kept for hover tooltip (spec 3) – first image
    public Bitmap? Thumbnail => PreviewImages.Count > 0 ? PreviewImages[0] : null;
}

public partial class BrowseViewModel : ViewModelBase
{
    private AppSettings _settings;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _currentPage;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private ModelVersionRowViewModel? _selectedRow;
    [ObservableProperty] private ModelDetailViewModel? _selectedDetail;

    // Filter state
    [ObservableProperty] private string? _filterType;
    [ObservableProperty] private string? _filterBaseModel;
    [ObservableProperty] private string? _filterCreator;
    [ObservableProperty] private int _maxNsfwLevel = 31;
    [ObservableProperty] private bool? _hasZipFilter;
    [ObservableProperty] private SortItem _selectedSortItem = SortItems_[0];

    // Sort option string kept for query building
    private string SortOption => SelectedSortItem.Value;

    public static readonly SortItem[] SortItems_ = [
        new("Name A-Z",        "NameAsc"),
        new("Name Z-A",        "NameDesc"),
        new("Newest first",    "NewestFirst"),
        new("Oldest first",    "OldestFirst"),
        new("Most downloaded", "MostDownloaded"),
        new("Highest rated",   "HighestRated"),
        new("NSFW level",      "NsfwLevelAsc"),
    ];

    public SortItem[] AvailableSortItems => SortItems_;

    // Tag filter
    [ObservableProperty] private string _tagInput = string.Empty;
    public ObservableCollection<string> ActiveTags { get; } = [];

    // Spec 8: PageSizeText for two-way text binding
    public string PageSizeText
    {
        get => PageSize.ToString();
        set { if (int.TryParse(value, out var v) && v > 0) PageSize = v; }
    }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public ObservableCollection<ModelVersionRowViewModel> Results { get; } = [];
    public ObservableCollection<string> AvailableTypes { get; } = [];
    public ObservableCollection<string> AvailableBaseModels { get; } = [];

    public BrowseViewModel(AppSettings settings)
    {
        _settings = settings;
        _pageSize = settings.PageSize;
    }

    public void RefreshSettings(AppSettings settings)
    {
        _settings = settings;
        PageSize = settings.PageSize;
    }

    partial void OnSearchTextChanged(string value) => _ = DebounceSearchAsync();
    partial void OnFilterTypeChanged(string? value) => _ = ExecuteSearchAsync();
    partial void OnFilterBaseModelChanged(string? value) => _ = ExecuteSearchAsync();
    partial void OnFilterCreatorChanged(string? value) => _ = ExecuteSearchAsync();
    partial void OnMaxNsfwLevelChanged(int value) => _ = ExecuteSearchAsync();
    partial void OnHasZipFilterChanged(bool? value) => _ = ExecuteSearchAsync();
    partial void OnSelectedSortItemChanged(SortItem value) => _ = ExecuteSearchAsync();
    partial void OnPageSizeChanged(int value) => _ = ExecuteSearchAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await ExecuteSearchAsync();

    [RelayCommand]
    private void IncrementPageSize() { PageSize = Math.Min(PageSize + 10, 500); OnPropertyChanged(nameof(PageSizeText)); }

    [RelayCommand]
    private void DecrementPageSize() { PageSize = Math.Max(PageSize - 10, 10); OnPropertyChanged(nameof(PageSizeText)); }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages - 1) CurrentPage++;
        await ExecuteSearchAsync(resetPage: false);
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 0) CurrentPage--;
        await ExecuteSearchAsync(resetPage: false);
    }

    [RelayCommand]
    private async Task NextTenPagesAsync()
    {
        CurrentPage = Math.Min(CurrentPage + 10, TotalPages - 1);
        await ExecuteSearchAsync(resetPage: false);
    }

    [RelayCommand]
    private async Task PreviousTenPagesAsync()
    {
        CurrentPage = Math.Max(CurrentPage - 10, 0);
        await ExecuteSearchAsync(resetPage: false);
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        var tag = TagInput.Trim();
        if (string.IsNullOrEmpty(tag) || ActiveTags.Contains(tag)) return;
        ActiveTags.Add(tag);
        TagInput = string.Empty;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task RemoveTagAsync(string tag)
    {
        ActiveTags.Remove(tag);
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ClearCreatorFilterAsync()
    {
        FilterCreator = null;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ClearTypeFilterAsync()
    {
        FilterType = null;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ClearBaseModelFilterAsync()
    {
        FilterBaseModel = null;
        await ExecuteSearchAsync();
    }

    partial void OnSelectedRowChanged(ModelVersionRowViewModel? value)
    {
        if (value is null) { SelectedDetail = null; return; }
        _ = LoadDetailAsync(value);
    }

    private async Task LoadDetailAsync(ModelVersionRowViewModel row)
    {
        if (string.IsNullOrEmpty(_settings.DatabasePath)) return;
        var db = new CaiDbContext(_settings.DatabasePath);
        SelectedDetail = await ModelDetailViewModel.LoadAsync(db, row.ModelId, row.CivitaiVersionId, _settings.CivitAiRepoPath, _settings.SafetensorsSearchPath);
    }

    private async Task DebounceSearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCts.Token);
            await ExecuteSearchAsync();
        }
        catch (OperationCanceledException) { }
    }

    private async Task ExecuteSearchAsync(bool resetPage = true)
    {
        if (string.IsNullOrEmpty(_settings.DatabasePath)) return;
        if (resetPage) CurrentPage = 0;

        IsLoading = true;
        try
        {
            var db = new CaiDbContext(_settings.DatabasePath);
            var svc = new SearchService(db);
            var q = new SearchQuery
            {
                FullText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Types = FilterType is null ? null : [FilterType],
                BaseModels = FilterBaseModel is null ? null : [FilterBaseModel],
                CreatorUsername = string.IsNullOrWhiteSpace(FilterCreator) ? null : FilterCreator,
                MaxNsfwLevel = MaxNsfwLevel < 31 ? MaxNsfwLevel : null,
                HasZip = HasZipFilter,
                Tags = ActiveTags.Count > 0 ? [.. ActiveTags] : null,
                TagsMatchAll = true,
                Sort = Enum.TryParse<SortOption>(SortOption, out var s) ? s : Core.Search.SortOption.NameAsc,
                Page = CurrentPage,
                PageSize = PageSize
            };

            var (rows, total) = await svc.SearchAsync(q);
            TotalCount = total;

            Results.Clear();
            foreach (var r in rows)
            {
                Results.Add(new ModelVersionRowViewModel
                {
                    ModelVersionId = r.ModelVersionId,
                    CivitaiVersionId = r.CivitaiVersionId,
                    ModelId = r.ModelId,
                    ModelName = r.ModelName,
                    ModelType = r.ModelType,
                    VersionName = r.VersionName,
                    BaseModel = r.BaseModel,
                    CreatorUsername = r.CreatorUsername,
                    NsfwLevel = r.NsfwLevel,
                    PrimaryZipPath = r.PrimaryZipPath,
                    StatRating = r.StatRating
                });
            }

            await LoadFilterOptionsAsync(db);
            StatusText = $"{total} versions found | DB: {_settings.DatabasePath}";

            // Issue 4: Load preview thumbnails from ZIP in background
            _ = LoadThumbnailsAsync(Results.ToList(), _settings.CivitAiRepoPath);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task LoadThumbnailsAsync(List<ModelVersionRowViewModel> rows, string repoRoot)
    {
        // Group rows by model: each row in the list is one result row for a model version.
        // We show up to 3 preview images (one per version zip) per result row.
        // Because results may show the same model multiple times (different versions),
        // collect ZIPs for the same ModelId together.
        var byModel = rows.GroupBy(r => r.ModelId);

        foreach (var group in byModel)
        {
            var zips = group
                .Where(r => !string.IsNullOrEmpty(r.PrimaryZipPath))
                .Select(r => Path.IsPathRooted(r.PrimaryZipPath!)
                    ? r.PrimaryZipPath!
                    : Path.Combine(repoRoot, r.PrimaryZipPath!))
                .Distinct()
                .Take(3)
                .ToList();

            var bitmaps = new List<Bitmap>();
            foreach (var zipPath in zips)
            {
                if (!File.Exists(zipPath)) continue;
                var bmp = await Task.Run(() =>
                {
                    try
                    {
                        using var zip = ZipFile.OpenRead(zipPath);
                        var entry = zip.Entries.FirstOrDefault(e =>
                            e.Name.EndsWith("_preview.webp", StringComparison.OrdinalIgnoreCase));
                        if (entry is null) return null;
                        using var stream = entry.Open();
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return new Bitmap(ms);
                    }
                    catch { return null; }
                });
                if (bmp is not null) bitmaps.Add(bmp);
            }

            // Assign the same list of bitmaps to every row in this model group
            foreach (var row in group)
            {
                row.PreviewImages.Clear();
                foreach (var b in bitmaps) row.PreviewImages.Add(b);
            }
        }
    }

    private async Task LoadFilterOptionsAsync(CaiDbContext db)
    {
        if (AvailableTypes.Count > 0 && AvailableBaseModels.Count > 0) return;
        try
        {
            await using var conn = db.CreateConnection();
            var types = await Dapper.SqlMapper.QueryAsync<string>(conn,
                "SELECT DISTINCT type FROM Model WHERE type IS NOT NULL ORDER BY type");
            var bases = await Dapper.SqlMapper.QueryAsync<string>(conn,
                "SELECT DISTINCT base_model FROM ModelVersion WHERE base_model IS NOT NULL ORDER BY base_model");
            AvailableTypes.Clear();
            foreach (var t in types) AvailableTypes.Add(t);
            AvailableBaseModels.Clear();
            foreach (var b in bases) AvailableBaseModels.Add(b);
        }
        catch { }
    }
}
