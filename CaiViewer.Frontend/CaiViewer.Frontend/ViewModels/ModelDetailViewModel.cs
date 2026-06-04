using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;
using CaiViewer.Core.Domain;

namespace CaiViewer.Frontend.ViewModels;

public enum ZipStatus { Unknown, Present, Missing, NoSafetensors }

public partial class ModelVersionTabViewModel : ViewModelBase
{
    public int SurrogateId { get; init; }
    public int CivitaiVersionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? BaseModel { get; init; }
    public string? BaseModelType { get; init; }
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
    public ZipStatus ZipStatus { get; init; } = ZipStatus.Unknown;

    /// <summary>Status icon: ✅ zip+safetensors present, ❌ zip missing, ⚠ zip present but no safetensors.</summary>
    public string ZipStatusIcon => ZipStatus switch
    {
        ZipStatus.Present       => "✅",
        ZipStatus.Missing       => "❌",
        ZipStatus.NoSafetensors => "⚠️",
        _                       => "❓"
    };

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
    public string? LocalPath { get; init; }

    /// <summary>true = file physically exists on disk</summary>
    public bool IsLocallyPresent => !string.IsNullOrEmpty(LocalPath) && File.Exists(LocalPath);

    public string LocalPresenceIcon => IsLocallyPresent ? "✅" : "❌";

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
    [ObservableProperty] private bool _tagsExpanded;

    public int ModelId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public string? CreatorUsername { get; init; }
    public int? NsfwLevel { get; init; }
    public string? Description { get; init; }
    public string Tags { get; init; } = string.Empty;
    public ObservableCollection<string> TagList { get; } = [];
    public string CivitAiUrl { get; init; } = string.Empty;
    public string HuggingFaceUrl { get; init; } = string.Empty;
    public string CreativityUrl { get; init; } = string.Empty;

    public ObservableCollection<ModelVersionTabViewModel> Versions { get; } = [];

    public static async Task<ModelDetailViewModel?> LoadAsync(
        CaiDbContext db, int modelId, int civitaiVersionId, string repoRoot, string safetensorsSearchPath = "")
    {
        try
        {
            await using var conn = db.CreateConnection();
            var model = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<Core.Domain.Model>(conn, """
                SELECT
                    id AS Id,
                    name AS Name,
                    description AS Description,
                    type AS Type,
                    creator_username AS CreatorUsername,
                    nsfw_level AS NsfwLevel
                FROM Model
                WHERE id = @modelId
                """, new { modelId });
            if (model is null) return null;

            var tags = (await Dapper.SqlMapper.QueryAsync<string>(conn, """
                SELECT t.name FROM Tag t
                JOIN ModelTag mt ON mt.tag_id = t.id
                WHERE mt.model_id = @modelId ORDER BY t.name
                """, new { modelId })).ToList();

            var versions = await Dapper.SqlMapper.QueryAsync<ModelVersion>(conn, """
                SELECT
                    id AS Id,
                    civitai_version_id AS CivitaiVersionId,
                    snapshot_index AS SnapshotIndex,
                    model_id AS ModelId,
                    name AS Name,
                    status AS Status,
                    availability AS Availability,
                    base_model AS BaseModel,
                    base_model_type AS BaseModelType,
                    description AS Description,
                    air AS Air,
                    download_url AS DownloadUrl,
                    training_details AS TrainingDetails,
                    published_at AS PublishedAt,
                    stat_download_count AS StatDownloadCount,
                    stat_thumbs_up AS StatThumbsUp,
                    stat_rating AS StatRating
                FROM ModelVersion
                WHERE model_id = @modelId
                ORDER BY published_at DESC
                """, new { modelId });

            var vm = new ModelDetailViewModel
            {
                ModelId = modelId,
                ModelName = model.Name,
                ModelType = model.Type,
                CreatorUsername = model.CreatorUsername,
                NsfwLevel = model.NsfwLevel,
                Description = HtmlHelper.ToPlainText(model.Description),
                Tags = string.Join(", ", tags),
                // Issue 7: link only to model page, no version ID
                CivitAiUrl = $"https://civitai.com/models/{modelId}",
                HuggingFaceUrl = $"https://huggingface.co/models?search={Uri.EscapeDataString(model.Name)}",
                CreativityUrl = $"https://www.creativity.ai/search?q={Uri.EscapeDataString(model.Name)}"
            };

            foreach (var tag in tags) vm.TagList.Add(tag);

            foreach (var v in versions)
            {
                var zipRow = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ZipLocation>(conn,
                    "SELECT * FROM ZipLocation WHERE model_version_id = @id AND is_primary = 1",
                    new { id = v.Id });
                var zipPath = zipRow is not null
                    ? Path.Combine(repoRoot, zipRow.ZipPath)
                    : string.Empty;

                // Spec 6: determine file presence: sibling dir of ZIP + optional search path
                var zipStatus = ZipStatus.Unknown;
                if (!string.IsNullOrEmpty(zipPath))
                {
                    if (File.Exists(zipPath))
                    {
                        var zipDir = Path.GetDirectoryName(zipPath) ?? string.Empty;
                        bool hasSafetensors = (Directory.Exists(zipDir) &&
                            Directory.EnumerateFiles(zipDir, "*.safetensors", SearchOption.TopDirectoryOnly).Any())
                            || (!string.IsNullOrEmpty(safetensorsSearchPath) && Directory.Exists(safetensorsSearchPath) &&
                                Directory.EnumerateFiles(safetensorsSearchPath, "*.safetensors", SearchOption.AllDirectories).Any());
                        zipStatus = hasSafetensors ? ZipStatus.Present : ZipStatus.NoSafetensors;
                    }
                    else
                    {
                        zipStatus = ZipStatus.Missing;
                    }
                }

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
                    BaseModelType = v.BaseModelType,
                    PublishedAt = v.PublishedAt?.ToString("yyyy-MM-dd"),
                    Status = v.Status,
                    Availability = v.Availability,
                    Description = HtmlHelper.ToPlainText(v.Description),
                    Air = v.Air,
                    DownloadUrl = v.DownloadUrl,
                    TrainingDetails = v.TrainingDetails,
                    StatDownloadCount = v.StatDownloadCount,
                    StatThumbsUp = v.StatThumbsUp,
                    StatRating = v.StatRating,
                    TriggerWords = string.Join(", ", words),
                    PrimaryZipPath = zipPath,
                    ZipStatus = zipStatus
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
                        VirusScan = f.VirusScanResult,
                        LocalPath = f.LocalPath
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
    private void ToggleTagsExpanded() => TagsExpanded = !TagsExpanded;

    [RelayCommand]
    private async Task CopyModelIdAsync()
    {
        await CopyToClipboardAsync(ModelId.ToString());
    }

    [RelayCommand]
    private void OpenOnCivitAi()
    {
        // Issue 7: always link to model page only (no modelVersionId param)
        var url = CivitAiUrl;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    [RelayCommand]
    private void OpenOnHuggingFace()
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(HuggingFaceUrl) { UseShellExecute = true }); }
        catch { }
    }

    [RelayCommand]
    private void OpenOnCreativity()
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(CreativityUrl) { UseShellExecute = true }); }
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
