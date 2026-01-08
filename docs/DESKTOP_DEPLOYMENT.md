# Single-File Desktop Executable

## Overview

The MCP Manager Desktop application is distributed as a **single-file, self-contained executable** that includes:
- Complete .NET 10.0 runtime
- All application dependencies
- Blazor UI components
- SQLite database engine
- Native libraries

**No installation required!** Just download and run.

## Download

Get the latest release from: https://github.com/JerrettDavis/McpManager/releases

### Available Downloads

- **Windows**: `mcpmanager-desktop-win-x64.zip` (~50-60 MB)
- **Linux**: `mcpmanager-desktop-linux-x64.tar.gz` (~50-60 MB)

## Installation

### Windows

1. **Download** `mcpmanager-desktop-win-x64.zip`
2. **Extract** the archive to a folder (e.g., `C:\Tools\McpManager`)
3. **Run** `McpManager.Desktop.exe`
4. The application opens in its own window - no browser needed!

**Optional - Add to PATH:**
```powershell
# Add to your user PATH
$env:PATH += ";C:\Tools\McpManager"
```

**Optional - Create Desktop Shortcut:**
```powershell
# PowerShell command to create shortcut
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$Home\Desktop\MCP Manager.lnk")
$Shortcut.TargetPath = "C:\Tools\McpManager\McpManager.Desktop.exe"
$Shortcut.Save()
```

### Linux

1. **Download** `mcpmanager-desktop-linux-x64.tar.gz`
2. **Extract** the archive:
   ```bash
   tar -xzf mcpmanager-desktop-linux-x64.tar.gz -C ~/Applications/McpManager
   ```
3. **Make executable** (if needed):
   ```bash
   chmod +x ~/Applications/McpManager/McpManager.Desktop
   ```
4. **Run** the application:
   ```bash
   ~/Applications/McpManager/McpManager.Desktop
   ```

**Optional - Add to PATH:**
```bash
echo 'export PATH="$HOME/Applications/McpManager:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

**Optional - Create Desktop Entry:**
```bash
cat > ~/.local/share/applications/mcpmanager.desktop <<EOF
[Desktop Entry]
Name=MCP Manager
Comment=Manage Model Context Protocol servers
Exec=$HOME/Applications/McpManager/McpManager.Desktop
Icon=application-default-icon
Terminal=false
Type=Application
Categories=Development;Utility;
EOF
```

## Features

### Self-Contained
- ✅ **No .NET installation required**
- ✅ **No dependencies to install**
- ✅ **Works offline** (after initial setup)
- ✅ **Portable** - can run from USB drive

### Single File
- ✅ **One executable file** contains everything
- ✅ **~50-60 MB total size** (compressed in release)
- ✅ **Easy distribution** - just share the executable
- ✅ **No installation wizard** needed

### Native Performance
- ✅ **Fast startup** - typically under 2 seconds
- ✅ **Low memory** - ~90 MB RAM usage
- ✅ **Optimized** for desktop use
- ✅ **Native window** - not a browser wrapper

## File Structure

After extraction, you'll have:
```
McpManager/
├── McpManager.Desktop.exe (or McpManager.Desktop on Linux)  # ← Single executable
├── wwwroot/
│   ├── index.html           # Static assets
│   ├── app.css
│   └── *.styles.css
├── appsettings.json         # Application configuration
└── *.pdb (optional)         # Debug symbols (can delete)
```

**Note**: The `wwwroot` folder must stay next to the executable. It contains static web assets needed by the Blazor UI.

## Configuration

### Database Location

By default, the application stores its database in:
- **Windows**: `%LOCALAPPDATA%\McpManager\mcpmanager.db`
- **Linux**: `~/.local/share/McpManager/mcpmanager.db`

### Settings

Edit `appsettings.json` to customize:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Command Line Options

```bash
# Reset database
McpManager.Desktop.exe --reset-db

