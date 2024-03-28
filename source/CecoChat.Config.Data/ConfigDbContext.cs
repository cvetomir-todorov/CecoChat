using Microsoft.EntityFrameworkCore;

namespace CecoChat.Config.Data;

public class ConfigDbContext : DbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options)
    { }

    public DbSet<ElementEntity> Elements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ElementEntity>(entity =>
        {
            entity.ToTable("elements");

            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasKey(e => e.Name);
            entity.Property(e => e.Version).IsConcurrencyToken();
        });

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
