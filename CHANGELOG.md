# Changelog

All notable changes to MCP Manager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.5] - 2026-01-12

### Added

#### Bulk Agent Management
- Added ability to add/remove servers from multiple agents simultaneously from Server Details page
- Added checkboxes to select/deselect agents for a server
- Added "Select All" and "Deselect All" bulk action buttons
- Added visual indicators showing current status vs. pending changes
- Added real-time status updates showing what changes will be applied
- Added success/error messages with detailed operation results
- See [BULK_AGENT_MANAGEMENT.md](docs/BULK_AGENT_MANAGEMENT.md) for details

#### Debug Page Cleanup Tools
- Added "Detect Duplicates" tool to identify duplicate servers with same name but different IDs
- Added "Remove Duplicates" tool to automatically clean up duplicate servers
- Added "Sync Installation Records" tool to create missing installation records
- Added "Remove Orphaned Records" tool to clean up stale installation records
- Added color-coded status messages for all cleanup operations
- Added processing state to prevent concurrent operations
- See [DEBUG_CLEANUP_TOOLS.md](docs/DEBUG_CLEANUP_TOOLS.md) for details

### Changed

#### Comprehensive Styling Update
- **Replaced** vaporwave pastel color palette with professional blue-gray theme
- **Removed** inappropriate hover effects from non-interactive cards and list items
- **Refined** typography with better font sizes, weights, and letter-spacing
- **Improved** button styling with subtle animations and consistent sizing
- **Enhanced** form inputs with cleaner focus states and better accessibility
- **Updated** badge, alert, and table styling for more professional appearance
- **Simplified** shadows and transitions for cleaner, less "AI-generated" look
- **Maintained** all responsive layouts and sidebar structure
- See [STYLING_UPDATE_V0.1.5.md](docs/STYLING_UPDATE_V0.1.5.md) for complete details

### Fixed
- Fixed syntax error in AgentServerSyncWorker.cs causing build failures
- Fixed duplicate server creation by checking for existing servers by name before installing
- Fixed missing closing brace in AgentServerSyncWorker duplicate prevention logic

### Documentation
- Added [BULK_AGENT_MANAGEMENT.md](docs/BULK_AGENT_MANAGEMENT.md) - Comprehensive guide for bulk agent management
- Added [DEBUG_CLEANUP_TOOLS.md](docs/DEBUG_CLEANUP_TOOLS.md) - Documentation for automated cleanup tools
- Added [STYLING_UPDATE_V0.1.5.md](docs/STYLING_UPDATE_V0.1.5.md) - Complete styling update documentation
- Updated [CLEANUP_DUPLICATES.md](docs/CLEANUP_DUPLICATES.md) to recommend automated tools

## [0.1.4] - 2026-01-12

### Fixed
- Fixed duplicate server creation in background worker
- Background worker now checks for existing servers by name before creating new ones
- Background worker checks again after registry search to prevent race conditions
- Added name-based matching fallback to prevent duplicates with different IDs

### Changed
- Background worker no longer creates duplicates when auto-discovering servers
- Duplicate prevention logic now handles concurrent server creation

## [0.1.3] - 2026-01-12

### Added
- Added Debug page (`/debug`) for troubleshooting sync issues
- Added UI-side sync on Agent Details page load
- Shows all agents, configured servers, installation records, and sync status
- Added ability to view agent config file contents on Debug page

### Fixed
- Fixed server ID mismatch between config files and installed servers
- Background worker now overrides registry IDs with config file IDs for consistency
- UI now matches servers by name when exact ID doesn't match
- Installation manager's AddServerToAgentAsync is now idempotent

### Changed
- Config file IDs are now the source of truth for server IDs
- Registry IDs are overridden during auto-installation to match config
- UI sync creates installation records using actual installed server IDs

### Documentation
- Added [SERVER_ID_MATCHING_FIX.md](docs/SERVER_ID_MATCHING_FIX.md) - Details on ID matching fix
- Added [INSTALLATION_SYNC_FIX.md](docs/INSTALLATION_SYNC_FIX.md) - Details on sync improvements
- Added [DEBUGGING_SYNC_ISSUES.md](docs/DEBUGGING_SYNC_ISSUES.md) - Troubleshooting guide

## [0.1.2] - 2026-01-11

### Fixed
- Fixed background workers not running in Desktop app (Photino)
- Desktop app now manually starts background workers since Photino doesn't support IHostedService

### Changed
- Background workers registered as singletons in Desktop app
- Workers started manually in Program.cs with proper cancellation token handling

### Documentation
- Added [DESKTOP_BACKGROUND_WORKERS.md](docs/DESKTOP_BACKGROUND_WORKERS.md) - Explains desktop worker fix

## [0.1.1] - 2026-01-11

### Added
- Auto-detection of MCP servers from Claude Code's user-scoped config (`~/.claude.json`)
- Support for both user-level and project-level `mcpServers` in Claude config
- ConfigurationWatcher service with FileSystemWatcher for detecting external config changes
- ConfigurationWatcherWorker background service for monitoring config files
- Console logging in ClaudeCodeConnector for debugging detection

### Changed
- ClaudeCodeConnector now reads from `~/.claude.json` in addition to legacy `~/.claude/settings.json`
- Added support for multiple path formats in project-level server detection
- Background worker now auto-installs servers found in agent configs

### Documentation
- Added [CLAUDE_CODE_CONFIGURATION.md](docs/CLAUDE_CODE_CONFIGURATION.md) - Claude Code config structure

## [0.1.0] - 2026-01-10

### Added
- Initial release of MCP Manager
- Browse and search MCP servers from Smithery registry
- Install and manage MCP servers
- Multi-agent support (Claude Code, GitHub Copilot)
- Agent-specific server configuration
- Global server configuration with auto-propagation
- Background workers for registry refresh and agent server sync
- Docker support
- Desktop app using Photino.Blazor
- Comprehensive unit test coverage

### Architecture
- Clean Architecture with SOLID principles
- Core domain layer with models and interfaces
- Application layer with business logic
- Infrastructure layer with connectors and registries
- Blazor Server web UI
- Plugin-based architecture for agents and registries

[0.1.5]: https://github.com/JerrettDavis/McpManager/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/JerrettDavis/McpManager/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/JerrettDavis/McpManager/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/JerrettDavis/McpManager/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/JerrettDavis/McpManager/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/JerrettDavis/McpManager/releases/tag/v0.1.0
