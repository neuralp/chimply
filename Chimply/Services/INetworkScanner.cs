using Chimply.Models;

namespace Chimply.Services;

public interface INetworkScanner
{
    Task ScanAsync(string cidr, IProgress<ScanResult> progress, CancellationToken cancellationToken);
}
