# Code Signing for Windows SmartScreen

## The SmartScreen Problem

Windows SmartScreen shows warnings for unsigned executables:
```
"Windows protected your PC"
"Microsoft Defender SmartScreen prevented an unrecognized app from starting"
```

This happens because the executable isn't digitally signed with a trusted certificate.

## Solutions for Open Source Projects

### Option 1: Purchase Code Signing Certificate (Traditional)

**Cost**: $300-500/year

**Providers:**
- DigiCert (~$474/year)
- Sectigo (~$299/year)
- GlobalSign (~$369/year)
- SSL.com (~$249/year)

**Requirements:**
- Business entity verification (LLC, Corporation, etc.)
- EV (Extended Validation) certificate for best SmartScreen reputation
- Hardware token (USB key) for private key storage
- Annual renewal

**Pros:**
- ✅ Immediate SmartScreen trust (with EV)
- ✅ Shows your organization name
- ✅ Valid for all releases

**Cons:**
- ❌ Expensive ($300-500/year)
- ❌ Requires business entity
- ❌ Complex validation process (2-4 weeks)
- ❌ Hardware token management

**Process:**
1. Register business entity (if not already)
2. Purchase certificate from provider
3. Complete identity verification (documents, phone calls)
4. Receive hardware token with certificate
5. Sign executables with `signtool.exe`
6. Timestamping ensures signature validity after cert expires

**Commands:**
```bash
# Sign with certificate
signtool sign /f MyCert.pfx /p password /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 McpManager.Desktop.exe

# Verify signature
signtool verify /pa /v McpManager.Desktop.exe
```

---

### Option 2: Free Code Signing for Open Source (SignPath.io)

**Cost**: FREE for open source

**Provider**: https://about.signpath.io/product/open-source

**Requirements:**
- Public GitHub repository
- OSI-approved open source license (MIT ✅)
- Apply and get approved
- Build via GitHub Actions
- Submit releases for signing

**Pros:**
- ✅ Completely free
- ✅ No business entity needed
- ✅ Automated via CI/CD
- ✅ Legitimate code signing certificate
- ✅ Used by many popular open source projects

**Cons:**
- ⚠️ Application/approval process (1-2 weeks)
- ⚠️ SmartScreen reputation takes time to build (same as paid)
- ⚠️ Must build via their system or submit artifacts
- ⚠️ Certificate is in their name, not yours

**How It Works:**
1. Apply at https://about.signpath.io/product/open-source
2. Provide GitHub repo, project description
3. Get approved and receive signing project
4. Integrate with GitHub Actions
5. SignPath automatically signs your releases

**Example GitHub Action:**
```yaml
- name: Submit for code signing
  uses: signpath/github-action-submit-signing-request@v1
  with:
    api-token: ${{ secrets.SIGNPATH_API_TOKEN }}
    organization-id: ${{ secrets.SIGNPATH_ORGANIZATION_ID }}
    project-slug: 'McpManager'
    signing-policy-slug: 'release-signing'
    artifact-configuration-slug: 'windows-exe'
    input-artifact-path: 'McpManager.Desktop.exe'
    output-artifact-path: 'McpManager.Desktop.signed.exe'
```

**Open Source Projects Using SignPath:**
- PuTTY
- WinSCP
- OpenVPN GUI
- Notepad++
- Many others

---

### Option 3: Self-Signed Certificate (Not Recommended)

**Cost**: Free

**Process:**
```powershell
# Create self-signed cert
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=McpManager" -CertStoreLocation Cert:\CurrentUser\My

# Export certificate
Export-Certificate -Cert $cert -FilePath McpManager.cer

# Sign executable
Set-AuthenticodeSignature -FilePath McpManager.Desktop.exe -Certificate $cert
```

**Pros:**
- ✅ Free
- ✅ Quick

**Cons:**
- ❌ Doesn't help with SmartScreen (not trusted)
- ❌ Users must manually trust certificate
- ❌ Still shows security warnings
- ❌ Not suitable for distribution

**Verdict**: ❌ Don't use for public distribution

---

### Option 4: Wait for SmartScreen Reputation (Current Approach)

**Cost**: Free

**How It Works:**
- SmartScreen builds reputation based on downloads
- After enough users run your app, warnings reduce
- Microsoft's telemetry sees app is safe
- Eventually warnings disappear

**Timeline:**
- First 100 downloads: Full warnings
- 100-1,000 downloads: Warnings start reducing
- 1,000+ downloads: Warnings mostly gone
- 10,000+ downloads: Trusted by SmartScreen

**Pros:**
- ✅ Free
- ✅ No setup required
- ✅ Eventually works

**Cons:**
- ⏱️ Takes months/years
- ⚠️ Poor user experience initially
- ⚠️ Every new release restarts the process
- ⚠️ No control over timeline

**Verdict**: ⚠️ Viable for patient open source projects

---

## Recommended Approach for McpManager

### Immediate: Document the Warning

**Create user guide explaining:**
1. Warning is normal for open source software
2. How to safely bypass SmartScreen
3. Verify download integrity with SHA256 hash
4. Source code is available for audit

**Update release notes:**
```markdown
## ⚠️ Windows SmartScreen Warning

When you run McpManager.Desktop.exe, Windows may show:
"Windows protected your PC"

**This is normal for open source software.** Here's why:
- Code signing certificates cost $300-500/year
- We're an open source project with no funding
- The code is 100% open and auditable on GitHub

**To run the app:**
1. Click "More info"
2. Click "Run anyway"

**Verify the download (optional):**
```bash
# Compare with SHA256 in release notes
Get-FileHash McpManager.Desktop.exe -Algorithm SHA256
```

**Source code**: https://github.com/JerrettDavis/McpManager
```

