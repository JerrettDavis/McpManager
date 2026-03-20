using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for OpenClaw (https://openclaw.ai).
/// Handles file-backed OpenClaw-specific configuration and MCP server management.
/// OpenClaw stores MCP servers under the nested mcp.servers object, but installations
/// may have multiple config files across default, legacy, or profile-specific state roots.
/// </summary>
public class OpenClawConnector(
    Func<string, string?>? environmentLookup = null,
    Func<string>? homeDirectoryResolver = null,
    Func<string, bool>? fileExists = null,
    Func<string, bool>? directoryExists = null,
    Func<string, string, CancellationToken, Task<(int ExitCode, string StandardOutput, string StandardError)>>? processRunner = null) : IAgentConnector, IAgentRuntimeConnector
{
    private const string ConfigFileName = "openclaw.json";
    private const string ConfigScopePrefix = "config:";
    private const int RuntimeCatalogTimeoutMilliseconds = 10_000;
    private static readonly string[] LegacyStateDirectoryNames = [".clawdbot", ".moldbot", ".moltbot"];
    private static readonly string[] LegacyConfigFileNames = ["clawdbot.json", "moldbot.json", "moltbot.json"];
    private static readonly string[] ProfileStateDirectoryPrefixes = [".openclaw-", ".clawdbot-", ".moldbot-", ".moltbot-"];
    private readonly Func<string, string?> _environmentLookup = environmentLookup ?? Environment.GetEnvironmentVariable;
    private readonly Func<string> _homeDirectoryResolver = homeDirectoryResolver ??
        (() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
    private readonly Func<string, bool> _directoryExists = directoryExists ?? Directory.Exists;
    private readonly Func<string, string, CancellationToken, Task<(int ExitCode, string StandardOutput, string StandardError)>> _processRunner = processRunner ?? RunProcessAsync;

    public AgentType AgentType => AgentType.OpenClaw;

    public Task<bool> IsAgentInstalledAsync()
    {
        if (ResolveCandidateConfigPaths().Any(_fileExists))
        {
            return Task.FromResult(true);
        }

        var knownDirectories = EnumerateStateDirectories().ToList();
        if (knownDirectories.Any(_directoryExists))
        {
            return Task.FromResult(true);
        }

        var configDirectory = Path.GetDirectoryName(ResolveConfigPath());
        return Task.FromResult(!string.IsNullOrWhiteSpace(configDirectory) && _directoryExists(configDirectory));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(ResolveConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configuredServers = await GetConfiguredServersAsync();
        return configuredServers.Select(server => server.ServerId).ToList();
    }

    public async Task<AgentRuntimeCatalog?> GetRuntimeCatalogAsync()
    {
        using var timeout = new CancellationTokenSource(RuntimeCatalogTimeoutMilliseconds);

        try
        {
            var (exitCode, standardOutput, standardError) = await _processRunner(
                ResolveOpenClawCliExecutable(),
                "gateway call tools.catalog --json --timeout 10000",
                timeout.Token);

            if (exitCode != 0)
            {
                return CreateRuntimeCatalogError($"OpenClaw runtime query failed: {SummarizeProcessError(standardError, standardOutput)}");
            }

            if (string.IsNullOrWhiteSpace(standardOutput))
            {
                return CreateRuntimeCatalogError("OpenClaw runtime query returned no output.");
            }

            return ParseRuntimeCatalog(standardOutput);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return CreateRuntimeCatalogError("OpenClaw runtime query timed out.");
        }
        catch (Win32Exception ex)
        {
            return CreateRuntimeCatalogError($"OpenClaw CLI is unavailable: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return CreateRuntimeCatalogError($"OpenClaw runtime query failed: {ex.Message}");
        }
        catch (IOException ex)
        {
            return CreateRuntimeCatalogError($"OpenClaw runtime query failed: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateRuntimeCatalogError($"OpenClaw runtime query failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return CreateRuntimeCatalogError($"OpenClaw runtime output was invalid JSON: {ex.Message}");
        }
    }

    public async Task<IEnumerable<ConfiguredAgentServer>> GetConfiguredServersAsync()
    {
        var primaryConfigPath = ResolveConfigPath();
        var configuredServers = new Dictionary<string, ConfiguredAgentServer>(StringComparer.OrdinalIgnoreCase);

        foreach (var configPath in ResolveCandidateConfigPaths().Where(_fileExists))
        {
            try
            {
                var root = await ReadConfigAsync(configPath);
                var servers = GetServersObject(root);
                if (servers == null)
                {
                    continue;
                }

                foreach (var property in servers)
                {
                    var configuredServerKey = BuildConfiguredServerKey(primaryConfigPath, configPath, property.Key);
                    configuredServers[configuredServerKey] = new ConfiguredAgentServer
                    {
                        ConfiguredServerKey = configuredServerKey,
                        ServerId = ResolveCanonicalServerId(property.Key, property.Value),
                        IsEnabled = IsServerEnabled(property.Value),
                        RawConfig = property.Value is JsonObject serverConfig
                            ? ExtractRawConfig(serverConfig)
                            : new Dictionary<string, string>()
                    };
                }
            }
            catch (JsonException)
            {
                continue;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
        }

        return configuredServers.Values.ToList();
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var (configPath, configuredServerKey) = ResolveConfigTarget(serverId);
        var root = await ReadOrCreateConfigAsync(configPath);
        var servers = EnsureServersObject(root);

        servers[configuredServerKey] = BuildServerConfig(configuredServerKey, config);

        await WriteConfigAsync(configPath, root);
        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var (configPath, configuredServerKey) = ResolveConfigTarget(serverId);
        if (!_fileExists(configPath))
        {
            return false;
        }

        var root = await ReadConfigAsync(configPath);
        var servers = GetServersObject(root);
        if (servers == null || !servers.Remove(configuredServerKey))
        {
            return false;
        }

        CleanupEmptySections(root, servers);
        await WriteConfigAsync(configPath, root);
        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var (configPath, configuredServerKey) = ResolveConfigTarget(serverId);
        if (!_fileExists(configPath))
        {
            return false;
        }

        var root = await ReadConfigAsync(configPath);
        var servers = GetServersObject(root);
        if (servers == null || servers[configuredServerKey] is not JsonObject serverConfig)
        {
            return false;
        }

        if (enabled)
        {
            serverConfig.Remove("disabled");
        }
        else
        {
            serverConfig["disabled"] = JsonValue.Create(true);
        }

        await WriteConfigAsync(configPath, root);
        return true;
    }

    private string ResolveConfigPath()
    {
        var existing = ResolveCandidateConfigPaths().FirstOrDefault(_fileExists);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var configuredStateDir = GetEnvironmentValue("OPENCLAW_STATE_DIR") ?? GetEnvironmentValue("CLAWDBOT_STATE_DIR");
        if (!string.IsNullOrWhiteSpace(configuredStateDir))
        {
            return Path.Combine(ExpandHomePath(configuredStateDir), ConfigFileName);
        }

        return Path.Combine(ResolveDefaultStateDirectory(), ConfigFileName);
    }

    private static string ResolveOpenClawCliExecutable()
    {
        return OperatingSystem.IsWindows() ? "openclaw.cmd" : "openclaw";
    }

    private IEnumerable<string> ResolveCandidateConfigPaths()
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateConfigPaths())
        {
            if (seenPaths.Add(path))
            {
                yield return path;
            }
        }
    }

    private IEnumerable<string> EnumerateConfigPaths()
    {
        var explicitConfigPath = GetEnvironmentValue("OPENCLAW_CONFIG_PATH") ?? GetEnvironmentValue("CLAWDBOT_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(explicitConfigPath))
        {
            yield return ExpandHomePath(explicitConfigPath);
        }

        var configuredStateDir = GetEnvironmentValue("OPENCLAW_STATE_DIR") ?? GetEnvironmentValue("CLAWDBOT_STATE_DIR");
        if (!string.IsNullOrWhiteSpace(configuredStateDir))
        {
            var resolvedStateDir = ExpandHomePath(configuredStateDir);
            foreach (var candidate in EnumerateConfigPathsForStateDirectory(resolvedStateDir))
            {
                yield return candidate;
            }
        }

        foreach (var stateDirectory in EnumerateStateDirectories())
        {
            foreach (var candidate in EnumerateConfigPathsForStateDirectory(stateDirectory))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<string> EnumerateStateDirectories()
    {
        yield return ResolveDefaultStateDirectory();

        foreach (var legacyDirectory in LegacyStateDirectoryNames)
        {
            yield return Path.Combine(_homeDirectoryResolver(), legacyDirectory);
        }

        foreach (var profileStateDirectory in EnumerateProfileStateDirectories())
        {
            yield return profileStateDirectory;
        }
    }

    private IEnumerable<string> EnumerateProfileStateDirectories()
    {
        var homeDirectory = _homeDirectoryResolver();
        if (!_directoryExists(homeDirectory))
        {
            yield break;
        }

        foreach (var prefix in ProfileStateDirectoryPrefixes)
        {
            IEnumerable<string> matches;
            try
            {
                matches = Directory.EnumerateDirectories(homeDirectory, $"{prefix}*", SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var match in matches)
            {
                yield return match;
            }
        }
    }

    private IEnumerable<string> BuildConfigCandidates(string stateDirectory)
    {
        yield return Path.Combine(stateDirectory, ConfigFileName);

        foreach (var legacyFileName in LegacyConfigFileNames)
        {
            yield return Path.Combine(stateDirectory, legacyFileName);
        }
    }

    private IEnumerable<string> EnumerateConfigPathsForStateDirectory(string stateDirectory)
    {
        foreach (var candidate in BuildConfigCandidates(stateDirectory))
        {
            yield return candidate;
        }

        if (!_directoryExists(stateDirectory))
        {
            yield break;
        }

        IEnumerable<string> jsonFiles;
        try
        {
            jsonFiles = Directory.EnumerateFiles(stateDirectory, "*.json", SearchOption.TopDirectoryOnly);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var jsonFile in jsonFiles)
        {
            yield return jsonFile;
        }
    }

    private string ResolveDefaultStateDirectory()
    {
        return Path.Combine(_homeDirectoryResolver(), ".openclaw");
    }

    private string? GetEnvironmentValue(string key)
    {
        return _environmentLookup(key)?.Trim();
    }

    private string ExpandHomePath(string pathValue)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return pathValue;
        }

        if (pathValue.StartsWith("~"))
        {
            var relativePath = pathValue[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(_homeDirectoryResolver(), relativePath);
        }

        return Environment.ExpandEnvironmentVariables(pathValue);
    }

    private static JsonObject BuildServerConfig(string serverId, Dictionary<string, string>? config)
    {
        if (config == null || config.Count == 0)
        {
            throw new InvalidOperationException($"OpenClaw requires explicit configuration for '{serverId}'.");
        }

        var serverConfig = new JsonObject();

        foreach (var (key, value) in config)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            switch (key)
            {
                case "args":
                    serverConfig[key] = ParseArgsValue(value);
                    break;
                case "env":
                    serverConfig[key] = ParseEnvValue(value);
                    break;
                case "enabled":
                    if (bool.TryParse(value, out var enabled))
                    {
                        if (!enabled)
                        {
                            serverConfig["disabled"] = JsonValue.Create(true);
                        }
                    }
                    else
                    {
                        serverConfig[key] = value;
                    }
                    break;
                case "disabled":
                    if (bool.TryParse(value, out var disabled))
                    {
                        if (disabled)
                        {
                            serverConfig[key] = JsonValue.Create(true);
                        }
                    }
                    else
                    {
                        serverConfig[key] = value;
                    }
                    break;
                default:
                    serverConfig[key] = ParseScalarOrJsonValue(value);
                    break;
            }
        }

        if (serverConfig.Count == 0)
        {
            serverConfig["command"] = "npx";
            serverConfig["args"] = new JsonArray("-y", serverId);
        }

        return serverConfig;
    }

    private static JsonNode ParseArgsValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            var parsed = JsonNode.Parse(trimmed, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (parsed is JsonArray array)
            {
                return array;
            }
        }

        var args = TokenizeArguments(trimmed)
            .Select(arg => JsonValue.Create(arg))
            .ToArray();

        return new JsonArray(args);
    }

    private static JsonNode ParseEnvValue(string value)
    {
        var parsed = JsonNode.Parse(value, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        if (parsed is JsonObject jsonObject)
        {
            return jsonObject;
        }

        throw new InvalidOperationException("OpenClaw env configuration must be a JSON object.");
    }

    private static JsonNode ParseScalarOrJsonValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            var parsed = JsonNode.Parse(trimmed, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (parsed != null)
            {
                return parsed;
            }
        }

        if (bool.TryParse(trimmed, out var booleanValue))
        {
            return JsonValue.Create(booleanValue);
        }

        if (long.TryParse(trimmed, out var longValue))
        {
            return JsonValue.Create(longValue);
        }

        if (decimal.TryParse(trimmed, out var decimalValue))
        {
            return JsonValue.Create(decimalValue);
        }

        return JsonValue.Create(value)!;
    }

    private static JsonObject EnsureServersObject(JsonObject root)
    {
        if (root["mcp"] == null)
        {
            root["mcp"] = new JsonObject();
        }

        if (root["mcp"] is not JsonObject mcpObject)
        {
            throw new InvalidOperationException("OpenClaw config contains a non-object mcp section.");
        }

        if (mcpObject["servers"] == null)
        {
            mcpObject["servers"] = new JsonObject();
        }

        if (mcpObject["servers"] is not JsonObject serversObject)
        {
            throw new InvalidOperationException("OpenClaw config contains a non-object mcp.servers section.");
        }

        return serversObject;
    }

    private static JsonObject? GetServersObject(JsonObject root)
    {
        if (root["mcp"] is not JsonObject mcpObject)
        {
            return null;
        }

        return mcpObject["servers"] as JsonObject;
    }

    private static AgentRuntimeCatalog ParseRuntimeCatalog(string standardOutput)
    {
        var parsed = JsonNode.Parse(standardOutput, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) as JsonObject ?? throw new JsonException("OpenClaw runtime catalog root must be a JSON object.");

        var catalog = new AgentRuntimeCatalog
        {
            AgentId = parsed["agentId"]?.GetValue<string>() ?? string.Empty
        };

        if (parsed["groups"] is not JsonArray groups)
        {
            return catalog;
        }

        foreach (var groupNode in groups.OfType<JsonObject>())
        {
            var group = new AgentRuntimeGroup
            {
                Id = groupNode["id"]?.GetValue<string>() ?? string.Empty,
                Label = groupNode["label"]?.GetValue<string>() ?? groupNode["id"]?.GetValue<string>() ?? string.Empty,
                Source = groupNode["source"]?.GetValue<string>() ?? "runtime",
                PluginId = groupNode["pluginId"]?.GetValue<string>()
            };

            if (groupNode["tools"] is JsonArray tools)
            {
                foreach (var toolNode in tools.OfType<JsonObject>())
                {
                    group.Tools.Add(new AgentRuntimeTool
                    {
                        Id = toolNode["id"]?.GetValue<string>() ?? string.Empty,
                        Label = toolNode["label"]?.GetValue<string>() ?? toolNode["id"]?.GetValue<string>() ?? string.Empty,
                        Description = toolNode["description"]?.GetValue<string>() ?? string.Empty,
                        Source = toolNode["source"]?.GetValue<string>() ?? group.Source,
                        PluginId = toolNode["pluginId"]?.GetValue<string>() ?? group.PluginId,
                        Optional = toolNode["optional"]?.GetValue<bool>() ?? false
                    });
                }
            }

            catalog.Groups.Add(group);
        }

        return catalog;
    }

    private static AgentRuntimeCatalog CreateRuntimeCatalogError(string message)
    {
        return new AgentRuntimeCatalog
        {
            ErrorMessage = message
        };
    }

    private static string SummarizeProcessError(string standardError, string standardOutput)
    {
        var message = !string.IsNullOrWhiteSpace(standardError) ? standardError : standardOutput;
        if (string.IsNullOrWhiteSpace(message))
        {
            return "unknown error";
        }

        var firstLine = message
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstLine) ? message.Trim() : firstLine.Trim();
    }

    private (string ConfigPath, string ConfiguredServerKey) ResolveConfigTarget(string configuredServerKey)
    {
        if (TryParseConfiguredServerKey(configuredServerKey, out var configPath, out var parsedServerKey))
        {
            return (configPath, parsedServerKey);
        }

        return (ResolveConfigPath(), configuredServerKey);
    }

    private static string BuildConfiguredServerKey(string primaryConfigPath, string configPath, string configuredServerKey)
    {
        return PathsEqual(primaryConfigPath, configPath)
            ? configuredServerKey
            : $"{ConfigScopePrefix}{NormalizeConfigPath(configPath)}::{configuredServerKey}";
    }

    private static bool TryParseConfiguredServerKey(string configuredServerKey, out string configPath, out string parsedServerKey)
    {
        configPath = string.Empty;
        parsedServerKey = configuredServerKey;

        if (!configuredServerKey.StartsWith(ConfigScopePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var separatorIndex = configuredServerKey.IndexOf("::", ConfigScopePrefix.Length, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return false;
        }

        var normalizedPath = configuredServerKey[ConfigScopePrefix.Length..separatorIndex];
        parsedServerKey = configuredServerKey[(separatorIndex + 2)..];
        if (string.IsNullOrWhiteSpace(normalizedPath) || string.IsNullOrWhiteSpace(parsedServerKey))
        {
            return false;
        }

        configPath = DenormalizeConfigPath(normalizedPath);
        return true;
    }

    private static string NormalizeConfigPath(string configPath)
    {
        return configPath.Replace('\\', '/');
    }

    private static string DenormalizeConfigPath(string configPath)
    {
        return configPath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool PathsEqual(string leftPath, string rightPath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(leftPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(rightPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return string.Equals(NormalizeConfigPath(leftPath), NormalizeConfigPath(rightPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (NotSupportedException)
        {
            return string.Equals(NormalizeConfigPath(leftPath), NormalizeConfigPath(rightPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (PathTooLongException)
        {
            return string.Equals(NormalizeConfigPath(leftPath), NormalizeConfigPath(rightPath), StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ResolveCanonicalServerId(string configuredKey, JsonNode? serverNode)
    {
        if (serverNode is not JsonObject serverConfig)
        {
            return configuredKey;
        }

        var command = serverConfig["command"]?.GetValue<string>();
        var args = serverConfig["args"] as JsonArray;
        var derivedFromArgs = DeriveCanonicalIdFromCommandArgs(command, args);
        return string.IsNullOrWhiteSpace(derivedFromArgs) ? configuredKey : derivedFromArgs;
    }

    private static bool IsServerEnabled(JsonNode? serverNode)
    {
        if (serverNode is not JsonObject serverConfig)
        {
            return true;
        }

        if (serverConfig["disabled"] is not JsonValue disabledValue)
        {
            return true;
        }

        if (disabledValue.TryGetValue<bool>(out var disabled))
        {
            return !disabled;
        }

        if (disabledValue.TryGetValue<string>(out var disabledText) &&
            bool.TryParse(disabledText, out var parsedDisabled))
        {
            return !parsedDisabled;
        }

        return true;
    }

    private static Dictionary<string, string> ExtractRawConfig(JsonObject serverConfig)
    {
        var rawConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in serverConfig)
        {
            if (value == null)
            {
                continue;
            }

            rawConfig[key] = value switch
            {
                JsonArray or JsonObject => value.ToJsonString(),
                JsonValue jsonValue when jsonValue.TryGetValue<string>(out var stringValue) => stringValue,
                _ => value.ToJsonString()
            };
        }

        return rawConfig;
    }

    private static string? DeriveCanonicalIdFromCommandArgs(string? command, JsonArray? args)
    {
        if (string.IsNullOrWhiteSpace(command) || args == null)
        {
            return null;
        }

        var normalizedCommand = Path.GetFileNameWithoutExtension(command).ToLowerInvariant();
        if (normalizedCommand is not ("npx" or "npm" or "pnpm" or "bunx" or "uvx" or "pipx"))
        {
            return null;
        }

        var argumentValues = args
            .Select(arg => arg is JsonValue value ? value.GetValue<string>() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        if (!argumentValues.Any())
        {
            return null;
        }

        string? packageIdentifier;
        if (normalizedCommand == "npx")
        {
            packageIdentifier = TryGetPackageIdentifier(argumentValues, allowPackageFlags: true);
        }
        else if (normalizedCommand == "npm")
        {
            var npmSubcommand = argumentValues.FirstOrDefault();
            if (!string.Equals(npmSubcommand, "exec", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            argumentValues = argumentValues.Skip(1).ToList();
            packageIdentifier = TryGetPackageIdentifier(argumentValues, allowPackageFlags: true);
        }
        else if (normalizedCommand == "pnpm")
        {
            var pnpmSubcommand = argumentValues.FirstOrDefault();
            if (!string.Equals(pnpmSubcommand, "dlx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            argumentValues = argumentValues.Skip(1).ToList();
            packageIdentifier = TryGetPackageIdentifier(argumentValues, allowPackageFlags: false);
        }
        else if (normalizedCommand == "uvx")
        {
            packageIdentifier = TryGetPackageIdentifier(
                argumentValues,
                allowPackageFlags: false,
                flagsWithValues: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "--python",
                    "-p"
                });
        }
        else if (normalizedCommand == "pipx")
        {
            var pipxSubcommand = argumentValues.FirstOrDefault();
            if (!string.Equals(pipxSubcommand, "run", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            argumentValues = argumentValues.Skip(1).ToList();
            packageIdentifier = TryGetPackageIdentifier(
                argumentValues,
                allowPackageFlags: false,
                flagsWithValues: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "--python"
                });
        }
        else
        {
            packageIdentifier = TryGetPackageIdentifier(argumentValues, allowPackageFlags: false);
        }

        return string.IsNullOrWhiteSpace(packageIdentifier)
            ? null
            : NormalizePackageIdentifier(packageIdentifier);
    }

    private static string? TryGetPackageIdentifier(
        List<string> argumentValues,
        bool allowPackageFlags,
        IReadOnlySet<string>? flagsWithValues = null)
    {
        for (var index = 0; index < argumentValues.Count; index++)
        {
            var value = argumentValues[index];
            if (value == "--")
            {
                break;
            }

            if (allowPackageFlags)
            {
                if (value.StartsWith("--package=", StringComparison.OrdinalIgnoreCase))
                {
                    return value["--package=".Length..];
                }

                if ((string.Equals(value, "--package", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(value, "-p", StringComparison.OrdinalIgnoreCase)) &&
                    index + 1 < argumentValues.Count)
                {
                    return argumentValues[index + 1];
                }
            }

            if (!allowPackageFlags &&
                flagsWithValues?.Contains(value) == true &&
                index + 1 < argumentValues.Count)
            {
                index++;
                continue;
            }

            if (!value.StartsWith("-", StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }

    private static string NormalizePackageIdentifier(string packageIdentifier)
    {
        var identifier = packageIdentifier.Trim();

        if (identifier.StartsWith("@", StringComparison.Ordinal))
        {
            var versionSeparator = identifier.LastIndexOf('@');
            var scopeSeparator = identifier.IndexOf('/');
            if (versionSeparator > scopeSeparator)
            {
                identifier = identifier[..versionSeparator];
            }
        }
        else
        {
            var versionSeparator = identifier.IndexOf('@');
            if (versionSeparator > 0)
            {
                identifier = identifier[..versionSeparator];
            }
        }

        var lastSegment = identifier.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? identifier;
        foreach (var prefix in new[] { "mcp-server-", "server-", "mcp-" })
        {
            if (lastSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                lastSegment = lastSegment[prefix.Length..];
                break;
            }
        }

        return lastSegment;
    }

    private static IEnumerable<string> TokenizeArguments(string arguments)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quoteCharacter = null;
        var tokenStarted = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            var character = arguments[index];

            if (quoteCharacter != null)
            {
                if (character == '\\' && index + 1 < arguments.Length)
                {
                    var nextCharacter = arguments[index + 1];
                    if (nextCharacter == quoteCharacter || nextCharacter == '\\')
                    {
                        if (nextCharacter == quoteCharacter &&
                            (index + 2 >= arguments.Length || char.IsWhiteSpace(arguments[index + 2])))
                        {
                            current.Append(character);
                            tokenStarted = true;
                            continue;
                        }

                        current.Append(nextCharacter);
                        tokenStarted = true;
                        index++;
                        continue;
                    }
                }

                if (character == quoteCharacter)
                {
                    quoteCharacter = null;
                }
                else
                {
                    current.Append(character);
                    tokenStarted = true;
                }

                continue;
            }

            if (character is '"' or '\'')
            {
                quoteCharacter = character;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (tokenStarted)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }

                continue;
            }

            current.Append(character);
            tokenStarted = true;
        }

        if (tokenStarted)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static void CleanupEmptySections(JsonObject root, JsonObject servers)
    {
        if (servers.Count > 0)
        {
            return;
        }

        if (root["mcp"] is JsonObject mcpObject)
        {
            mcpObject.Remove("servers");
            if (mcpObject.Count == 0)
            {
                root.Remove("mcp");
            }
        }
    }

    private static async Task WriteConfigAsync(string configPath, JsonObject root)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(configPath, json);
    }

    private static async Task<JsonObject> ReadConfigAsync(string configPath)
    {
        var raw = await File.ReadAllTextAsync(configPath);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new JsonObject();
        }

        var parsed = JsonNode.Parse(raw, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return parsed as JsonObject
               ?? throw new JsonException("OpenClaw config root must be a JSON object.");
    }

    private static async Task<JsonObject> ReadOrCreateConfigAsync(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new JsonObject();
        }

        return await ReadConfigAsync(configPath);
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }

        return (process.ExitCode, await standardOutputTask, await standardErrorTask);
    }
}
