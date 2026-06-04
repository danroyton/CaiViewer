using CommunityToolkit.Mvvm.ComponentModel;
using CaiViewer.Core;

namespace CaiViewer.Frontend.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentTab;

    public BrowseViewModel Browse { get; }
    public ImportViewModel Import { get; }
    public ScannerViewModel Scanner { get; }
    public SettingsViewModel Settings { get; }

    public AppSettings AppSettings { get; }

    public ShellViewModel()
    {
        AppSettings = AppSettings.Load();
        Settings = new SettingsViewModel(AppSettings);
        Browse = new BrowseViewModel(AppSettings);
        Import = new ImportViewModel(AppSettings);
        Scanner = new ScannerViewModel(AppSettings);

        Settings.SettingsSaved += OnSettingsSaved;

        _currentTab = Browse;
    }

    private void OnSettingsSaved()
    {
        Browse.RefreshSettings(AppSettings);
    }

    partial void OnCurrentTabChanged(ViewModelBase value) { }
}
