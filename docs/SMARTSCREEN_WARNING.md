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

## ⚠️ This Is Normal - Here's Why

### The Technical Reason

Windows SmartScreen shows this warning because the executable is **not digitally signed** with a trusted code signing certificate.

**Why isn't it signed?**
- Code signing certificates cost **$300-500 per year**
- MCP Manager is an **open source project** with no funding
- We provide the source code for full transparency instead

### Is It Safe?

**YES!** Here's how you can verify:

#### 1. Source Code is Public
- **View the code**: https://github.com/JerrettDavis/McpManager
- **Audit yourself** or have security experts review it
- **See every commit**: Full git history available
- **No secrets**: Everything is transparent

#### 2. Built by GitHub Actions
- **Reproducible builds**: Anyone can rebuild from source
- **No manual tampering**: Automated build process
- **Build logs public**: See exactly how it was built
- **Workflow auditable**: Review `.github/workflows/release.yml`

#### 3. Verify Download Integrity
Every release includes SHA256 checksums. Verify your download:

**PowerShell (Windows):**
```powershell
Get-FileHash -Path McpManager.Desktop.exe -Algorithm SHA256
```

**Compare with release notes** - the hash should match exactly.

#### 4. Community Trust
- **GitHub stars**: See how many users trust the project
- **Open issues**: Active community feedback
- **Commit history**: Regular updates and fixes
- **Contributors**: Multiple people reviewing code

## How to Run the App

### Step 1: Click "More info"

```
┌─────────────────────────────────────────┐
│ Windows protected your PC              │
│                                         │
│ Microsoft Defender SmartScreen          │
│ prevented an unrecognized app from      │
│ starting. Running this app might put    │
│ your PC at risk.                        │
│                                         │
│ App: McpManager.Desktop.exe            │
│ Publisher: Unknown publisher            │
│                                         │
│ [Run anyway]  [Don't run]              │ ← Click here first
└─────────────────────────────────────────┘
```

### Step 2: Click "Run anyway"

The app will start normally. You'll only see this warning **the first time** you run each version.

## Why This Happens to Open Source Software

### The Code Signing Dilemma

**Paid Certificates:**
- **Cost**: $300-500/year minimum
- **Requirements**: Business entity, validation process
- **Timeline**: 2-4 weeks to get certificate
- **Hardware**: Requires USB token for security

**Free Alternatives:**
- **SignPath.io**: Free for open source, but requires approval
- **Self-signed**: Doesn't help - still shows warnings
- **Wait for reputation**: Takes months of downloads

### We're Working On It!

**Current status**: Applied for **SignPath.io** free code signing for open source projects

**Timeline:**
- ⏳ Application submitted
- ⏳ Waiting for approval (1-2 weeks)
- ⏳ Integration with CI/CD
- ✅ Future releases will be signed

Until then, we rely on:
1. **Source code transparency** (audit the code yourself)
2. **SHA256 verification** (ensure integrity)
3. **Community trust** (GitHub stars, issues, reviews)
4. **Reproducible builds** (rebuild from source)

## Alternatives to Avoid the Warning

### Option 1: Run from Source

Build and run directly from source code:

```bash
# Clone repository
git clone https://github.com/JerrettDavis/McpManager.git
cd McpManager

# Restore and run
dotnet restore
dotnet run --project src/McpManager.Desktop
```

**No warnings** because .NET SDK executables are already trusted.

### Option 2: Use the Server Version

Run the web server version instead:

```bash
# No executable warnings - runs in browser
dotnet run --project src/McpManager.Web

# Open browser to http://localhost:5000
```

**Trade-off**: Requires .NET SDK installed.

### Option 3: Wait for Signed Releases

Once SignPath.io approves our application:
- ✅ All releases will be digitally signed
- ✅ No more SmartScreen warnings
- ✅ Same $0 cost (free for open source)

**ETA**: 2-4 weeks from now

## For the Paranoid: Full Verification

### 1. Verify the Download Source

Only download from official sources:
- ✅ **GitHub Releases**: https://github.com/JerrettDavis/McpManager/releases
- ❌ **Anywhere else**: Don't trust it

### 2. Check File Hash

```powershell
# Windows PowerShell
$hash = Get-FileHash -Path McpManager.Desktop.exe -Algorithm SHA256
Write-Host "SHA256: $($hash.Hash)"

# Compare with release notes
```

### 3. Scan with Antivirus

```powershell
# Windows Defender
Start-MpScan -ScanPath "C:\path\to\McpManager.Desktop.exe"
```

