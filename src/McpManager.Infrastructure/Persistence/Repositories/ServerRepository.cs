using Microsoft.EntityFrameworkCore;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence.Entities;
using System.Text.Json;

namespace McpManager.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for persisting installed MCP servers using EF Core.
/// </summary>
public class ServerRepository : IServerRepository
{
    private readonly McpManagerDbContext _context;

    public ServerRepository(McpManagerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<McpServer>> GetAllAsync()
    {
        var entities = await _context.InstalledServers.ToListAsync();
        return entities.Select(MapToModel);
    }

    public async Task<McpServer?> GetByIdAsync(string serverId)
    {
        var entity = await _context.InstalledServers.FindAsync(serverId);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<bool> AddAsync(McpServer server)
    {
        if (await ExistsAsync(server.Id))
        {
            return false;
        }

        var entity = MapToEntity(server);
        _context.InstalledServers.Add(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(McpServer server)
    {
        var entity = await _context.InstalledServers.FindAsync(server.Id);
        if (entity == null)
        {
            return false;
        }

        entity.Name = server.Name;
        entity.Description = server.Description;
        entity.Version = server.Version;
        entity.Author = server.Author;
        entity.RepositoryUrl = server.RepositoryUrl;
        entity.InstallCommand = server.InstallCommand;
        entity.TagsJson = JsonSerializer.Serialize(server.Tags);
        entity.ConfigurationJson = JsonSerializer.Serialize(server.Configuration);
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string serverId)
    {
        var entity = await _context.InstalledServers.FindAsync(serverId);
        if (entity == null)
        {
            return false;
        }

        _context.InstalledServers.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string serverId)
    {
        return await _context.InstalledServers.AnyAsync(s => s.Id == serverId);
    }

    private static McpServer MapToModel(InstalledServerEntity entity)
    {
        return new McpServer
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Version = entity.Version,
            Author = entity.Author,
            RepositoryUrl = entity.RepositoryUrl,
            InstallCommand = entity.InstallCommand,
            Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new List<string>(),
            IsInstalled = true,
            InstalledAt = entity.InstalledAt,
            Configuration = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.ConfigurationJson) 
                ?? new Dictionary<string, string>()
        };
    }

    private static InstalledServerEntity MapToEntity(McpServer server)
    {
        return new InstalledServerEntity
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            Version = server.Version,
            Author = server.Author,
            RepositoryUrl = server.RepositoryUrl,
            InstallCommand = server.InstallCommand,
            TagsJson = JsonSerializer.Serialize(server.Tags),
            InstalledAt = server.InstalledAt ?? DateTime.UtcNow,
            ConfigurationJson = JsonSerializer.Serialize(server.Configuration)
        };
    }
}
