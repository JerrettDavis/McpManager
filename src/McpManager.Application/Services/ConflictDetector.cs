using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Detects version and configuration conflicts for MCP servers across agents.
/// Compares RawConfig dictionaries from ConfiguredAgentServer entries to identify mismatches.
/// </summary>
public class ConflictDetector(
    IAgentManager agentManager,
    IInstallationManager installationManager
) : IConflictDetector
{
    public async Task<IReadOnlyList<ServerConflict>> DetectAllConflictsAsync()
    {
        var agents = (await agentManager.DetectInstalledAgentsAsync()).ToList();
        if (agents.Count == 0)
            return [];

        var serverEntries = new Dictionary<string, List<(Agent Agent, ConfiguredAgentServer Config)>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var agent in agents)
        {
            foreach (var server in agent.ConfiguredServers)
            {
                var serverId = server.ServerId;
                if (string.IsNullOrWhiteSpace(serverId))
                    continue;

                if (!serverEntries.TryGetValue(serverId, out var entries))
                {
                    entries = [];
                    serverEntries[serverId] = entries;
                }
                entries.Add((agent, server));
            }
        }

        var conflicts = new List<ServerConflict>();
        foreach (var (serverId, entries) in serverEntries)
        {
            var conflict = AnalyzeEntries(serverId, entries);
            if (conflict != null)
                conflicts.Add(conflict);
        }

        return conflicts;
    }

    public async Task<ServerConflict?> DetectConflictForServerAsync(string serverId)
    {
        var agents = (await agentManager.DetectInstalledAgentsAsync()).ToList();
        if (agents.Count == 0)
            return null;

        var entries = new List<(Agent Agent, ConfiguredAgentServer Config)>();
        foreach (var agent in agents)
        {
            foreach (var server in agent.ConfiguredServers)
            {
                if (string.Equals(server.ServerId, serverId, StringComparison.OrdinalIgnoreCase))
                    entries.Add((agent, server));
            }
        }

        return AnalyzeEntries(serverId, entries);
    }

    private static ServerConflict? AnalyzeEntries(
        string serverId,
        List<(Agent Agent, ConfiguredAgentServer Config)> entries)
    {
        if (entries.Count < 2)
            return null;

        // Check for duplicates within a single agent first
        var duplicateGroups = entries
            .GroupBy(e => e.Agent.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            return new ServerConflict
            {
                ServerId = serverId,
                Type = ConflictType.Duplicate,
                Entries = entries.Select(e => new AgentConflictEntry
                {
                    AgentId = e.Agent.Id,
                    AgentName = e.Agent.Name,
                    ConfiguredServerKey = e.Config.ConfiguredServerKey,
                    RawConfig = new Dictionary<string, string>(e.Config.RawConfig)
                }).ToList()
            };
        }

        // Cross-agent comparison
        var firstConfig = entries[0].Config.RawConfig;
        var hasCommandOrArgsDifference = false;
        var hasOtherDifference = false;

        for (var i = 1; i < entries.Count; i++)
        {
            var otherConfig = entries[i].Config.RawConfig;
            var (commandArgsDiff, otherDiff) = CompareConfigs(firstConfig, otherConfig);
            hasCommandOrArgsDifference |= commandArgsDiff;
            hasOtherDifference |= otherDiff;
        }

        if (!hasCommandOrArgsDifference && !hasOtherDifference)
            return null;

        var conflictType = hasCommandOrArgsDifference
            ? ConflictType.VersionMismatch
            : ConflictType.ConfigMismatch;

        return new ServerConflict
        {
            ServerId = serverId,
            Type = conflictType,
            Entries = entries.Select(e => new AgentConflictEntry
            {
                AgentId = e.Agent.Id,
                AgentName = e.Agent.Name,
                ConfiguredServerKey = e.Config.ConfiguredServerKey,
                RawConfig = new Dictionary<string, string>(e.Config.RawConfig)
            }).ToList()
        };
    }

    private static (bool CommandArgsDiff, bool OtherDiff) CompareConfigs(
        Dictionary<string, string> a,
        Dictionary<string, string> b)
    {
        var commandArgsDiff = false;
        var otherDiff = false;

        var allKeys = new HashSet<string>(a.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(b.Keys);

        foreach (var key in allKeys)
        {
            var aHas = a.TryGetValue(key, out var aVal);
            var bHas = b.TryGetValue(key, out var bVal);

            if (aHas == bHas && string.Equals(aVal, bVal, StringComparison.Ordinal))
                continue;

            if (key.Equals("command", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("args", StringComparison.OrdinalIgnoreCase))
            {
                commandArgsDiff = true;
            }
            else
            {
                otherDiff = true;
            }
        }

        return (commandArgsDiff, otherDiff);
    }
}
