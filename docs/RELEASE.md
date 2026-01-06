# Release Guide

This guide explains how to create a new release of MCP Manager.

## Release Process

MCP Manager uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for automatic versioning based on git history and tags.

### Current Version

The current version is defined in `version.json`:
- **Base version**: 0.1
- **Pre-release**: Development builds have pre-release suffixes

### Creating a Release

#### Automatic Release (Recommended)

1. **Tag a commit** on the main branch:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```

2. **GitHub Actions will automatically**:
   - Build the solution
   - Run tests
   - Create deployment artifacts:
     - Windows executable (win-x64, self-contained)
     - Linux executable (linux-x64, self-contained)
     - Docker image (multi-arch: linux/amd64, linux/arm64)
   - Publish Docker image to GitHub Container Registry
   - Create a GitHub Release with all artifacts

#### Manual Release Trigger

You can also manually trigger a release via GitHub Actions:

1. Go to **Actions** → **Release** workflow
2. Click **Run workflow**
3. Enter the version (e.g., `v0.1.0`)
4. Click **Run workflow**

### Version Numbering

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality (backward compatible)
- **PATCH** version: Bug fixes (backward compatible)

Examples:
- `v0.1.0` - Initial release
- `v0.2.0` - New features added
- `v0.2.1` - Bug fix release
- `v1.0.0` - First stable release

### Release Artifacts

Each release includes:

#### 1. Windows Executable
- **File**: `mcpmanager-win-x64.zip`
- **Platform**: Windows x64
- **Type**: Self-contained (no .NET runtime required)
- **Single file**: Yes
- **Size**: ~100MB

#### 2. Linux Executable
- **File**: `mcpmanager-linux-x64.tar.gz`
- **Platform**: Linux x64
- **Type**: Self-contained (no .NET runtime required)
- **Single file**: Yes
- **Size**: ~100MB

#### 3. Docker Image
- **Registry**: `ghcr.io/jerrettdavis/mcpmanager`
- **Tags**: 
  - `latest` (most recent release)
  - `vX.Y.Z` (specific version)
  - `vX.Y` (minor version)
  - `vX` (major version)
- **Platforms**: linux/amd64, linux/arm64
- **Size**: ~250MB

### Release Checklist

Before creating a release:

- [ ] All tests pass (`dotnet test`)
- [ ] Code builds successfully (`dotnet build`)
- [ ] Security scans pass (CodeQL)
- [ ] Documentation is up to date
- [ ] CHANGELOG.md is updated (if exists)
- [ ] Version number follows semantic versioning
- [ ] Breaking changes are documented

### Post-Release

After a release is published:

1. **Verify artifacts**:
   - Download and test Windows executable
   - Download and test Linux executable
   - Pull and run Docker image

2. **Update documentation**:
   - Update README badges if needed
   - Update installation instructions if needed

3. **Announce the release**:
   - Post in discussions
   - Update project website (if applicable)

## Version Configuration

To change the version number:

1. Edit `version.json`:
   ```json
   {
     "version": "0.2",
     "publicReleaseRefSpec": [
       "^refs/heads/main$",
       "^refs/heads/v\\d+(?:\\.\\d+)?$"
     ]
   }
   ```

2. Commit and push:
   ```bash
   git add version.json
   git commit -m "chore: bump version to 0.2"
   git push
   ```

3. Create a tag:
   ```bash
   git tag v0.2.0
   git push origin v0.2.0
   ```

## Troubleshooting

### Build fails in GitHub Actions

- Check the Actions logs for specific errors
- Ensure all dependencies are compatible with .NET 10.0
- Verify Dockerfile syntax if Docker build fails

### Docker image not published

- Check if GitHub Container Registry permissions are set
- Verify the `GITHUB_TOKEN` has package write permissions
- Ensure the workflow has `packages: write` permission

### Artifacts missing from release

- Check if the build-artifacts job completed successfully
- Verify artifact upload/download steps in the workflow
- Check artifact retention settings

## CI/CD Workflows

The following workflows are involved in releases:

- **CI** (`ci.yml`): Runs on every push and PR
- **CodeQL** (`codeql.yml`): Security scanning
- **Release** (`release.yml`): Creates releases and artifacts

For more information, see the [`.github/workflows`](../.github/workflows) directory.
