using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitOverview.Models;
using GitOverview.ViewModels;

namespace GitOverview.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var start = string.IsNullOrWhiteSpace(ViewModel.RootPath)
            ? null
            : await StorageProvider.TryGetFolderFromPathAsync(ViewModel.RootPath);

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose a folder containing git repositories",
            AllowMultiple = false,
            SuggestedStartLocation = start
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (path is null) return;

        ViewModel.RootPath = path;
        if (ViewModel.ScanCommand.CanExecute(null))
            await ViewModel.ScanCommand.ExecuteAsync(null);
    }

    private void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: RepoStatus repository })
            ViewModel.OpenFolderCommand.Execute(repository);
    }
}
