using AutoMapper;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Repos;

internal class ProfileQueryRepo : IProfileQueryRepo
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly UserDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileQueryRepo(
        ILogger<ProfileQueryRepo> logger,
        IMapper mapper,
        UserDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticateResult> Authenticate(string userName, string password)
    {
        if (userName.Any(char.IsUpper))
        {
            throw new ArgumentException("User name should not contain upper-case letters.", nameof(userName));
        }

        ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserName == userName);
        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch full profile for user {UserName}", userName);
            return new AuthenticateResult { Missing = true };
        }

        bool areEqual = _passwordHasher.Verify(password, entity.Password);
        if (!areEqual)
        {
            _logger.LogTrace("Fetched profile for user {UserId} named {UserName} doesn't have the attempted password", entity.UserId, entity.UserName);
            return new AuthenticateResult { InvalidPassword = true };
        }

        _logger.LogTrace("Fetched profile for user {UserId} named {UserName} with expected password", entity.UserId, entity.UserName);
        ProfileFull profile = _mapper.Map<ProfileFull>(entity);
        return new AuthenticateResult { Profile = profile };
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

        ProfileFull profile = _mapper.Map<ProfileFull>(entity);
        return profile;
    }

    public async Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId)
    {
        ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserId == requestedUserId);
        if (entity == null)
        {
            return null;
        }

        ProfilePublic profile = _mapper.Map<ProfilePublic>(entity);
        return profile;
    }

    public async Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId)
    {
        return await _dbContext.Profiles
            .Where(entity => requestedUserIds.Contains(entity.UserId))
            .Select(entity => _mapper.Map<ProfilePublic>(entity))
            .ToListAsync();
    }
}
