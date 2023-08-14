using Microsoft.EntityFrameworkCore;

namespace CecoChat.Data.User;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    { }

    public DbSet<ProfileEntity> Profiles { get; set; } = null!;

    public DbSet<ContactEntity> Contacts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ProfileEntity>().HasKey(e => e.UserId);
        modelBuilder.Entity<ProfileEntity>().Property(e => e.UserId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ProfileEntity>().Property(e => e.Version).IsConcurrencyToken();

        modelBuilder.Entity<ContactEntity>().HasKey(nameof(ContactEntity.User1Id), nameof(ContactEntity.User2Id));
        modelBuilder.Entity<ContactEntity>().Property(e => e.Status)
            .HasDefaultValue(ContactEntityStatus.PendingRequest)
            .HasConversion<string>();
        modelBuilder.Entity<ContactEntity>().Property(e => e.Version).IsConcurrencyToken();

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class ProfileEntity
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

public sealed class ContactEntity
{
    public long User1Id { get; set; }
    public long User2Id { get; set; }
    public Guid Version { get; set; }
    public ContactEntityStatus Status { get; set; }
}

public enum ContactEntityStatus
{
    PendingRequest,
    CancelledRequest,
    Established,
    Removed
}
