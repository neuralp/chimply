<p align="center">
  <img src="Chimply/Resource/logo.svg" width="150" height="150" alt="Chimply logo">
</p>

<h1 align="center">Chimply</h1>

<p align="center">A cross-platform network scanner built with .NET 8 and Avalonia UI.</p>

---

![Chimply screenshot](Static/program-screenshot.png)

## Features

- **ICMP ping** with TCP port probe fallback for host discovery
- **Port scanning** (21, 22, 80, 443) with clickable links that open in the default handler
- **MAC address resolution** (Windows via SendARP, Linux via /proc/net/arp)
- **Manufacturer lookup** from IEEE OUI database (~57K entries)
- **Auto-detect subnet** from the primary network adapter
- **CIDR and IP range input** — supports `192.168.1.0/24`, `192.168.1.1-50`, or `192.168.1.1-192.168.1.50`
- **Persistent results** across scans with New/Up/Down/Upd IP status tracking
- **IP change detection** — detects when a host changes IP address via MAC tracking
- **Color-coded "Last Change"** timestamps with live updates
- **Right-click context menu** to copy IP address, hostname, or MAC address to clipboard
- **Export CSV** for scan results
- **Numeric IP sorting** in the DataGrid
- Dark Fluent theme

## Building

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build Chimply.sln
dotnet run --project Chimply/Chimply.csproj
```

## Publishing

Single-file self-contained binaries can be built using the included publish profiles:

```bash
# Linux
dotnet publish Chimply/Chimply.csproj -p:PublishProfile=linux-x64

# Windows
dotnet publish Chimply/Chimply.csproj -p:PublishProfile=win-x64
```

Output will be in `Chimply/bin/Release/net8.0/<rid>/publish/`.

## Updating the OUI Database

Replace `Chimply/Resource/mac.csv` with a newer version from [maclookup.app](https://maclookup.app/downloads/csv-database) and rebuild.

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
