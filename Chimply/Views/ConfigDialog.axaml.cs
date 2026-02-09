using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chimply.Views;

public partial class ConfigDialog : Window
{
    public ConfigDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