### Short Term: Apply for SignPath.io (Recommended)

**Action Items:**
1. Apply at https://about.signpath.io/product/open-source
2. Fill out application (project description, goals)
3. Wait for approval (1-2 weeks)
4. Integrate with GitHub Actions
5. Sign all future releases

**Expected Timeline:**
- Week 1: Submit application
- Week 2-3: Review and approval
- Week 3: Integration setup
- Week 4+: All releases signed

**Benefits:**
- ✅ Free forever
- ✅ Legitimate code signing
- ✅ Automated via CI/CD
- ✅ Better user experience
- ✅ Professional appearance

### Long Term: Consider Paid Certificate (If Funded)

**When to consider:**
- Project gets sponsorship/funding
- Want to show your organization name
- Need maximum trust immediately
- Have budget for $300-500/year

**Providers to consider:**
- **DigiCert**: Industry standard, excellent support
- **SSL.com**: Cheaper option, good for open source
- **Sectigo**: Mid-range price, good reputation

---

## Implementation Plan

### Phase 1: Document (Now)

**Create files:**
- `docs/SMARTSCREEN_WARNING.md` - Explain the warning
- Update `README.md` - Add security section
- Update release notes template - Include warning info

**Add to README:**
```markdown
## 🔒 Security Note

Windows SmartScreen may show a warning when running the desktop app. This is because:
- We're an open source project without paid code signing (~$500/year)
- The executable is safe - all source code is auditable
- We're working on free code signing via SignPath.io

See [SmartScreen Warning Guide](docs/SMARTSCREEN_WARNING.md) for details.
```

### Phase 2: Apply for SignPath.io (This Week)

**Application checklist:**
- [x] Public GitHub repo with MIT license
- [ ] Project description prepared
- [ ] Maintainer information ready
- [ ] GitHub Actions workflow ready
- [ ] Apply at SignPath.io

**Application info to prepare:**
```
Project Name: McpManager
Repository: https://github.com/JerrettDavis/McpManager
License: MIT
Description: A Blazor-based dashboard for managing Model Context Protocol (MCP) 
servers across multiple AI agents. Provides a unified interface for discovering,
installing, and configuring MCP servers for Claude Desktop, GitHub Copilot, and
other AI tools.

Maintainer: JerrettDavis
Contact: [GitHub profile]

Build System: GitHub Actions
Target: Windows desktop executable (McpManager.Desktop.exe)
Release Frequency: As needed (semantic versioning)

Why we need signing: Open source tool distributed to end users who encounter
SmartScreen warnings. Want to provide better user experience.
```

### Phase 3: Integrate Signing (After Approval)

**Update `.github/workflows/release.yml`:**
```yaml
- name: Sign Windows executable
  if: matrix.os == 'windows-latest' && contains(matrix.artifact_name, 'desktop')
  uses: signpath/github-action-submit-signing-request@v1
  with:
    api-token: ${{ secrets.SIGNPATH_API_TOKEN }}
    organization-id: ${{ secrets.SIGNPATH_ORGANIZATION_ID }}
    project-slug: 'McpManager'
    signing-policy-slug: 'release-signing'
    artifact-configuration-slug: 'desktop-exe'
    input-artifact-path: ./publish/win-x64/McpManager.Desktop.exe
    output-artifact-path: ./publish/win-x64/McpManager.Desktop.exe
    wait-for-completion: true
```

### Phase 4: Build Reputation

**Even with signing:**
- SmartScreen reputation still needs building
- First release with new cert may still warn
- After 100-1000 downloads, trust improves
- Monitor feedback from users

---

## Alternative: GitHub Actions Attestations (Future)

**New feature (2024):**
GitHub now supports build attestations that prove:
- Artifact was built by specific GitHub Actions workflow
- Source code commit that produced it
- Build environment details

**Not a replacement for code signing**, but helps with:
- Verifying authenticity
- Supply chain security
- Reproducible builds

**To explore:**
```yaml
- name: Attest Build Provenance
  uses: actions/attest-build-provenance@v1
  with:
    subject-path: 'McpManager.Desktop.exe'
```

---

## Cost-Benefit Analysis

| Option | Cost | Setup Time | User Impact | Recommendation |
|--------|------|------------|-------------|----------------|
| **Do Nothing** | $0 | 0 | ⚠️ Poor (warnings) | ❌ Current state |
| **Document Warning** | $0 | 1 hour | ⚠️ Slightly better | ✅ Do immediately |
| **SignPath.io** | $0 | 2-3 weeks | ✅ Good | ✅ **Recommended** |
| **Paid Certificate** | $300-500/year | 2-4 weeks | ✅ Best | ⚠️ If funded |
| **Self-Signed** | $0 | 30 mins | ❌ No improvement | ❌ Don't use |

---

## Next Steps

### This Week:
1. ✅ Create `SMARTSCREEN_WARNING.md` documentation
2. ✅ Update README with security section
3. ✅ Update release notes template
4. ⏳ Apply for SignPath.io free code signing

### After SignPath Approval:
1. Integrate signing into GitHub Actions
2. Sign all new releases
3. Update documentation (signed releases)
4. Monitor SmartScreen reputation

### Optional (If Funded):
1. Research paid certificate providers
2. Evaluate cost vs benefit
3. Register business entity if needed
4. Purchase and implement certificate

---

## Resources

- **SignPath Open Source**: https://about.signpath.io/product/open-source
- **Microsoft SignTool**: https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe
- **Code Signing Guide**: https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools
- **SmartScreen Overview**: https://docs.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-smartscreen/

---

**Recommendation**: Start with documenting the warning, then apply for SignPath.io free signing. This provides the best balance of cost ($0) and user experience for an open source project.
