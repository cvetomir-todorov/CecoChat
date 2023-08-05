using CecoChat.Contracts.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Repos;

internal class ProfileCommandRepo : IProfileCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public ProfileCommandRepo(ILogger<ProfileCommandRepo> logger, UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<CreateProfileResult> CreateProfile(ProfileCreate profile)
    {
        if (profile.UserName.Any(char.IsUpper))
        {
            throw new ArgumentException("Profile user name should not contain upper-case letters.", nameof(profile));
        }

        ProfileEntity entity = new()
        {
            UserName = profile.UserName,
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
        catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException)
        {
            PostgresException postgresException = (PostgresException)dbUpdateException.InnerException;
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
}
