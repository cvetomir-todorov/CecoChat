using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Profiles;

internal class ProfileCommandRepo : IProfileCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileCommandRepo(ILogger<ProfileCommandRepo> logger, UserDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateProfileResult> CreateProfile(ProfileCreate profile)
    {
        if (profile.UserName.Any(char.IsUpper))
        {
            throw new ArgumentException("Profile user name should not contain upper-case letters.", nameof(profile));
        }

        string passwordHashAndSalt = _passwordHasher.Hash(profile.Password);

        ProfileEntity entity = new()
        {
            UserName = profile.UserName,
            Version = Guid.NewGuid(),
            Password = passwordHashAndSalt,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            Phone = profile.Phone,
            Email = profile.Email
        };
        _dbContext.Profiles.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Inserted a new profile for user {UserName}", profile.UserName);

            return new CreateProfileResult
            {
                Success = true
            };
        }
        catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException postgresException)
        {
            // https://www.postgresql.org/docs/current/errcodes-appendix.html
            if (postgresException.SqlState == "23505")
            {
                if (postgresException.MessageText.Contains("Profiles_UserName_unique"))
                {
                    return new CreateProfileResult
                    {
                        DuplicateUserName = true
                    };
                }
            }

            throw;
        }
    }

    public async Task<ChangePasswordResult> ChangePassword(ProfileChangePassword profile, long userId)
    {
        string passwordHashAndSalt = _passwordHasher.Hash(profile.NewPassword);

        ProfileEntity entity = new()
        {
            UserId = userId,
            Password = passwordHashAndSalt
        };
        EntityEntry<ProfileEntity> entry = _dbContext.Profiles.Attach(entity);
        entry.Property(e => e.Password).IsModified = true;
        Guid newVersion = SetVersion(entry, profile.Version.ToGuid());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Updated password for user {UserId}", userId);

            return new ChangePasswordResult
            {
                Success = true,
                NewVersion = newVersion
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new ChangePasswordResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    public async Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId)
    {
        ProfileEntity entity = new()
        {
            UserId = userId,
            DisplayName = profile.DisplayName
        };
        EntityEntry<ProfileEntity> entry = _dbContext.Profiles.Attach(entity);
        entry.Property(e => e.DisplayName).IsModified = true;
        Guid newVersion = SetVersion(entry, profile.Version.ToGuid());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Updated existing profile for user {UserId}", userId);

            return new UpdateProfileResult
            {
                Success = true,
                NewVersion = newVersion
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateProfileResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    private static Guid SetVersion(EntityEntry<ProfileEntity> entity, Guid originalVersion)
    {
        PropertyEntry<ProfileEntity, Guid> property = entity.Property(e => e.Version);
        property.OriginalValue = originalVersion;
        property.CurrentValue = Guid.NewGuid();

        return property.CurrentValue;
    }
}
