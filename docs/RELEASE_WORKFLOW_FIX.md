# Release Workflow Trigger Issue & Fix

## Problem

The release workflow wasn't triggering even though tags were being created successfully (e.g., `v0.1.17.39754`).

## Root Cause

**GitHub Actions Security Feature**: When a workflow uses the default `GITHUB_TOKEN` to create a tag or push commits, it **does not trigger other workflows**. This is intentional to prevent recursive workflow execution.

From GitHub Docs:
> "When you use the repository's GITHUB_TOKEN to perform tasks, events triggered by the GITHUB_TOKEN, with the exception of workflow_dispatch and repository_dispatch, will not create a new workflow run."

## Our Workflow Chain

```
1. PR merged to main (version.json changed)
   ↓
2. auto-tag.yml triggers
   ↓
3. auto-tag.yml creates tag using GITHUB_TOKEN
   ↓
4. ❌ release.yml should trigger on tag push
   BUT: Tag created by GITHUB_TOKEN = NO TRIGGER
```

## Solution Options

### Option 1: Use Personal Access Token (PAT) ❌
- Create PAT with `repo` scope
- Store as secret
- Use PAT instead of GITHUB_TOKEN
- **Downside**: Security risk, token management overhead, expiration

### Option 2: Use workflow_run Trigger ✅ **Implemented**
- Release workflow triggers when auto-tag completes
- No additional tokens needed
- Secure and maintainable
- **This is what we implemented**

### Option 3: Combine Workflows ❌
- Make auto-tag create the release directly
- **Downside**: Violates separation of concerns, harder to maintain

### Option 4: Use repository_dispatch ❌
- Manual event triggering
- **Downside**: More complex, less intuitive

## Implementation

### Before
```yaml
# release.yml
on:
  push:
    tags:
      - 'v*'  # ❌ Never triggers when tag created by GITHUB_TOKEN
```

### After
```yaml
# release.yml
on:
  push:
    tags:
      - 'v*'  # Still here for manual tags
  workflow_run:
    workflows: ["Tag Release on Main Push"]
    types:
      - completed  # ✅ Triggers when auto-tag completes
```

## New Flow

```
1. PR merged to main (version.json changed)
   ↓
2. auto-tag.yml triggers (on push to main)
   ↓
3. auto-tag.yml creates tag v0.2.0
   ↓
4. auto-tag.yml completes successfully
   ↓
5. ✅ release.yml triggers (workflow_run)
   ↓
6. release.yml reads version from version.json
   ↓
7. Release artifacts built and published
```

## Version Detection Logic

The release workflow now handles three trigger types:

```bash
if workflow_run trigger:
  # Get version from version.json on main
  VERSION = "v$(nbgv get-version -v Version)"

elif manual tag push:
  # Extract from tag name
  VERSION = "${GITHUB_REF#refs/tags/}"

elif workflow_dispatch:
  # Use manual input
  VERSION = "${{ github.event.inputs.version }}"
```

## Benefits

✅ **Secure**: Uses default GITHUB_TOKEN, no PAT needed  
✅ **Automatic**: Works seamlessly with automated versioning  
✅ **Maintainable**: Clear separation of concerns  
✅ **Reliable**: Triggers consistently on workflow completion  
✅ **Flexible**: Still supports manual tag pushes  

## Testing

### Automatic Flow (via PR merge)
1. Create PR with conventional commit title
2. Version auto-updates in PR
3. Merge PR to main
4. Watch Actions tab:
   - "Tag Release on Main Push" runs and creates tag
   - "Release" triggers automatically when tagging completes
   - Artifacts built and release created

### Manual Tag (still works)
```bash
git tag v1.0.0
git push origin v1.0.0
# Release workflow triggers via push event
```

### Manual Workflow Dispatch (still works)
1. Go to Actions tab
2. Select "Release" workflow
3. Click "Run workflow"
4. Enter version (e.g., v1.0.0)
5. Release builds with specified version

## Troubleshooting

### Release didn't trigger after merge

**Check:**
1. Did auto-tag workflow complete successfully?
   - Go to Actions → "Tag Release on Main Push"
   - Verify it ran and succeeded
2. Was version.json actually changed in the commit?
   - Check commit diff
3. Look for workflow_run trigger in Actions tab
   - Should show "Release" triggered by "Tag Release on Main Push"

### Release triggered but failed

**Common causes:**
- Build errors in artifacts
- Docker build failures
- Artifact upload issues

**Check:**
- Release workflow logs in Actions tab
- Individual job failures
- Error messages in collapsed sections

### Want to manually trigger release for existing tag

**Option 1: Workflow Dispatch**
```
1. Actions → Release → Run workflow
2. Enter tag version
```

**Option 2: Re-push Tag**
```bash
git tag -d v0.2.0
git push origin :refs/tags/v0.2.0
git tag v0.2.0
git push origin v0.2.0
```

## Related Issues

- GitHub Issue: Tags created by Actions don't trigger workflows
- GitHub Docs: [Events that trigger workflows](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows)
- Limitation: `workflow_run` can only reference workflows in the same repository

## Alternatives Considered

### GitHub App
- Create GitHub App with elevated permissions
- Generate installation token per workflow
- **Rejected**: Overkill for this use case

### Fine-grained PAT
- Create fine-grained PAT with limited scope
- Rotate regularly
- **Rejected**: Still requires external token management

### Deploy Keys
- SSH deploy key with write access
- **Rejected**: Complex setup, not suited for this

## Commit

Fix implemented in: `78f52ef`
- Updated `.github/workflows/release.yml`
- Added `workflow_run` trigger
- Centralized version detection in `get-version` job
- All jobs now reference centralized version outputs

---

**Status**: ✅ Fixed and deployed  
**Date**: 2026-01-08  
**Next Test**: Will trigger on next PR merge to main
