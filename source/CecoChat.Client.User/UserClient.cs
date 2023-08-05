using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

public interface IUserClient : IDisposable
{
    Task<CreateProfileResult> CreateProfile(ProfileCreate profile, CancellationToken ct);

    Task<ProfileFull> GetFullProfile(string accessToken, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);
}

public struct CreateProfileResult
{
    public bool Success { get; init; }

    public bool DuplicateUserName { get; init; }
}

internal sealed class UserClient : IUserClient
{
    private readonly ILogger _logger;
    private readonly UserOptions _options;
    private readonly ProfileQuery.ProfileQueryClient _profileQueryClient;
    private readonly ProfileCommand.ProfileCommandClient _profileCommandClient;

    public UserClient(
        ILogger<UserClient> logger,
        IOptions<UserOptions> options,
        ProfileQuery.ProfileQueryClient profileQueryClient,
        ProfileCommand.ProfileCommandClient profileCommandClient)
    {
        _logger = logger;
        _options = options.Value;
        _profileQueryClient = profileQueryClient;
        _profileCommandClient = profileCommandClient;
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<CreateProfileResult> CreateProfile(ProfileCreate profile, CancellationToken ct)
    {
        CreateProfileRequest request = new();
        request.Profile = profile;

        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        CreateProfileResponse response = await _profileCommandClient.CreateProfileAsync(request, deadline: deadline, cancellationToken: ct);

        if (response.Success)
        {
            _logger.LogTrace("Received confirmation for created profile for user {UserName}", profile.UserName);
            return new CreateProfileResult { Success = true };
        }
        if (response.DuplicateUserName)
        {
            _logger.LogTrace("Received failure for creating a profile for user {UserName} because of a duplicate user name", profile.UserName);
            return new CreateProfileResult { DuplicateUserName = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(CreateProfileResponse)}.");
    }

    public async Task<ProfileFull> GetFullProfile(string accessToken, CancellationToken ct)
    {
        GetFullProfileRequest request = new();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        GetFullProfileResponse response = await _profileQueryClient.GetFullProfileAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received full profile {ProfileUserName} for user {UserId}", response.Profile.UserName, response.Profile.UserId);
        return response.Profile;
    }

    public async Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct)
    {
        GetPublicProfileRequest request = new();
        request.UserId = requestedUserId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        GetPublicProfileResponse response = await _profileQueryClient.GetPublicProfileAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userId);
        return response.Profile;
    }

    public async Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct)
    {
        GetPublicProfilesRequest request = new();
        request.UserIds.Add(requestedUserIds);

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        GetPublicProfilesResponse response = await _profileQueryClient.GetPublicProfilesAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userId);
        return response.Profiles;
    }
}
