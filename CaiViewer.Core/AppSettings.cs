using System.Text.Json;

namespace CaiViewer.Core;

public class AppSettings
{
    public string DatabasePath { get; set; } = string.Empty;
    public string CivitAiRepoPath { get; set; } = string.Empty;
    public string FilescannerCliPath { get; set; } = string.Empty;
    public int DefaultMaxNsfwLevel { get; set; } = 31;
    public int ThumbnailSize { get; set; } = 1;
    public bool ShowBlurhashPlaceholder { get; set; } = true;
    public bool AllowNetworkFallback { get; set; } = false;

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CaiViewer", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return new AppSettings();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var path = SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
