using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Profiles;

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

    public async Task<CreateProfileResult> CreateProfile(Registration registration)
    {
        if (registration.UserName.Any(char.IsUpper))
        {
            throw new ArgumentException("Profile user name should not contain upper-case letters.", nameof(registration));
        }

        ProfileEntity entity = new()
        {
            UserName = registration.UserName,
            Version = Guid.NewGuid(),
            Password = registration.Password,
            DisplayName = registration.DisplayName,
            AvatarUrl = registration.AvatarUrl,
            Phone = registration.Phone,
            Email = registration.Email
        };
        _dbContext.Profiles.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Inserted a new profile for user {UserName}", registration.UserName);

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

    public async Task<ChangePasswordResult> ChangePassword(string newPassword, Guid version, long userId)
    {
        ProfileEntity entity = new()
        {
            UserId = userId,
            Password = newPassword
        };
        EntityEntry<ProfileEntity> entry = _dbContext.Profiles.Attach(entity);
        entry.Property(e => e.Password).IsModified = true;
        Guid newVersion = _dataUtility.SetVersion(entry, version);

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
        Guid newVersion = _dataUtility.SetVersion(entry, profile.Version.ToGuid());

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