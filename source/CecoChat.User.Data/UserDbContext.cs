using Microsoft.EntityFrameworkCore;

namespace CecoChat.User.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    { }

    public DbSet<ProfileEntity> Profiles { get; set; } = null!;

    public DbSet<ConnectionEntity> Connections { get; set; } = null!;

    public DbSet<FileEntity> Files { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        ConfigureProfiles(modelBuilder);
        ConfigureConnections(modelBuilder);
        ConfigureFiles(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProfileEntity>(entity =>
        {
            entity.ToTable("profiles");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.UserName).HasColumnName("username");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email");

            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).ValueGeneratedOnAdd();
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => e.UserName).IsUnique();
        });
    }

    private static void ConfigureConnections(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConnectionEntity>(entity =>
        {
            entity.ToTable("connections");

            entity.Property(e => e.User1Id).HasColumnName("user1_id");
            entity.Property(e => e.User2Id).HasColumnName("user2_id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TargetId).HasColumnName("target_id");

            entity.HasKey(nameof(ConnectionEntity.User1Id), nameof(ConnectionEntity.User2Id));
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => e.User1Id);
            entity.HasIndex(e => e.User2Id);
            entity.HasOne<ProfileEntity>().WithMany().HasForeignKey(e => e.User1Id);
            entity.HasOne<ProfileEntity>().WithMany().HasForeignKey(e => e.User2Id);
            entity.Property(e => e.Status)
                .HasConversion<string>(
                    status => status.ToString(),
                    statusString => (ConnectionEntityStatus)Enum.Parse(typeof(ConnectionEntityStatus), statusString));
        });
    }

    private static void ConfigureFiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.ToTable("files");

            entity.Property(e => e.Bucket).HasColumnName("bucket");
            entity.Property(e => e.Path).HasColumnName("path");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AllowedUsers).HasColumnName("allowed_users");

            entity.HasKey(nameof(FileEntity.Bucket), nameof(FileEntity.Path));
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(nameof(FileEntity.UserId), nameof(FileEntity.Version));
            entity.HasOne<ProfileEntity>().WithMany().HasForeignKey(e => e.UserId);
        });
    }
}

public interface IVersionEntity
{
    DateTime Version { get; set; }
}

public sealed class ProfileEntity : IVersionEntity
{
    public long UserId { get; set; }
    public DateTime Version { get; set; }
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
    public DateTime Version { get; set; }
    public ConnectionEntityStatus Status { get; set; }
    public long TargetId { get; set; }
}

public enum ConnectionEntityStatus
{
    NotConnected,
    Pending,
    Connected
}

public sealed class FileEntity : IVersionEntity
{
    public string Bucket { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime Version { get; set; }
    public long UserId { get; set; }
    public long[] AllowedUsers { get; set; } = Array.Empty<long>();
}
