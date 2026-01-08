# Pull Request

## Description
<!-- Provide a brief description of your changes -->

## Type of Change
<!-- Mark the appropriate option with an 'x' -->

- [ ] 🐛 Bug fix (patch version bump - e.g., 0.1.0 → 0.1.1)
- [ ] ✨ New feature (minor version bump - e.g., 0.1.0 → 0.2.0)
- [ ] 💥 Breaking change (major version bump - e.g., 0.1.0 → 1.0.0)
- [ ] 📝 Documentation update (patch version bump)
- [ ] 🎨 Code style/refactor (patch version bump)
- [ ] ⚡ Performance improvement (patch version bump)
- [ ] ✅ Test updates (patch version bump)
- [ ] 🔧 CI/CD change (patch version bump)

## Conventional Commit Title Format
<!-- Your PR title should follow conventional commit format -->

**Examples:**
- `feat: add server auto-discovery` (minor bump)
- `fix: resolve desktop routing issue` (patch bump)
- `feat!: redesign configuration API` (major bump - breaking)
- `docs: update installation guide` (patch bump)

## Versioning
<!-- The version will be automatically updated based on your PR title and labels -->

**Automated Version Bump:** ✅ The workflow will automatically:
1. Detect the version increment type from your PR title/labels
2. Update `version.json` if needed
3. Commit the change to your PR branch
4. Add a comment with the version details

You don't need to manually update `version.json` - just ensure your PR title follows the format above!

## Related Issue
<!-- Link to the issue this PR addresses -->
Fixes #(issue)

## Changes Made
<!-- List the specific changes made in this PR -->
- 
- 

## Testing
<!-- Describe the tests you ran to verify your changes -->
- [ ] Unit tests pass locally
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes

## Checklist
- [ ] My code follows the style guidelines of this project
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] PR title follows conventional commit format (feat:, fix:, etc.)
- [ ] Any dependent changes have been merged and published

