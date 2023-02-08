using CecoChat.Contracts.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Repos;

public interface IProfileRepo
{
    Task<ProfileFull?> GetFullProfile(long requestedUserId);

    Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId);

    IQueryable<ProfilePublic> GetPublicProfiles(IEnumerable<long> requestedUserIds, long userId);
}

internal class ProfileRepo : IProfileRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public ProfileRepo(ILogger<ProfileRepo> logger, UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ProfileFull?> GetFullProfile(long requestedUserId)
    {
        ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserId == requestedUserId);
        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch full profile for user {UserId}", requestedUserId);
            return null;
        }

        _logger.LogTrace("Fetched full profile {ProfileUserName} for user {UserId}", entity.UserName, entity.UserId);

        // TODO: consider using AutoMapper
        return new ProfileFull
        {
            UserId = entity.UserId,
            UserName = entity.UserName,
            DisplayName = entity.DisplayName,
            AvatarUrl = entity.AvatarUrl,
            Phone = entity.Phone,
            Email = entity.Email
        };
    }

    public async Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId)
    {
        ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserId == requestedUserId);
        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch public profile for user {UserId}", requestedUserId);
            return null;
        }

        ProfilePublic profile = MapEntityToPublicProfile(entity);
        _logger.LogTrace("Fetched public profile {RequestedUserId} requested by {UserId}", requestedUserId, userId);

        return profile;
    }

    public IQueryable<ProfilePublic> GetPublicProfiles(IEnumerable<long> requestedUserIds, long userId)
    {
        _logger.LogTrace("Fetched (count here unknown) public profiles requested by user {UserId}", userId);

        return _dbContext.Profiles
            .Where(entity => requestedUserIds.Contains(entity.UserId))
            .Select(entity => MapEntityToPublicProfile(entity));
    }

    private static ProfilePublic MapEntityToPublicProfile(ProfileEntity entity)
    {
        return new ProfilePublic
        {
            UserId = entity.UserId,
            UserName = entity.UserName,
            DisplayName = entity.DisplayName,
            AvatarUrl = entity.AvatarUrl
        };
    }
}
