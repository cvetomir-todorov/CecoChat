using Microsoft.EntityFrameworkCore;

namespace CecoChat.Data.User;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    { }

    public DbSet<ProfileEntity> Profiles { get; set; } = null!;

    public DbSet<ConnectionEntity> Connections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ProfileEntity>().HasKey(e => e.UserId);
        modelBuilder.Entity<ProfileEntity>().Property(e => e.UserId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ProfileEntity>().Property(e => e.Version).IsConcurrencyToken();

        modelBuilder.Entity<ConnectionEntity>().HasKey(nameof(ConnectionEntity.User1Id), nameof(ConnectionEntity.User2Id));
        modelBuilder.Entity<ConnectionEntity>().Property(e => e.Status)
            .HasConversion<string>(
                status => status.ToString(),
                statusString => (ConnectionEntityStatus)Enum.Parse(typeof(ConnectionEntityStatus), statusString));
        modelBuilder.Entity<ConnectionEntity>().Property(e => e.Version).IsConcurrencyToken();

        base.OnModelCreating(modelBuilder);
    }
}

public interface IVersionEntity
{
    Guid Version { get; set; }
}

public sealed class ProfileEntity : IVersionEntity
{
    public long UserId { get; set; }
    public Guid Version { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class ConnectionEntity : IVersionEntity
{
    public long User1Id { get; set; }
    public long User2Id { get; set; }
    public Guid Version { get; set; }
    public ConnectionEntityStatus Status { get; set; }
    public long TargetId { get; set; }
}

public enum ConnectionEntityStatus
{
    Pending,
    Connected
}
