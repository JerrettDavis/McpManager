using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace McpManager.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// </summary>
public class McpManagerDbContextFactory : IDesignTimeDbContextFactory<McpManagerDbContext>
{
    public McpManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<McpManagerDbContext>();
        
        // Use a default SQLite database path for migrations
        optionsBuilder.UseSqlite("Data Source=mcpmanager.db");

        return new McpManagerDbContext(optionsBuilder.Options);
    }
}
