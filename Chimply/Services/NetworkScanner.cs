using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Chimply.Models;

namespace Chimply.Services;

public class NetworkScanner : INetworkScanner
{
    private static readonly int[] TcpFallbackPorts = [80, 443, 22, 445, 3389];
    private static readonly int[] DisplayPorts = [21, 22, 80, 443];
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

            // TCP fallback if ping failed — connection refused still means host is alive
            if (!isUp)
            {
                foreach (var port in TcpFallbackPorts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await IsHostAliveOnPortAsync(ip, port, cancellationToken))
                    {
                        isUp = true;
                        break;
                    }
                }
            }

            result.Status = isUp ? "Up" : "Down";

            if (isUp)
            {
                // Port scan — check common ports in parallel (open = accepting connections)
                var portTasks = DisplayPorts.Select(async port =>
                    await IsPortOpenAsync(ip, port, cancellationToken) ? port : -1);
                var portResults = await Task.WhenAll(portTasks);
                result.OpenPorts = portResults.Where(p => p > 0).OrderBy(p => p).ToList();

                // Reverse DNS (best-effort)
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(ip);
                    result.Hostname = hostEntry.HostName;
                }
                catch
                {
                    // Ignore DNS resolution failures
                }

                // MAC address lookup (best-effort)
                result.MacAddress = MacAddressResolver.Resolve(ip) ?? string.Empty;
            }

            progress.Report(result);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>Returns true if the host responds at all (connect or connection refused).</summary>
    private static async Task<bool> IsHostAliveOnPortAsync(IPAddress ip, int port, CancellationToken cancellationToken)
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
            throw;
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

    /// <summary>Returns true only if the port is actually accepting connections.</summary>
    private static async Task<bool> IsPortOpenAsync(IPAddress ip, int port, CancellationToken cancellationToken)
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
            throw;
        }
        catch
        {
            return false;
        }
    }
}
