using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

internal sealed class ProfileClient : IProfileClient
{
    private readonly ILogger _logger;
    private readonly UserOptions _options;
    private readonly IClock _clock;
    private readonly ProfileQuery.ProfileQueryClient _profileQueryClient;
    private readonly ProfileCommand.ProfileCommandClient _profileCommandClient;

    public ProfileClient(
        ILogger<ProfileClient> logger,
        IOptions<UserOptions> options,
        IClock clock,
        ProfileQuery.ProfileQueryClient profileQueryClient,
        ProfileCommand.ProfileCommandClient profileCommandClient)
    {
        _logger = logger;
        _options = options.Value;
        _clock = clock;
        _profileQueryClient = profileQueryClient;
        _profileCommandClient = profileCommandClient;

        _logger.LogInformation("Profile client address set to {Address}", _options.Address);
    }

    public async Task<ChangePasswordResult> ChangePassword(string newPassword, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        ChangePasswordRequest request = new();
        request.NewPassword = newPassword;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        ChangePasswordResponse response = await _profileCommandClient.ChangePasswordAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received successful password change for user {UserId}", userId);
            return new ChangePasswordResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received failed password change for user {UserId} because of concurrently updated profile", userId);
            return new ChangePasswordResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(ChangePasswordResponse));
    }

    public async Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId, string accessToken, CancellationToken ct)
    {
        UpdateProfileRequest request = new();
        request.Profile = profile;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        UpdateProfileResponse response = await _profileCommandClient.UpdateProfileAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received successful profile update for user {UserId}", userId);
            return new UpdateProfileResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received failed profile update for user {UserId} because of concurrently updated profile", userId);
            return new UpdateProfileResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(UpdateProfileResponse));
    }

    public async Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct)
    {
        GetPublicProfileRequest request = new();
        request.UserId = requestedUserId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetPublicProfileResponse response = await _profileQueryClient.GetPublicProfileAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userId);
        return response.Profile;
    }

    public async Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct)
    {
        GetPublicProfilesRequest request = new();
        request.UserIds.Add(requestedUserIds);

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetPublicProfilesResponse response = await _profileQueryClient.GetPublicProfilesAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userId);
        return response.Profiles;
    }
}
