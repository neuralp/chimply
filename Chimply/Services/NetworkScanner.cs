using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Chimply.Models;

namespace Chimply.Services;

public class NetworkScanner : INetworkScanner
{
    private static readonly int[] TcpFallbackPorts = [80, 443, 22, 445, 3389];
    private const int MaxConcurrency = 50;
    private const int PingTimeoutMs = 1000;
    private const int TcpTimeoutMs = 500;

    public async Task ScanAsync(string cidr, IProgress<ScanResult> progress, CancellationToken cancellationToken)
    {
        var addresses = CidrParser.Parse(cidr);
        using var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

        var tasks = addresses.Select(ip => ScanHostAsync(ip, semaphore, progress, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private static async Task ScanHostAsync(IPAddress ip, SemaphoreSlim semaphore, IProgress<ScanResult> progress, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new ScanResult { IpAddress = ip.ToString() };
            var isUp = false;

            // Try ICMP ping first
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, PingTimeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    isUp = true;
                    result.RoundTripTime = reply.RoundtripTime;
                }
            }
            catch
            {
                // ICMP may fail due to permissions on Linux — fall through to TCP
            }

            // TCP fallback if ping failed
            if (!isUp)
            {
                foreach (var port in TcpFallbackPorts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await TryTcpConnectAsync(ip, port, cancellationToken))
                    {
                        isUp = true;
                        break;
                    }
                }
            }

            result.Status = isUp ? "Up" : "Down";

            // Reverse DNS (best-effort)
            if (isUp)
            {
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(ip);
                    result.Hostname = hostEntry.HostName;
                }
                catch
                {
                    // Ignore DNS resolution failures
                }
            }

            // MAC address lookup (best-effort)
            if (isUp)
            {
                result.MacAddress = MacAddressResolver.Resolve(ip) ?? string.Empty;
            }

            progress.Report(result);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<bool> TryTcpConnectAsync(IPAddress ip, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TcpTimeoutMs);
            await client.ConnectAsync(ip, port, cts.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate user cancellation
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return true; // Connection refused means the host is alive
        }
        catch
        {
            return false;
        }
    }
}
