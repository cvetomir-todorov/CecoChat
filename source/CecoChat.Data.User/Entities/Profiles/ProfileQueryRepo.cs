using AutoMapper;
using CecoChat.User.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Entities.Profiles;

internal class ProfileQueryRepo : IProfileQueryRepo
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly UserDbContext _dbContext;

    public ProfileQueryRepo(
        ILogger<ProfileQueryRepo> logger,
        IMapper mapper,
        UserDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<FullProfileResult> GetFullProfile(string userName, bool includePassword)
    {
        if (userName.Any(char.IsUpper))
        {
            throw new ArgumentException("User name should not contain upper-case letters.", nameof(userName));
        }

        ProfileEntity? entity = await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.UserName == userName);

        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch profile for user {UserName}", userName);
            return new FullProfileResult
            {
                Success = false
            };
        }

        ProfileFull? profile = _mapper.Map<ProfileFull>(entity);
        string? password = includePassword ? entity.Password : null;

        _logger.LogTrace("Fetched profile for user {UserId} named {UserName}", entity.UserId, entity.UserName);
        return new FullProfileResult
        {
            Success = true,
            Profile = profile,
            Password = password
        };
    }

    public async Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId)
    {
        ProfileEntity? entity = await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.UserId == requestedUserId);
        if (entity == null)
        {
            return null;
        }

        ProfilePublic? profile = _mapper.Map<ProfilePublic>(entity);
        return profile;
    }

    public async Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId)
    {
        return await _dbContext.Profiles
            .Where(entity => requestedUserIds.Contains(entity.UserId))
            .Select(entity => _mapper.Map<ProfilePublic>(entity)!)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(string searchPattern, int profileCount, long userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        string likePattern = $"%{searchPattern}%";
        return await _dbContext.Profiles
            .Where(entity => EF.Functions.Like(entity.UserName, likePattern))
            .Take(profileCount)
            .Select(entity => _mapper.Map<ProfilePublic>(entity)!)
            .AsNoTracking()
            .ToListAsync();
    }
}
