using Microsoft.EntityFrameworkCore;
using McpManager.Infrastructure.Persistence.Entities;

namespace McpManager.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for MCP Manager.
/// Manages persistence of installed servers, cached registry data, and metadata.
/// </summary>
public class McpManagerDbContext(DbContextOptions<McpManagerDbContext> options) : DbContext(options)
{
    public DbSet<InstalledServerEntity> InstalledServers => Set<InstalledServerEntity>();
    public DbSet<CachedRegistryServerEntity> CachedRegistryServers => Set<CachedRegistryServerEntity>();
    public DbSet<RegistryMetadataEntity> RegistryMetadata => Set<RegistryMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure InstalledServerEntity
        modelBuilder.Entity<InstalledServerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Version).HasMaxLength(100);
            entity.Property(e => e.Author).HasMaxLength(500);
            entity.Property(e => e.RepositoryUrl).HasMaxLength(1000);
            entity.Property(e => e.InstallCommand).HasMaxLength(2000);
            entity.Property(e => e.TagsJson).IsRequired();
            entity.Property(e => e.ConfigurationJson).IsRequired();
            entity.Property(e => e.InstalledAt).IsRequired();
            entity.Property(e => e.RegistrySource).HasMaxLength(500);
            entity.Property(e => e.InstallLocation).HasMaxLength(1000);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.InstalledAt);
        });

        // Configure CachedRegistryServerEntity
        modelBuilder.Entity<CachedRegistryServerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegistryName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ServerId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Version).HasMaxLength(100);
            entity.Property(e => e.Author).HasMaxLength(500);
            entity.Property(e => e.RepositoryUrl).HasMaxLength(1000);
            entity.Property(e => e.InstallCommand).HasMaxLength(2000);
            entity.Property(e => e.TagsJson).IsRequired();
            entity.Property(e => e.FetchedAt).IsRequired();

            entity.HasIndex(e => new { e.RegistryName, e.ServerId });
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.FetchedAt);
        });

        // Configure RegistryMetadataEntity
        modelBuilder.Entity<RegistryMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.RegistryName);
            entity.Property(e => e.RegistryName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.LastRefreshAt).IsRequired();
            entity.Property(e => e.LastRefreshError).HasMaxLength(2000);

            entity.HasIndex(e => e.NextRefreshAt);
        });
    }
}
