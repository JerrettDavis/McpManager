# Version Display Implementation

## Overview
The application displays its version number in the navigation menu using a `VersionService` that reads version information from Nerdbank.GitVersioning attributes.

## How It Works

### Version Service
Located in `src/McpManager.Application/Services/VersionService.cs`, this service:

1. **Reads from Entry Assembly** - Uses `Assembly.GetEntryAssembly()` to read version from the running application (Web or Desktop), not the Application layer
2. **Extracts Version** - Parses `AssemblyInformationalVersionAttribute` which contains the Nerdbank.GitVersioning version
3. **Returns Simple Version** - Strips commit hash and pre-release info (e.g., "0.1.23" from "0.1.23+c9eddb756e")

### Integration
- **Registration**: Service is registered as singleton in `ServiceCollectionExtensions.cs`
- **Usage**: NavMenu injects `IVersionService` and calls `GetVersion()` to display the version

### Why Entry Assembly?
The Application layer doesn't have Nerdbank.GitVersioning configured. Only Web and Desktop projects have it. By using `GetEntryAssembly()`, we read the version from whichever application is currently running.

## Version Format

### Full Information Available
- **GetVersion()**: Simple version (e.g., "0.1.23")
- **GetInformationalVersion()**: Full version with commit (e.g., "0.1.23+c9eddb756e")
- **GetAssemblyVersion()**: Assembly version (e.g., "0.1.0")

### Current Display
The NavMenu displays only the simple version: "Version 0.1.23"

## Testing
To verify the version displayed:

```powershell
# Check what nbgv reports
nbgv get-version

# Build and run Web app
dotnet run --project src\McpManager.Web\McpManager.Web.csproj

# Build and run Desktop app
dotnet run --project src\McpManager.Desktop\McpManager.Desktop.csproj
```

The version number in the bottom-left of the navigation menu should match the version from `nbgv get-version`.

## Updating Version
Version is controlled by `version.json` and calculated automatically by Nerdbank.GitVersioning:
- Base version in `version.json`: "0.1"
- Height (commits since base): Automatically calculated
- Format: `{Major}.{Minor}.{Height}`

See [VERSIONING.md](VERSIONING.md) for details on how version bumps are automated through PRs.
