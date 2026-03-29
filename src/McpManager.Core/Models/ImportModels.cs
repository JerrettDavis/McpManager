namespace McpManager.Core.Models;

public class ImportSource
{
    public string AgentName { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public bool Detected { get; set; }
    public List<ImportableServer> Servers { get; set; } = [];
}

public class ImportableServer
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = [];
    public Dictionary<string, string> Env { get; set; } = [];
    public bool AlreadyManaged { get; set; }
    public bool Selected { get; set; } = true;
}

public class ImportResult
{
    public int TotalDetected { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<ImportedServerResult> Details { get; set; } = [];
}

public class ImportedServerResult
{
    public string ServerName { get; set; } = string.Empty;
    public string SourceAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
