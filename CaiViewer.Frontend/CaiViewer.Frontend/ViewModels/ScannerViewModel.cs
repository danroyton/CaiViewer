using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Scanner;

namespace CaiViewer.Frontend.ViewModels;

public partial class ScannerViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private CancellationTokenSource? _cts;

    // Mode
    [ObservableProperty] private ScannerMode _mode = ScannerMode.FileScan;
    [ObservableProperty] private InputType _inputType = InputType.SingleFile;
    [ObservableProperty] private string _input = string.Empty;

    // Option flags
    [ObservableProperty] private bool _createZip = true;
    [ObservableProperty] private bool _allVersionsMeta = true;
    [ObservableProperty] private bool _fileOutput;
    [ObservableProperty] private bool _writeImageUrlLists;
    [ObservableProperty] private bool _historyOutput;
    [ObservableProperty] private bool _updateMode;
    [ObservableProperty] private bool _debugOutput;
    [ObservableProperty] private bool _fastFail;

    // Download options
    [ObservableProperty] private bool _primaryTensorIfMissing;
    [ObservableProperty] private bool _allVersionsPrimaryTensorOverwrite;
    [ObservableProperty] private bool _allVersionsPrimaryTensorIfMissing;
    [ObservableProperty] private bool _allVersionsAllFilesOverwrite;
    [ObservableProperty] private bool _allVersionsAllFilesIfMissing;

    // Inspect options
    [ObservableProperty] private bool _inspectRawHeader;
    [ObservableProperty] private bool _inspectAllMetadata;
    [ObservableProperty] private bool _inspectInteresting = true;

    // Named params
    [ObservableProperty] private string _centralCivitAiPath = string.Empty;
    [ObservableProperty] private int _maxDownloadSizeMb = 1600;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _workDirectory = string.Empty;

    // State
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _commandPreview = string.Empty;
    [ObservableProperty] private int _exitCode;
    [ObservableProperty] private bool _hasResult;

    public ObservableCollection<string> LogLines { get; } = [];

    public ScannerViewModel(AppSettings settings)
    {
        _settings = settings;
        _centralCivitAiPath = settings.CivitAiRepoPath;
        UpdatePreview();
    }

    private ScannerOptions BuildOptions() => new()
    {
        Mode = Mode,
        InputType = InputType,
        Input = Input,
        CreateZip = CreateZip,
        AllVersionsMeta = AllVersionsMeta,
        FileOutput = FileOutput,
        WriteImageUrlLists = WriteImageUrlLists,
        HistoryOutput = HistoryOutput,
        UpdateMode = UpdateMode,
        DebugOutput = DebugOutput,
        FastFail = FastFail,
        PrimaryTensorIfMissing = PrimaryTensorIfMissing,
        AllVersionsPrimaryTensorOverwrite = AllVersionsPrimaryTensorOverwrite,
        AllVersionsPrimaryTensorIfMissing = AllVersionsPrimaryTensorIfMissing,
        AllVersionsAllFilesOverwrite = AllVersionsAllFilesOverwrite,
        AllVersionsAllFilesIfMissing = AllVersionsAllFilesIfMissing,
        InspectRawHeader = InspectRawHeader,
        InspectAllMetadata = InspectAllMetadata,
        InspectInteresting = InspectInteresting,
        CentralCivitAiPath = string.IsNullOrWhiteSpace(CentralCivitAiPath) ? null : CentralCivitAiPath,
        MaxDownloadSizeMb = MaxDownloadSizeMb,
        ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey,
        WorkDirectory = string.IsNullOrWhiteSpace(WorkDirectory) ? null : WorkDirectory
    };

    private void UpdatePreview()
    {
        var exe = _settings.FilescannerCliPath;
        if (string.IsNullOrWhiteSpace(exe)) exe = "FilescannerCLI.exe";
        CommandPreview = ScannerService.BuildCommandPreview(exe, BuildOptions());
    }

    // Trigger preview update on all relevant property changes
    partial void OnModeChanged(ScannerMode value) => UpdatePreview();
    partial void OnInputTypeChanged(InputType value) => UpdatePreview();
    partial void OnInputChanged(string value) => UpdatePreview();
    partial void OnCreateZipChanged(bool value) => UpdatePreview();
    partial void OnAllVersionsMetaChanged(bool value) => UpdatePreview();
    partial void OnFileOutputChanged(bool value) => UpdatePreview();
    partial void OnWriteImageUrlListsChanged(bool value) => UpdatePreview();
    partial void OnHistoryOutputChanged(bool value) => UpdatePreview();
    partial void OnUpdateModeChanged(bool value) => UpdatePreview();
    partial void OnDebugOutputChanged(bool value) => UpdatePreview();
    partial void OnFastFailChanged(bool value) => UpdatePreview();
    partial void OnPrimaryTensorIfMissingChanged(bool value) => UpdatePreview();
    partial void OnAllVersionsPrimaryTensorOverwriteChanged(bool value) => UpdatePreview();
    partial void OnAllVersionsPrimaryTensorIfMissingChanged(bool value) => UpdatePreview();
    partial void OnAllVersionsAllFilesOverwriteChanged(bool value) => UpdatePreview();
    partial void OnAllVersionsAllFilesIfMissingChanged(bool value) => UpdatePreview();
    partial void OnCentralCivitAiPathChanged(string value) => UpdatePreview();
    partial void OnMaxDownloadSizeMbChanged(int value) => UpdatePreview();
    partial void OnWorkDirectoryChanged(string value) => UpdatePreview();

    [RelayCommand]
    private async Task BrowseInputAsync()
    {
        string? path;
        if (InputType is InputType.SingleFile)
            path = await PickFileAsync("Select .safetensors file",
                [new Avalonia.Platform.Storage.FilePickerFileType("SafeTensors") { Patterns = ["*.safetensors"] },
                 new Avalonia.Platform.Storage.FilePickerFileType("Text files") { Patterns = ["*.txt"] }]);
        else
            path = await PickFileAsync("Select list file",
                [new Avalonia.Platform.Storage.FilePickerFileType("Text files") { Patterns = ["*.txt"] }]);
        if (path is not null) Input = path;
    }

    [RelayCommand]
    private async Task BrowseCcdAsync()
    {
        var path = await PickFolderAsync("Select CivitAI repo directory");
        if (path is not null) CentralCivitAiPath = path;
    }

    [RelayCommand]
    private async Task BrowseWorkDirAsync()
    {
        var path = await PickFolderAsync("Select work directory");
        if (path is not null) WorkDirectory = path;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var exe = _settings.FilescannerCliPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            LogLines.Add("ERROR: FilescannerCLI path not configured. Go to Settings.");
            return;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        HasResult = false;
        LogLines.Clear();

        try
        {
            var svc = new ScannerService();
            var progress = new Progress<string>(line =>
            {
                LogLines.Add(line);
                if (LogLines.Count > 5000) LogLines.RemoveAt(0);
            });
            var result = await svc.RunAsync(exe, BuildOptions(), progress, _cts.Token);
            ExitCode = result.ExitCode;
            HasResult = true;
            LogLines.Add($"--- Process exited with code {result.ExitCode} ---");
        }
        catch (OperationCanceledException)
        {
            LogLines.Add("--- Scan cancelled ---");
        }
        catch (Exception ex)
        {
            LogLines.Add($"--- ERROR: {ex.Message} ---");
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private async Task CopyCommandAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard is not null) await clipboard.SetTextAsync(CommandPreview);
        }
    }

    private bool CanRun() => !IsRunning && !string.IsNullOrWhiteSpace(Input);

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
