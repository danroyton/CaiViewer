using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;
using CaiViewer.Core.Search;

namespace CaiViewer.Frontend.ViewModels;

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
}

public partial class BrowseViewModel : ViewModelBase
{
    private AppSettings _settings;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _currentPage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private ModelVersionRowViewModel? _selectedRow;
    [ObservableProperty] private ModelDetailViewModel? _selectedDetail;

    // Filter state
    [ObservableProperty] private string? _filterType;
    [ObservableProperty] private string? _filterBaseModel;
    [ObservableProperty] private int _maxNsfwLevel = 31;
    [ObservableProperty] private bool? _hasZipFilter;
    [ObservableProperty] private string _sortOption = "NameAsc";

    public ObservableCollection<ModelVersionRowViewModel> Results { get; } = [];
    public ObservableCollection<string> AvailableTypes { get; } = [];
    public ObservableCollection<string> AvailableBaseModels { get; } = [];

    public BrowseViewModel(AppSettings settings)
    {
        _settings = settings;
    }

    public void RefreshSettings(AppSettings settings)
    {
        _settings = settings;
    }

    partial void OnSearchTextChanged(string value) => _ = DebounceSearchAsync();
    partial void OnFilterTypeChanged(string? value) => _ = ExecuteSearchAsync();
    partial void OnFilterBaseModelChanged(string? value) => _ = ExecuteSearchAsync();
    partial void OnMaxNsfwLevelChanged(int value) => _ = ExecuteSearchAsync();
    partial void OnHasZipFilterChanged(bool? value) => _ = ExecuteSearchAsync();
    partial void OnSortOptionChanged(string value) => _ = ExecuteSearchAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await ExecuteSearchAsync();

    [RelayCommand]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await ExecuteSearchAsync(resetPage: false);
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 0) CurrentPage--;
        await ExecuteSearchAsync(resetPage: false);
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
        SelectedDetail = await ModelDetailViewModel.LoadAsync(db, row.ModelId, row.CivitaiVersionId, _settings.CivitAiRepoPath);
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
                MaxNsfwLevel = MaxNsfwLevel < 31 ? MaxNsfwLevel : null,
                HasZip = HasZipFilter,
                Sort = Enum.TryParse<SortOption>(SortOption, out var s) ? s : Core.Search.SortOption.NameAsc,
                Page = CurrentPage,
                PageSize = 50
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
            StatusText = $"{total} versions found | {_settings.DatabasePath}";
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
