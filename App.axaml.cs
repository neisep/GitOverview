using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitOverview.Services;
using GitOverview.ViewModels;
using GitOverview.Views;

namespace GitOverview;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel(
                new FileSystemRepositoryLocator(),
                new GitStatusReader(new GitCommandRunner()));

            var window = new MainWindow { DataContext = viewModel };

            if (desktop.Args is [var rootPath])
            {
                viewModel.RootPath = rootPath;
                window.Opened += (_, _) => viewModel.ScanCommand.Execute(null);
            }

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
