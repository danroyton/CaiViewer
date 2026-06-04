using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;

namespace CaiViewer.Frontend.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    public event Action? SettingsSaved;

    [ObservableProperty] private string _databasePath;
    [ObservableProperty] private string _civitAiRepoPath;
    [ObservableProperty] private string _filescannerCliPath;
    [ObservableProperty] private int _defaultMaxNsfwLevel;
    [ObservableProperty] private int _thumbnailSize;
    [ObservableProperty] private bool _showBlurhashPlaceholder;
    [ObservableProperty] private bool _allowNetworkFallback;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        _databasePath = settings.DatabasePath;
        _civitAiRepoPath = settings.CivitAiRepoPath;
        _filescannerCliPath = settings.FilescannerCliPath;
        _defaultMaxNsfwLevel = settings.DefaultMaxNsfwLevel;
        _thumbnailSize = settings.ThumbnailSize;
        _showBlurhashPlaceholder = settings.ShowBlurhashPlaceholder;
        _allowNetworkFallback = settings.AllowNetworkFallback;
    }

    [RelayCommand]
    private async Task BrowseDatabaseAsync()
    {
        var path = await PickFileAsync("Select database file",
            [new Avalonia.Platform.Storage.FilePickerFileType("SQLite DB") { Patterns = ["*.db", "*.sqlite"] }]);
        if (path is not null) DatabasePath = path;
    }

    [RelayCommand]
    private async Task BrowseCivitAiRepoAsync()
    {
        var path = await PickFolderAsync("Select CivitAI repo root");
        if (path is not null) CivitAiRepoPath = path;
    }

    [RelayCommand]
    private async Task BrowseFilescannerCliAsync()
    {
        var path = await PickFileAsync("Select FilescannerCLI executable",
            [new Avalonia.Platform.Storage.FilePickerFileType("Executable") { Patterns = ["*.exe", "*"] }]);
        if (path is not null) FilescannerCliPath = path;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.DatabasePath = DatabasePath;
        _settings.CivitAiRepoPath = CivitAiRepoPath;
        _settings.FilescannerCliPath = FilescannerCliPath;
        _settings.DefaultMaxNsfwLevel = DefaultMaxNsfwLevel;
        _settings.ThumbnailSize = ThumbnailSize;
        _settings.ShowBlurhashPlaceholder = ShowBlurhashPlaceholder;
        _settings.AllowNetworkFallback = AllowNetworkFallback;
        _settings.Save();
        SettingsSaved?.Invoke();
    }

    private static async Task<string?> PickFileAsync(string title,
        IReadOnlyList<Avalonia.Platform.Storage.FilePickerFileType> filters)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dlg = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = title,
                FileTypeFilter = filters,
                AllowMultiple = false
            };
            var result = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(dlg);
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
        return null;
    }

    private static async Task<string?> PickFolderAsync(string title)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dlg = new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = title };
            var result = await desktop.MainWindow!.StorageProvider.OpenFolderPickerAsync(dlg);
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
        return null;
    }
}
