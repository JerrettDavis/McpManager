using System.Diagnostics;

namespace McpManager.Tests.Desktop;

public class DesktopAppBuildTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _projectPath;

    public DesktopAppBuildTests()
    {
        var baseDir = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), 
            "..", "..", "..", "..", "..", "src", "McpManager.Desktop"));
        
        _projectPath = Path.Combine(baseDir, "McpManager.Desktop.csproj");
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"McpManager.Tests.Desktop.{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
    }

    [Fact]
    public void Desktop_Project_Should_Exist()
    {
        Assert.True(File.Exists(_projectPath), 
            $"Desktop project file should exist at {_projectPath}");
    }

    [Fact]
    public void Desktop_Project_Should_Build_Successfully()
    {
        var result = RunDotNetCommand("build", _projectPath, "--configuration Release");
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build succeeded", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Desktop_Project_Should_Reference_Required_Dependencies()
    {
        var projectContent = File.ReadAllText(_projectPath);
        
        Assert.Contains("Photino.Blazor", projectContent);
        Assert.Contains("McpManager.Core", projectContent);
        Assert.Contains("McpManager.Application", projectContent);
        Assert.Contains("McpManager.Infrastructure", projectContent);
        Assert.Contains("McpManager.Web", projectContent);
    }

    [Fact]
    public void Desktop_Project_Should_Publish_Successfully()
    {
        var outputPath = Path.Combine(_testOutputDir, "publish");
        var result = RunDotNetCommand("publish", _projectPath, 
            $"--configuration Release --output {outputPath} --self-contained false");
        
        Assert.Equal(0, result.ExitCode);
        Assert.True(Directory.Exists(outputPath), "Publish output directory should exist");
        
        var files = Directory.GetFiles(outputPath);
        Assert.NotEmpty(files);
    }

    [Fact]
    public void Desktop_Project_Should_Include_Required_Files()
    {
        var projectDir = Path.GetDirectoryName(_projectPath)!;
        
        Assert.True(File.Exists(Path.Combine(projectDir, "Program.cs")), 
            "Program.cs should exist");
        Assert.True(File.Exists(Path.Combine(projectDir, "App.razor")), 
            "App.razor should exist");
        Assert.True(File.Exists(Path.Combine(projectDir, "_Imports.razor")), 
            "_Imports.razor should exist");
        Assert.True(Directory.Exists(Path.Combine(projectDir, "wwwroot")), 
            "wwwroot directory should exist");
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("linux-x64")]
    public void Desktop_Project_Should_Publish_For_Runtime(string runtime)
    {
        var outputPath = Path.Combine(_testOutputDir, $"publish-{runtime}");
        var result = RunDotNetCommand("publish", _projectPath,
            $"--configuration Release --runtime {runtime} --self-contained true --output {outputPath}");
        
        Assert.Equal(0, result.ExitCode);
        Assert.True(Directory.Exists(outputPath), 
            $"Publish output directory should exist for {runtime}");
        
        var expectedExe = runtime.StartsWith("win") ? "McpManager.Desktop.exe" : "McpManager.Desktop";
        var exePath = Path.Combine(outputPath, expectedExe);
        
        Assert.True(File.Exists(exePath), 
            $"Desktop executable should exist at {exePath} for runtime {runtime}");
    }

    [Fact]
    public void Desktop_App_Razor_Components_Should_Compile()
    {
        var projectDir = Path.GetDirectoryName(_projectPath)!;
        var appRazorPath = Path.Combine(projectDir, "App.razor");
        var appContent = File.ReadAllText(appRazorPath);

        Assert.Contains("Routes", appContent);
    }

    [Fact]
    public void Desktop_Program_Should_Use_Shared_Service_Extensions()
    {
        var projectDir = Path.GetDirectoryName(_projectPath)!;
        var programPath = Path.Combine(projectDir, "Program.cs");
        var programContent = File.ReadAllText(programPath);
        
        // Verify use of shared service registration
        Assert.Contains("AddMcpManagerServices", programContent);
        Assert.Contains("PhotinoBlazorAppBuilder", programContent);
    }

    [Fact]
    public void Desktop_Program_Should_Configure_Window()
    {
        var projectDir = Path.GetDirectoryName(_projectPath)!;
        var programPath = Path.Combine(projectDir, "Program.cs");
        var programContent = File.ReadAllText(programPath);
        
        // Verify window configuration
        Assert.Contains("SetTitle", programContent);
        Assert.Contains("SetSize", programContent);
        Assert.Contains("SetResizable", programContent);
    }

    private (int ExitCode, string Output) RunDotNetCommand(string command, string projectPath, string args = "")
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} \"{projectPath}\" {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start dotnet process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var allOutput = output + "\n" + error;
        return (process.ExitCode, allOutput);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch (IOException)
            {
                // Ignore if directory is in use
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore if we don't have permissions
            }
        }
    }
}