Or upload to:
- **VirusTotal**: https://www.virustotal.com (may flag as unsigned)
- **Hybrid Analysis**: https://www.hybrid-analysis.com

**Note**: Some antivirus may flag any unsigned executable as "potentially unwanted" - this is normal.

### 4. Review the Source Code

**Key files to audit:**
- `src/McpManager.Desktop/Program.cs` - Entry point
- `src/McpManager.*/` - All application code
- `.github/workflows/release.yml` - Build process

**Look for:**
- ❌ Network requests to unknown servers
- ❌ File system access outside app directory
- ❌ Registry modifications
- ❌ Process injection
- ✅ Only local SQLite database operations
- ✅ Only user-initiated actions

### 5. Run in Sandbox (Advanced)

Use Windows Sandbox to test:

```powershell
# Enable Windows Sandbox (one-time)
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM

# Run Sandbox and test app
# Any malware would be contained
```

## What We Do NOT Do

To build trust, here's what MCP Manager **does NOT** do:

❌ **No telemetry** - We don't track your usage
❌ **No auto-updates** - You control updates
❌ **No network calls** (except registry queries you initiate)
❌ **No data collection** - Everything stays local
❌ **No analytics** - No third-party services
❌ **No ads or monetization** - 100% free and open source

✅ **Data stays local** - SQLite database in your user folder
✅ **No external dependencies** - Self-contained executable
✅ **No background services** - Runs only when you run it
✅ **No system modifications** - Portable application

## Comparison: Open Source vs Commercial

| Aspect | MCP Manager (OSS) | Commercial Software |
|--------|-------------------|---------------------|
| **Source Code** | ✅ Public & Auditable | ❌ Closed & Hidden |
| **Build Process** | ✅ Open & Reproducible | ❌ Private |
| **Code Signing** | ⏳ Applied (free) | ✅ Yes ($500/year) |
| **SmartScreen** | ⚠️ Warning (for now) | ✅ Trusted |
| **Cost** | ✅ Free Forever | 💰 $50-500/year |
| **Privacy** | ✅ No tracking | ⚠️ Often tracks |
| **Trust Model** | Transparency | Certificate Authority |

## FAQ

### Q: Why not just buy a code signing certificate?

**A**: It costs $300-500/year, and we're an open source project with no funding or revenue. We provide source code transparency instead.

### Q: When will releases be signed?

**A**: We've applied for SignPath.io free code signing. Expected timeline: 2-4 weeks. Follow progress: https://github.com/JerrettDavis/McpManager/issues/[TBD]

### Q: Is this warning dangerous?

**A**: No - it's just Windows being cautious about unsigned software. Verify the SHA256 hash and you're safe.

### Q: Can I disable SmartScreen?

**A**: You can, but **we don't recommend it**. SmartScreen protects you from actual malware. Just click "Run anyway" for trusted apps like this one.

### Q: What about macOS Gatekeeper?

**A**: We don't currently distribute for macOS, but the same principle applies - Apple Developer Account costs $99/year. If there's demand, we'll explore it.

### Q: Will warnings go away over time?

**A**: Yes! SmartScreen builds reputation based on downloads. After hundreds of users run the app safely, warnings reduce automatically. But we're aiming for proper code signing first.

### Q: Can I help fund a certificate?

**A**: Not needed! We're using SignPath.io which provides free code signing for open source. If you want to support the project, contribute code or spread the word instead.

### Q: I'm still concerned. What should I do?

**Best approach**:
1. Build from source yourself
2. Run the web version instead
3. Wait for signed releases
4. Audit the source code
5. Run in Windows Sandbox

**Or** just skip it - there are other MCP management tools available.

## Report Security Issues

Found a security vulnerability? **Do NOT open a public issue.**

**Instead**:
1. Email: [Create private security advisory on GitHub]
2. GitHub Security: https://github.com/JerrettDavis/McpManager/security/advisories
3. Responsible disclosure: We'll credit you and patch quickly

## Updates

**Last updated**: 2026-01-08

**Status**: 
- ⏳ SignPath.io application submitted
- ⏳ Waiting for approval
- ✅ Source code fully auditable
- ✅ SHA256 hashes in all releases

**Track progress**: [Link to GitHub issue when created]

---

**Bottom line**: The warning is just because we don't pay $500/year for a certificate. The app is safe, open source, and auditable. We're working on free code signing through SignPath.io.

**Download**: https://github.com/JerrettDavis/McpManager/releases
