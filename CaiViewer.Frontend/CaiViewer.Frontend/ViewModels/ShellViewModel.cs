using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaiViewer.Core;
using CaiViewer.Core.Database;

namespace CaiViewer.Frontend.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentTab;

    [ObservableProperty]
    private bool _showFirstRunDialog;

    [ObservableProperty]
    private string _newProfileName = "Default";

    [ObservableProperty]
    private string _selectedProfileName;

    public BrowseViewModel Browse { get; }
    public ImportViewModel Import { get; }
    public ScannerViewModel Scanner { get; }
    public SettingsViewModel Settings { get; }

    public AppSettings AppSettings { get; }
    public ObservableCollection<string> ProfileNames { get; } = [];

    public ShellViewModel()
    {
        AppSettings = AppSettings.Load();
        Settings = new SettingsViewModel(AppSettings);
        Browse = new BrowseViewModel(AppSettings);
        Import = new ImportViewModel(AppSettings);
        Scanner = new ScannerViewModel(AppSettings);

        Settings.SettingsSaved += OnSettingsSaved;

        _currentTab = Browse;
        _selectedProfileName = AppSettings.ActiveProfileName;

        RefreshProfileNames();

        // Issue 2: if no DB configured, show first-run dialog
        if (string.IsNullOrEmpty(AppSettings.DatabasePath))
            ShowFirstRunDialog = true;
    }

    private void RefreshProfileNames()
    {
        ProfileNames.Clear();
        foreach (var p in AppSettings.Profiles)
            ProfileNames.Add(p.Name);
        if (ProfileNames.Count == 0)
        {
            ProfileNames.Add(AppSettings.ActiveProfileName);
        }
    }

    partial void OnSelectedProfileNameChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AppSettings.SwitchProfile(value);
        AppSettings.Save();
        Browse.RefreshSettings(AppSettings);
        Import.RefreshSettings(AppSettings);
        _ = Browse.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SelectDatabaseFolderAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dlg = new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select folder to create cai.db in"
            };
            var result = await desktop.MainWindow!.StorageProvider.OpenFolderPickerAsync(dlg);
            if (result.Count == 0) return;

            var folder = result[0].Path.LocalPath;
            var dbPath = Path.Combine(folder, "cai.db");
            AppSettings.DatabasePath = dbPath;

            var profileName = string.IsNullOrWhiteSpace(NewProfileName) ? "Default" : NewProfileName;
            AppSettings.ActiveProfileName = profileName;
            AppSettings.AddOrUpdateProfile(profileName, dbPath, folder);
            AppSettings.Save();

            // Ensure DB is initialised
            var ctx = new CaiDbContext(dbPath);
            await ctx.EnsureCreatedAsync();

            RefreshProfileNames();
            SelectedProfileName = profileName;

            Browse.RefreshSettings(AppSettings);
            Import.RefreshSettings(AppSettings);

            ShowFirstRunDialog = false;
        }
    }

    private void OnSettingsSaved()
    {
        Browse.RefreshSettings(AppSettings);
        Import.RefreshSettings(AppSettings);
        SelectedProfileName = AppSettings.ActiveProfileName;
        RefreshProfileNames();
    }

    partial void OnCurrentTabChanged(ViewModelBase value) { }
}
