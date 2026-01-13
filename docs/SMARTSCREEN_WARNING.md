# Windows SmartScreen Warning Guide

## The Warning Message

When you download and run `McpManager.Desktop.exe`, Windows may display:

```
┌─────────────────────────────────────────┐
│ Windows protected your PC              │
│                                         │
│ Microsoft Defender SmartScreen          │
│ prevented an unrecognized app from      │
│ starting. Running this app might put    │
│ your PC at risk.                        │
│                                         │
│ [More info]  [Don't run]               │
└─────────────────────────────────────────┘
```

## Why This Happens

Windows SmartScreen shows this warning because the executable isn't digitally signed with a code signing certificate.

Code signing certificates cost $300-500 per year. MCP Manager is an open source project with no funding, so we provide source code transparency instead.

### Verifying Safety

**Source Code**
- View at https://github.com/JerrettDavis/McpManager
- Audit the code yourself
- Full git history available

**Build Process**
- Built by GitHub Actions (reproducible builds)
- No manual tampering
- Review `.github/workflows/release.yml`

**Download Integrity**
Verify SHA256 hash:
```powershell
Get-FileHash -Path McpManager.Desktop.exe -Algorithm SHA256
```
Compare with release notes.

## Running the App

1. Click "More info" when the warning appears
2. Click "Run anyway"

The warning only appears the first time you run each version.

## Code Signing for Open Source

Code signing certificates require:
- $300-500/year cost
- Business entity and validation process
- 2-4 weeks to obtain
- USB token for security

Free alternatives:
- SignPath.io (free for open source, requires approval)
- Self-signed certificates (still trigger warnings)
- Building reputation (takes months of downloads)

Until we get code signing, we rely on source code transparency, SHA256 verification, and reproducible builds.

## Alternatives

**Run from Source**
```bash
git clone https://github.com/JerrettDavis/McpManager.git
cd McpManager
dotnet restore
dotnet run --project src/McpManager.Desktop
```
No warnings because .NET SDK executables are trusted.

**Use Server Version**
```bash
dotnet run --project src/McpManager.Web
# Open browser to http://localhost:5000
```
Requires .NET SDK installed.

## Full Verification

**Download Source**
Only download from GitHub Releases: https://github.com/JerrettDavis/McpManager/releases

**File Hash**
```powershell
$hash = Get-FileHash -Path McpManager.Desktop.exe -Algorithm SHA256
Write-Host "SHA256: $($hash.Hash)"
```
Compare with release notes.

**Antivirus Scan**
```powershell
Start-MpScan -ScanPath "C:\path\to\McpManager.Desktop.exe"
```
Or upload to VirusTotal. Note: Some antivirus flag unsigned executables as "potentially unwanted".

**Source Code Audit**
Key files:
- `src/McpManager.Desktop/Program.cs` - Entry point
- `.github/workflows/release.yml` - Build process

The app only accesses local SQLite database and performs user-initiated actions.

**Windows Sandbox**
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM
```
Run the app in Windows Sandbox for isolated testing.

## Privacy and Data

MCP Manager does NOT:
- Track usage or collect telemetry
- Automatically update (you control updates)
- Make network calls (except registry queries you initiate)
- Use analytics or third-party services
- Display ads or monetize

Data storage:
- Local SQLite database in your user folder
- Self-contained executable
- No background services
- No system modifications

## Open Source vs Commercial

| Aspect | MCP Manager | Commercial Software |
|--------|-------------|---------------------|
| Source Code | Public & auditable | Closed |
| Build Process | Open & reproducible | Private |
| Code Signing | Pending approval | Yes ($500/year) |
| SmartScreen | Warning | Trusted |
| Cost | Free | $50-500/year |
| Privacy | No tracking | Often tracks |
| Trust Model | Transparency | Certificate |

## FAQ

**Why not buy a code signing certificate?**
Cost is $300-500/year. As an open source project with no funding, we provide source code transparency instead.

**When will releases be signed?**
Applied for SignPath.io free code signing. Timeline varies based on approval.

**Is this warning dangerous?**
No. It's Windows being cautious about unsigned software. Verify the SHA256 hash for safety.

**Can I disable SmartScreen?**
Not recommended. SmartScreen protects against actual malware. Click "Run anyway" for trusted apps.

**What about macOS?**
Not currently distributed for macOS. Apple Developer Account costs $99/year.

**Will warnings go away?**
SmartScreen builds reputation over time. After many downloads, warnings may reduce automatically.

**Still concerned?**
Build from source, run the web version, audit the code, or use Windows Sandbox.

## Security Issues

Found a vulnerability? Use GitHub Security Advisories instead of opening a public issue:
https://github.com/JerrettDavis/McpManager/security/advisories

## Summary

The SmartScreen warning appears because the app isn't code-signed ($500/year cost). The app is safe, open source, and auditable. Applied for free code signing through SignPath.io.

Download: https://github.com/JerrettDavis/McpManager/releases
