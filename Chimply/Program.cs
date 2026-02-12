using Avalonia;
using Chimply.Converters;

namespace Chimply;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Ensure the comparer is preserved by the linker/trimmer when publishing (AOT/Trimmed)
        _ = IpAddressComparer.Instance;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
