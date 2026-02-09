using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chimply.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnConfigClick(object? sender, RoutedEventArgs e)
    {
        await new ConfigDialog().ShowDialog(this);
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        await new AboutDialog().ShowDialog(this);
    }
}
