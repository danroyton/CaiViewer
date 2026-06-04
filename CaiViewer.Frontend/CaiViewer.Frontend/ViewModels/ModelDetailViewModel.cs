using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;
using CaiViewer.Core.Domain;

namespace CaiViewer.Frontend.ViewModels;

public partial class ModelVersionTabViewModel : ViewModelBase
{
    public int SurrogateId { get; init; }
    public int CivitaiVersionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? BaseModel { get; init; }
    public string? PublishedAt { get; init; }
    public string? Status { get; init; }
    public string? Availability { get; init; }
    public string? Description { get; init; }
    public string? Air { get; init; }
    public string? DownloadUrl { get; init; }
    public string? TrainingDetails { get; init; }
    public int? StatDownloadCount { get; init; }
    public int? StatThumbsUp { get; init; }
    public double? StatRating { get; init; }
    public string TriggerWords { get; init; } = string.Empty;
    public string PrimaryZipPath { get; init; } = string.Empty;
    public ObservableCollection<ModelVersionFileRowViewModel> Files { get; } = [];
    public ObservableCollection<ImageItemViewModel> Images { get; } = [];
}

public partial class ModelVersionFileRowViewModel : ViewModelBase
{
    public string Name { get; init; } = string.Empty;
    public string? SizeKb { get; init; }
    public string? Type { get; init; }
    public string? Format { get; init; }
    public string? Fp { get; init; }
    public bool IsPrimary { get; init; }
    public string? HashAutov2 { get; init; }
    public string? HashAutov3 { get; init; }
    public string? PickleScan { get; init; }
    public string? VirusScan { get; init; }

    [RelayCommand]
    private void CopyAutov2() => _ = CopyToClipboardAsync(HashAutov2 ?? string.Empty);

    [RelayCommand]
    private void CopyAutov3() => _ = CopyToClipboardAsync(HashAutov3 ?? string.Empty);

    private static async Task CopyToClipboardAsync(string text)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard is not null) await clipboard.SetTextAsync(text);
        }
    }
}

public partial class ImageItemViewModel : ViewModelBase
{
    public string Url { get; init; } = string.Empty;
    public string? LocalFilename { get; init; }
    public int? NsfwLevel { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? Prompt { get; init; }
    public string ZipPath { get; init; } = string.Empty;
}

public partial class ModelDetailViewModel : ViewModelBase
{
    [ObservableProperty] private ModelVersionTabViewModel? _selectedVersion;

