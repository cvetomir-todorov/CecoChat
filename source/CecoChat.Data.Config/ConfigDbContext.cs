using Microsoft.EntityFrameworkCore;

namespace CecoChat.Data.Config;

public class ConfigDbContext : DbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options)
    { }

    public DbSet<ElementEntity> Elements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ElementEntity>().HasKey(e => e.Name);
        modelBuilder.Entity<ElementEntity>().Property(e => e.Version).IsConcurrencyToken();

        base.OnModelCreating(modelBuilder);
    }
}

public interface IVersionEntity
{
    DateTime Version { get; set; }
}

public sealed class ElementEntity : IVersionEntity
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime Version { get; set; }
}
