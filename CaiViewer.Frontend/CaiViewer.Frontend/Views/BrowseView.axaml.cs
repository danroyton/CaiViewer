using Avalonia.Controls;
using Avalonia.Input;
using CaiViewer.Frontend.ViewModels;

namespace CaiViewer.Frontend.Views;

public partial class BrowseView : UserControl
{
    public BrowseView()
    {
        InitializeComponent();
    }

    // Issue 11: press Enter in tag input to add tag
    private void OnTagInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is BrowseViewModel vm)
            _ = vm.AddTagCommand.ExecuteAsync(null);
    }

    // Issue 5: click on image opens lightbox
    private void OnImageClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not ImageItemViewModel img) return;

        // Find the parent tab's image list
        var detail = (DataContext as BrowseViewModel)?.SelectedDetail;
        if (detail?.SelectedVersion is null) return;

        var images = detail.SelectedVersion.Images;
        var idx = images.IndexOf(img);

        var vm = new ImageViewerViewModel(images, idx);
        var win = new ImageViewerWindow(vm);
        win.Show();
    }
}
