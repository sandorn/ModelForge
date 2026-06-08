using Microsoft.EntityFrameworkCore;
using ModelForge.Contracts;

namespace ModelForge.Backend.Data;

public sealed class ModelForgeDbContext : DbContext
{
    public ModelForgeDbContext(DbContextOptions<ModelForgeDbContext> options) : base(options) { }

    public DbSet<ConfigurationEntry> Configurations => Set<ConfigurationEntry>();
    public DbSet<AuditEventEntry> AuditEvents => Set<AuditEventEntry>();
    public DbSet<LinkMetadataEntry> LinkMetadata => Set<LinkMetadataEntry>();
    public DbSet<DictionaryEntry> DictionaryTerms => Set<DictionaryEntry>();
    public DbSet<UserEntry> Users => Set<UserEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationEntry>(entity =>
        {
            entity.HasKey(e => new { e.Scope, e.Key });
            entity.Property(e => e.Scope).HasMaxLength(64);
            entity.Property(e => e.Key).HasMaxLength(128);
            entity.Property(e => e.Value).HasMaxLength(2048);
        });

        modelBuilder.Entity<AuditEventEntry>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(64);
            entity.Property(e => e.EventType).HasMaxLength(128);
            entity.Property(e => e.ActorId).HasMaxLength(128);
            entity.HasIndex(e => e.RecordedAtUtc);
            entity.HasIndex(e => e.EventType);
        });

        modelBuilder.Entity<LinkMetadataEntry>(entity =>
        {
            entity.HasKey(e => e.LinkId);
            entity.Property(e => e.LinkId).HasMaxLength(64);
            entity.Property(e => e.SourceDocumentId).HasMaxLength(256);
            entity.Property(e => e.TargetDocumentId).HasMaxLength(256);
        });

        modelBuilder.Entity<DictionaryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Term).HasMaxLength(256);
        });

        modelBuilder.Entity<UserEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Username).HasMaxLength(128);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Role).HasMaxLength(32);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PasswordSalt).HasMaxLength(64);
        });
    }
}

public sealed class ConfigurationEntry
{
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEventEntry
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public int Host { get; set; }
    public int Severity { get; set; }
    public string? CommandId { get; set; }
    public string? ResourceId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LinkMetadataEntry
{
    public string LinkId { get; set; } = string.Empty;
    public LinkSourceType SourceType { get; set; }
    public string SourceDocumentId { get; set; } = string.Empty;
    public string SourceAddress { get; set; } = string.Empty;
    public LinkTargetType TargetType { get; set; }
    public string TargetDocumentId { get; set; } = string.Empty;
    public string TargetAddress { get; set; } = string.Empty;
    public string RefreshPolicy { get; set; } = "manual";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastRefreshedAtUtc { get; set; }
}

public sealed class DictionaryEntry
{
    public string Id { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string? Replacement { get; set; }
    public string? RegexPattern { get; set; }
    public string Category { get; set; } = "General";
    public string Severity { get; set; } = "Warning";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class UserEntry
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "Analyst";
    public bool IsActive { get; set; } = true;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}