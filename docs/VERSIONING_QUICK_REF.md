# 🏷️ Versioning Quick Reference

> **TL;DR**: Use conventional commit format in your PR title. Version updates automatically. That's it!

## PR Title Format

```
<type>[scope][!]: <description>
```

## Version Bumps

| PR Title Pattern | Example | Bump |
|-----------------|---------|------|
| `feat!:` | `feat!: redesign API` | **0.1.0 → 1.0.0** (MAJOR) |
| `feat:` | `feat: add feature` | **0.1.0 → 0.2.0** (MINOR) |
| `fix:` | `fix: bug fix` | **0.1.0 → 0.1.1** (PATCH) |
| `docs:` | `docs: update guide` | **0.1.0 → 0.1.1** (PATCH) |
| `chore:` | `chore: update deps` | **0.1.0 → 0.1.1** (PATCH) |

## What Happens

1. **You**: Create PR with title `feat: add discovery`
2. **Workflow**: Updates `version.json` to `0.2.0`
3. **Workflow**: Commits to your PR branch
4. **Workflow**: Posts comment explaining bump
5. **You**: Merge PR
6. **Workflow**: Creates tag `v0.2.0`
7. **Workflow**: Builds release

## Do ✅

- ✅ Use conventional commit format in PR title
- ✅ Review automated comment on your PR
- ✅ Check version is correct before merging

## Don't ❌

- ❌ Edit `version.json` manually
- ❌ Create tags manually
- ❌ Calculate versions yourself

## Commit Types

- `feat:` - New feature (MINOR)
- `fix:` - Bug fix (PATCH)
- `docs:` - Documentation (PATCH)
- `chore:` - Maintenance (PATCH)
- `refactor:` - Code cleanup (PATCH)
- `perf:` - Performance (PATCH)
- `test:` - Tests (PATCH)
- `style:` - Formatting (PATCH)
- `ci:` - CI/CD (PATCH)

## Breaking Changes

Add `!` after type:

```
feat!: redesign configuration API
fix!: remove legacy auth method
```

## Need Help?

- 📖 See [VERSIONING.md](VERSIONING.md) for full guide
- 📝 See [CONTRIBUTING.md](../CONTRIBUTING.md) for workflow
- 💬 Check automated PR comment for version explanation

---

**Remember**: The workflow does everything. Just write a good PR title! 🎉
