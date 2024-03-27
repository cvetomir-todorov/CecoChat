using CecoChat.Data.User.Infra;
using CecoChat.User.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Entities.Profiles;

internal class ProfileCommandRepo : IProfileCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;
    private readonly IDataUtility _dataUtility;

    public ProfileCommandRepo(
        ILogger<ProfileCommandRepo> logger,
        UserDbContext dbContext,
        IDataUtility dataUtility)
    {
        _logger = logger;
        _dbContext = dbContext;
        _dataUtility = dataUtility;
    }

    public async Task<CreateProfileResult> CreateProfile(ProfileFull profile, string password)
    {
        if (profile.UserName.Any(char.IsUpper))
        {
            throw new ArgumentException("Profile user name should not contain upper-case letters.", nameof(profile));
        }
        if (profile.UserId != 0)
        {
            throw new ArgumentException("Profile user ID should not be set.", nameof(profile));
        }
        if (profile.Version != null)
        {
            throw new ArgumentException("Profile version should not be set.", nameof(profile));
        }

        ProfileEntity entity = new()
        {
            UserName = profile.UserName,
            Version = DateTime.UtcNow,
            Password = password,
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
                if (postgresException.MessageText.Contains("profiles_username_unique"))
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

    public async Task<ChangePasswordResult> ChangePassword(string newPassword, DateTime version, long userId)
    {
        ProfileEntity entity = new()
        {
            UserId = userId,
            Password = newPassword
        };
        EntityEntry<ProfileEntity> entry = _dbContext.Profiles.Attach(entity);
        entry.Property(e => e.Password).IsModified = true;
        DateTime newVersion = _dataUtility.SetVersion(entry, version);

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
        DateTime newVersion = _dataUtility.SetVersion(entry, profile.Version.ToDateTime());

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
}
