using System.Text.Json;

namespace CaiViewer.Core;

public class DatabaseProfile
{
    public string Name { get; set; } = "Default";
    public string DatabasePath { get; set; } = string.Empty;
    public string CivitAiRepoPath { get; set; } = string.Empty;
}

public class AppSettings
{
    public string DatabasePath { get; set; } = string.Empty;
    public string CivitAiRepoPath { get; set; } = string.Empty;
    public string FilescannerCliPath { get; set; } = string.Empty;
    public int DefaultMaxNsfwLevel { get; set; } = 31;
    public int ThumbnailSize { get; set; } = 1;
    public bool ShowBlurhashPlaceholder { get; set; } = true;
    public bool AllowNetworkFallback { get; set; } = false;
    public int PageSize { get; set; } = 50;

    // Spec 6: additional path scanned recursively for safetensors / other model files
    public string SafetensorsSearchPath { get; set; } = string.Empty;

    // Multi-profile support
    public List<DatabaseProfile> Profiles { get; set; } = [];
    public string ActiveProfileName { get; set; } = "Default";

    private static string BaseDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CaiViewer");

    private static string SettingsPath => Path.Combine(BaseDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return new AppSettings();
            var json = File.ReadAllText(path);
            var s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            // Ensure active profile is always in the list
            if (!s.Profiles.Any(p => p.Name == s.ActiveProfileName))
            {
                s.Profiles.Insert(0, new DatabaseProfile
                {
                    Name = s.ActiveProfileName,
                    DatabasePath = s.DatabasePath,
                    CivitAiRepoPath = s.CivitAiRepoPath
                });
            }
            return s;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SwitchProfile(string profileName)
    {
        var profile = Profiles.FirstOrDefault(p => p.Name == profileName);
        if (profile is null) return;
        // Save current state back into active profile slot
        SyncCurrentToProfile();
        ActiveProfileName = profileName;
        DatabasePath = profile.DatabasePath;
        CivitAiRepoPath = profile.CivitAiRepoPath;
    }

    public void AddOrUpdateProfile(string name, string dbPath, string repoPath)
    {
        var existing = Profiles.FirstOrDefault(p => p.Name == name);
        if (existing is not null)
        {
            existing.DatabasePath = dbPath;
            existing.CivitAiRepoPath = repoPath;
        }
        else
        {
            Profiles.Add(new DatabaseProfile { Name = name, DatabasePath = dbPath, CivitAiRepoPath = repoPath });
        }
    }

    private void SyncCurrentToProfile()
    {
        var active = Profiles.FirstOrDefault(p => p.Name == ActiveProfileName);
        if (active is not null)
        {
            active.DatabasePath = DatabasePath;
            active.CivitAiRepoPath = CivitAiRepoPath;
        }
    }

    public void Save()
    {
        SyncCurrentToProfile();
        var path = SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