    public int ModelId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public string? CreatorUsername { get; init; }
    public int? NsfwLevel { get; init; }
    public string? Description { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string CivitAiUrl { get; init; } = string.Empty;

    public ObservableCollection<ModelVersionTabViewModel> Versions { get; } = [];

    public static async Task<ModelDetailViewModel?> LoadAsync(
        CaiDbContext db, int modelId, int civitaiVersionId, string repoRoot)
    {
        try
        {
            await using var conn = db.CreateConnection();
            var model = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<Core.Domain.Model>(conn,
                "SELECT * FROM Model WHERE id = @modelId", new { modelId });
            if (model is null) return null;

            var tags = await Dapper.SqlMapper.QueryAsync<string>(conn, """
                SELECT t.name FROM Tag t
                JOIN ModelTag mt ON mt.tag_id = t.id
                WHERE mt.model_id = @modelId ORDER BY t.name
                """, new { modelId });

            var versions = await Dapper.SqlMapper.QueryAsync<ModelVersion>(conn,
                "SELECT * FROM ModelVersion WHERE model_id = @modelId ORDER BY published_at DESC",
                new { modelId });

            var vm = new ModelDetailViewModel
            {
                ModelId = modelId,
                ModelName = model.Name,
                ModelType = model.Type,
                CreatorUsername = model.CreatorUsername,
                NsfwLevel = model.NsfwLevel,
                Description = model.Description,
                Tags = string.Join(", ", tags),
                CivitAiUrl = $"https://civitai.com/models/{modelId}"
            };

            foreach (var v in versions)
            {
                var zipRow = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ZipLocation>(conn,
                    "SELECT * FROM ZipLocation WHERE model_version_id = @id AND is_primary = 1",
                    new { id = v.Id });
                var zipPath = zipRow is not null
                    ? Path.Combine(repoRoot, zipRow.ZipPath)
                    : string.Empty;

                var words = await Dapper.SqlMapper.QueryAsync<string>(conn,
                    "SELECT word FROM ModelVersionTrainedWord WHERE model_version_id = @id", new { id = v.Id });

                var files = await Dapper.SqlMapper.QueryAsync<ModelVersionFile>(conn,
                    "SELECT * FROM ModelVersionFile WHERE model_version_id = @id", new { id = v.Id });

                var images = await Dapper.SqlMapper.QueryAsync<ModelVersionImage>(conn,
                    "SELECT * FROM ModelVersionImage WHERE model_version_id = @id ORDER BY sort_index",
                    new { id = v.Id });

                var tab = new ModelVersionTabViewModel
                {
                    SurrogateId = v.Id,
                    CivitaiVersionId = v.CivitaiVersionId,
                    Name = v.Name,
                    BaseModel = v.BaseModel,
                    PublishedAt = v.PublishedAt?.ToString("yyyy-MM-dd"),
                    Status = v.Status,
                    Availability = v.Availability,
                    Description = v.Description,
                    Air = v.Air,
                    DownloadUrl = v.DownloadUrl,
                    TrainingDetails = v.TrainingDetails,
                    StatDownloadCount = v.StatDownloadCount,
                    StatThumbsUp = v.StatThumbsUp,
                    StatRating = v.StatRating,
                    TriggerWords = string.Join(", ", words),
                    PrimaryZipPath = zipPath
                };

                foreach (var f in files)
                {
                    tab.Files.Add(new ModelVersionFileRowViewModel
                    {
                        Name = f.Name,
                        SizeKb = f.SizeKb.HasValue ? $"{f.SizeKb:N0} KB" : null,
                        Type = f.Type,
                        Format = f.Format,
                        Fp = f.Fp,
                        IsPrimary = f.PrimaryFile == 1,
                        HashAutov2 = f.HashAutov2,
                        HashAutov3 = f.HashAutov3,
                        PickleScan = f.PickleScanResult,
                        VirusScan = f.VirusScanResult
                    });
                }

                foreach (var img in images)
                {
                    tab.Images.Add(new ImageItemViewModel
                    {
                        Url = img.Url,
                        LocalFilename = img.LocalFilename,
                        NsfwLevel = img.NsfwLevel,
                        Width = img.Width,
                        Height = img.Height,
                        Prompt = img.Prompt,
                        ZipPath = zipPath
                    });
                }

                vm.Versions.Add(tab);
            }

            vm.SelectedVersion = vm.Versions.FirstOrDefault(v => v.CivitaiVersionId == civitaiVersionId)
                ?? vm.Versions.FirstOrDefault();

            return vm;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private async Task CopyModelIdAsync()
    {
        await CopyToClipboardAsync(ModelId.ToString());
    }

    [RelayCommand]
    private void OpenOnCivitAi()
    {
        var url = SelectedVersion is not null
            ? $"https://civitai.com/models/{ModelId}?modelVersionId={SelectedVersion.CivitaiVersionId}"
            : CivitAiUrl;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    [RelayCommand]
    private void OpenZipFolder()
    {
        if (SelectedVersion is null || string.IsNullOrEmpty(SelectedVersion.PrimaryZipPath)) return;
        var dir = Path.GetDirectoryName(SelectedVersion.PrimaryZipPath);
        if (dir is null) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true }); }
        catch { }
    }

    private static async Task CopyToClipboardAsync(string text)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard is not null) await clipboard.SetTextAsync(text);
        }
    }
}
