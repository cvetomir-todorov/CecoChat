using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CecoChat.Data.User;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    { }

    public DbSet<ProfileEntity> Profiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ProfileEntity>().HasKey(e => e.UserId);
        modelBuilder.Entity<ProfileEntity>().Property(e => e.UserId).ValueGeneratedOnAdd();

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class ProfileEntity
{
    public long UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