# Specify custom database path (future enhancement)
# McpManager.Desktop.exe --db-path /path/to/database.db
```

## Troubleshooting

### Windows: "Windows protected your PC"

This is SmartScreen warning because the executable isn't code-signed. To run:
1. Click "More info"
2. Click "Run anyway"

**Why?** Code signing certificates cost $300+/year. For open source, we rely on GitHub's distribution.

### Linux: Permission Denied

Make the file executable:
```bash
chmod +x McpManager.Desktop
```

### Application Won't Start

**Check:**
1. Is `wwwroot` folder present next to executable?
2. Do you have write permissions for the app directory?
3. Is port 5000 already in use? (Desktop app uses random port)

**View logs:**
```bash
# Windows
$env:ASPNETCORE_ENVIRONMENT="Development"
.\McpManager.Desktop.exe

# Linux
ASPNETCORE_ENVIRONMENT=Development ./McpManager.Desktop
```

### Database Errors

Reset the database:
```bash
# Windows
.\McpManager.Desktop.exe --reset-db

# Linux
./McpManager.Desktop --reset-db
```

## Size Optimization

The executable is ~50-60 MB because it includes:
- .NET 10.0 runtime (~30 MB)
- Application code (~10 MB)
- Dependencies (~10 MB)
- Blazor framework (~5 MB)
- Compression (~5-10 MB savings)

**Could it be smaller?**
- Yes, with trimming - but risks runtime errors with Blazor
- Server version is much smaller (~5 MB) but requires .NET installed

## Comparison: Desktop vs Server

| Feature | Desktop (Single File) | Server (Framework-Dependent) |
|---------|----------------------|------------------------------|
| **Size** | ~50-60 MB | ~5 MB |
| **.NET Required** | ❌ No | ✅ Yes (.NET 10.0) |
| **Distribution** | Single file | Multiple files |
| **Startup** | Native window | Browser required |
| **Portability** | ✅ USB drive ready | ❌ Needs .NET |
| **Updates** | Download new version | `git pull && dotnet run` |

## Building From Source

To build your own single-file executable:

```bash
# Clone repository
git clone https://github.com/JerrettDavis/McpManager.git
cd McpManager

# Publish for Windows
dotnet publish src/McpManager.Desktop/McpManager.Desktop.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64 \
  /p:PublishSingleFile=true

# Publish for Linux
dotnet publish src/McpManager.Desktop/McpManager.Desktop.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./publish/linux-x64 \
  /p:PublishSingleFile=true
```

The executable will be in `publish/win-x64/` or `publish/linux-x64/`.

## Technical Details

### What's Inside?

The single-file executable uses .NET's **single-file deployment** which:
1. Bundles all DLLs into the executable
2. Extracts them to temp folder on first run
3. Loads assemblies from temp location
4. Includes native libraries (SQLite, Photino)

### Compression

- **Enabled**: `EnableCompressionInSingleFile=true`
- **Saves**: ~30-40% size
- **Trade-off**: Slightly slower first launch (~200ms)

### Trimming

- **Disabled**: `PublishTrimmed=false`
- **Why?** Blazor uses reflection - trimming can break it
- **Future**: IL Linker improvements may enable safe trimming

## Security

### Code Signing

The release binaries are **not code-signed**. This means:
- ⚠️ Windows SmartScreen warnings
- ⚠️ macOS Gatekeeper will block (not distributed for macOS yet)
- ✅ Source code is open and auditable
- ✅ Built by GitHub Actions (reproducible)
- ✅ SHA256 checksums in releases

### Verify Integrity

Always download from official releases and verify checksums:
```bash
# Windows (PowerShell)
Get-FileHash -Algorithm SHA256 McpManager.Desktop.exe

# Linux
sha256sum McpManager.Desktop
```

Compare with release notes SHA256.

## Support

- **Issues**: https://github.com/JerrettDavis/McpManager/issues
- **Discussions**: https://github.com/JerrettDavis/McpManager/discussions
- **Documentation**: https://github.com/JerrettDavis/McpManager/tree/main/docs

## License

MIT License - see [LICENSE](../../LICENSE)

---

**Download Latest Release**: https://github.com/JerrettDavis/McpManager/releases/latest
