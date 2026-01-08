using System.Reflection;

namespace McpManager.Application.Services;

public interface IVersionService
{
    string GetVersion();
    string GetInformationalVersion();
    string GetAssemblyVersion();
}

public class VersionService : IVersionService
{
    private readonly string _version;
    private readonly string _informationalVersion;
    private readonly string _assemblyVersion;

    public VersionService()
    {
        // Use entry assembly (Web or Desktop) instead of executing assembly (Application)
        // This ensures we read the version from the app that has Nerdbank.GitVersioning
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        
        // Get version from Nerdbank.GitVersioning attributes
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _informationalVersion = versionAttribute?.InformationalVersion ?? "0.0.0";
        
        // Extract simple version (e.g., "0.1.0" from "0.1.0+abc123")
        _version = _informationalVersion.Split('+')[0].Split('-')[0];
        
        // Get assembly version
        var assemblyVersion = assembly.GetName().Version;
        _assemblyVersion = assemblyVersion != null 
            ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
            : "0.0.0";
    }

    public string GetVersion() => _version;

    public string GetInformationalVersion() => _informationalVersion;

    public string GetAssemblyVersion() => _assemblyVersion;
}
