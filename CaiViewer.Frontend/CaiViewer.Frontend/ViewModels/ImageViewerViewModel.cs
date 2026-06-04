using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO.Compression;

namespace CaiViewer.Frontend.ViewModels;

public partial class ImageViewerViewModel : ViewModelBase
{
    private readonly IReadOnlyList<ImageItemViewModel> _all;

    [ObservableProperty] private Bitmap? _currentBitmap;
    [ObservableProperty] private string _currentPrompt = string.Empty;
    [ObservableProperty] private string _currentInfo = string.Empty;
    [ObservableProperty] private int _currentIndex;

    public ImageViewerViewModel(IReadOnlyList<ImageItemViewModel> images, int startIndex)
    {
        _all = images;
        _currentIndex = Math.Clamp(startIndex, 0, Math.Max(0, images.Count - 1));
        _ = LoadCurrentAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (_currentIndex < _all.Count - 1)
        {
            CurrentIndex++;
            await LoadCurrentAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousAsync()
    {
        if (_currentIndex > 0)
        {
            CurrentIndex--;
            await LoadCurrentAsync();
        }
    }

    private async Task LoadCurrentAsync()
    {
        if (_all.Count == 0) return;
        var img = _all[CurrentIndex];
        CurrentPrompt = img.Prompt ?? string.Empty;
        CurrentInfo = $"{CurrentIndex + 1}/{_all.Count}  {img.Width}×{img.Height}  NSFW {img.NsfwLevel}";
        CurrentBitmap = await Task.Run(() => LoadBitmapFromZip(img));
    }

    private static Bitmap? LoadBitmapFromZip(ImageItemViewModel img)
    {
        try
        {
            if (!string.IsNullOrEmpty(img.ZipPath) && File.Exists(img.ZipPath) &&
                !string.IsNullOrEmpty(img.LocalFilename))
            {
                using var zip = ZipFile.OpenRead(img.ZipPath);
                var entry = zip.Entries.FirstOrDefault(e =>
                    string.Equals(e.Name, img.LocalFilename, StringComparison.OrdinalIgnoreCase));
                if (entry is not null)
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    return new Bitmap(ms);
                }
            }
            // Fallback: URL
            if (!string.IsNullOrEmpty(img.Url))
            {
                using var http = new System.Net.Http.HttpClient();
                var bytes = http.GetByteArrayAsync(img.Url).GetAwaiter().GetResult();
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
        }
        catch { }
        return null;
    }

    [RelayCommand]
    private async Task CopyPromptAsync()
    {
        if (string.IsNullOrEmpty(CurrentPrompt)) return;
        await CopyToClipboardAsync(CurrentPrompt);
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
