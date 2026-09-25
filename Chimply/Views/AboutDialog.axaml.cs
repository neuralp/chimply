using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chimply.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        // <Version> in Chimply.csproj, baked into the assembly at build time
        var version = typeof(AboutDialog).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        VersionText.Text = $"Version {version}";
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
