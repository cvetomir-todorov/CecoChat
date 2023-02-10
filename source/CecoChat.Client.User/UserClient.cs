using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

public interface IUserClient : IDisposable
{
    Task<ProfileFull> GetFullProfile(string accessToken, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);
}

internal sealed class UserClient : IUserClient
{
    private readonly ILogger _logger;
    private readonly UserOptions _options;
    private readonly Profile.ProfileClient _profileClient;

    public UserClient(
        ILogger<UserClient> logger,
        IOptions<UserOptions> options,
        Profile.ProfileClient profileClient)
    {
        _logger = logger;
        _options = options.Value;
        _profileClient = profileClient;
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<ProfileFull> GetFullProfile(string accessToken, CancellationToken ct)
    {
        GetFullProfileRequest request = new();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        GetFullProfileResponse response = await _profileClient.GetFullProfileAsync(request, headers, deadline, ct);

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
        GetPublicProfileResponse response = await _profileClient.GetPublicProfileAsync(request, headers, deadline, ct);

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
        GetPublicProfilesResponse response = await _profileClient.GetPublicProfilesAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userId);
        return response.Profiles;
    }
}
