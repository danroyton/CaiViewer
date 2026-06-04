using Avalonia.Controls;
using Avalonia.Input;
using CaiViewer.Frontend.ViewModels;

namespace CaiViewer.Frontend.Views;

public partial class ImageViewerWindow : Window
{
    public ImageViewerWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    public ImageViewerWindow(ImageViewerViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        if (DataContext is not ImageViewerViewModel vm) return;
        if (e.Key == Key.Right || e.Key == Key.Down) _ = vm.NextCommand.ExecuteAsync(null);
        if (e.Key == Key.Left  || e.Key == Key.Up)   _ = vm.PreviousCommand.ExecuteAsync(null);
    }
}
