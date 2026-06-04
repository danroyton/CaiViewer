using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;
using CaiViewer.Core.Import;

namespace CaiViewer.Frontend.ViewModels;

public partial class ImportViewModel : ViewModelBase
{
    private AppSettings _settings;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _rootPath = string.Empty;
    [ObservableProperty] private bool _recursive = true;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private string _summaryText = string.Empty;

    public ObservableCollection<string> LogLines { get; } = [];

    public ImportViewModel(AppSettings settings)
    {
        _settings = settings;
        _rootPath = settings.CivitAiRepoPath;
    }

    public void RefreshSettings(AppSettings settings)
    {
        _settings = settings;
        RootPath = settings.CivitAiRepoPath;
    }

    [RelayCommand]
    private async Task BrowseRootPathAsync()
    {
        var path = await PickFolderAsync();
        if (path is not null) RootPath = path;
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanDirectoryAsync()
    {
        if (string.IsNullOrEmpty(_settings.DatabasePath))
        {
            LogLines.Add("ERROR: No database path configured. Go to Settings first.");
            return;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        LogLines.Clear();
        SummaryText = string.Empty;
        ProgressValue = 0;

        try
        {
            var db = new CaiDbContext(_settings.DatabasePath);
            await db.EnsureCreatedAsync();
            var importSvc = new ImportService(db);
            var scanner = new ZipScanner(importSvc);

            var progress = new Progress<string>(line =>
            {
                LogLines.Add(line);
                if (LogLines.Count > 2000) LogLines.RemoveAt(0);
            });

            var summary = await scanner.ScanAsync(RootPath, Recursive, progress, _cts.Token);

            SummaryText = $"Done — Inserted: {summary.Inserted}  Updated: {summary.Updated}  " +
                          $"Aliases: {summary.AliasAdded}  Skipped: {summary.Skipped}  Failed: {summary.Failed}";
        }
        catch (OperationCanceledException)
        {
            SummaryText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            SummaryText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            ProgressValue = 100;
        }
    }

    [RelayCommand]
    private void CancelScan() => _cts?.Cancel();

    private bool CanScan() => !IsRunning && !string.IsNullOrWhiteSpace(RootPath);

    private static async Task<string?> PickFolderAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dlg = new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select root directory" };
            var result = await desktop.MainWindow!.StorageProvider.OpenFolderPickerAsync(dlg);
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
        return null;
    }
}
