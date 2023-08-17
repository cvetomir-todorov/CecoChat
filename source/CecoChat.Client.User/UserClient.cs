using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

internal sealed class UserClient : IUserClient
{
    private readonly ILogger _logger;
    private readonly UserOptions _options;
    private readonly ProfileQuery.ProfileQueryClient _profileQueryClient;
    private readonly ProfileCommand.ProfileCommandClient _profileCommandClient;
    private readonly ContactQuery.ContactQueryClient _contactQueryClient;
    private readonly ContactCommand.ContactCommandClient _contactCommandClient;
    private readonly IClock _clock;

    public UserClient(
        ILogger<UserClient> logger,
        IOptions<UserOptions> options,
        ProfileQuery.ProfileQueryClient profileQueryClient,
        ProfileCommand.ProfileCommandClient profileCommandClient,
        ContactQuery.ContactQueryClient contactQueryClient,
        ContactCommand.ContactCommandClient contactCommandClient,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _profileQueryClient = profileQueryClient;
        _profileCommandClient = profileCommandClient;
        _contactQueryClient = contactQueryClient;
        _contactCommandClient = contactCommandClient;
        _clock = clock;

        _logger.LogInformation("User address set to {Address}", _options.Address);
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<CreateProfileResult> CreateProfile(ProfileCreate profile, CancellationToken ct)
    {
        CreateProfileRequest request = new();
        request.Profile = profile;

        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
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

    public async Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct)
    {
        AuthenticateRequest request = new();
        request.UserName = userName;
        request.Password = password;

        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        AuthenticateResponse response = await _profileQueryClient.AuthenticateAsync(request, deadline: deadline, cancellationToken: ct);

        if (response.Missing)
        {
            _logger.LogTrace("Received response for missing profile for user {UserName}", userName);
            return new AuthenticateResult { Missing = true };
        }
        if (response.InvalidPassword)
        {
            _logger.LogTrace("Received response for invalid password attempted for user {UserName}", userName);
            return new AuthenticateResult { InvalidPassword = true };
        }
        if (response.Profile != null)
        {
            _logger.LogTrace("Received response for successful authentication and a full profile for user {UserId} named {UserName}",
                response.Profile.UserId, response.Profile.UserName);
            return new AuthenticateResult { Profile = response.Profile };
        }

        throw new InvalidOperationException($"Failed to process {nameof(AuthenticateResponse)}.");
    }

    public async Task<ChangePasswordResult> ChangePassword(ProfileChangePassword profile, long userId, string accessToken, CancellationToken ct)
    {
        ChangePasswordRequest request = new();
        request.Profile = profile;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        ChangePasswordResponse response = await _profileCommandClient.ChangePasswordAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received response for successful password change for user {UserId}", userId);
            return new ChangePasswordResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received response for concurrently updated profile for user {UserId}", userId);
            return new ChangePasswordResult { ConcurrentlyUpdated = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(ChangePasswordResponse)}.");
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
            _logger.LogTrace("Received response for successful profile update for user {UserId}", userId);
            return new UpdateProfileResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received response for concurrently updated profile for user {UserId}", userId);
            return new UpdateProfileResult { ConcurrentlyUpdated = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(UpdateProfileResponse)}.");
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

    public async Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct)
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

    public async Task<IEnumerable<Contact>> GetContacts(long userId, string accessToken, CancellationToken ct)
    {
        GetContactsRequest request = new();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetContactsResponse response = await _contactQueryClient.GetContactsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ContactCount} contacts for user {UserId}", response.Contacts.Count, userId);
        return response.Contacts;
    }

    public async Task<InviteContactResult> InviteContact(long contactUserId, long userId, string accessToken, CancellationToken ct)
    {
        InviteRequest request = new();
        request.ContactUserId = contactUserId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        InviteResponse response = await _contactCommandClient.InviteAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received successful sending for contact request from user {UserId} to contact {ContactUserId}", userId, contactUserId);
            return new InviteContactResult
            {
                Success = true,
                Version = response.Version.ToGuid()
            };
        }
        if (response.AlreadyExists)
        {
            _logger.LogTrace("Received failed sending for contact request from user {UserId} to contact {ContactUserId}", userId, contactUserId);
            return new InviteContactResult
            {
                AlreadyExists = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(InviteResponse)}.");
    }
    
    // TODO: add logging

    public async Task<ApproveContactResult> ApproveContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        ApproveRequest request = new();
        request.ContactUserId = contactUserId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        ApproveResponse response = await _contactCommandClient.ApproveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new ApproveContactResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.MissingContact)
        {
            return new ApproveContactResult
            {
                MissingContact = true
            };
        }
        if (response.Invalid)
        {
            return new ApproveContactResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new ApproveContactResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(ApproveResponse)}.");
    }

    public async Task<CancelContactResult> CancelContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        CancelRequest request = new();
        request.ContactUserId = contactUserId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        CancelResponse response = await _contactCommandClient.CancelAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new CancelContactResult
            {
                Success = true
            };
        }
        if (response.MissingContact)
        {
            return new CancelContactResult
            {
                MissingContact = true
            };
        }
        if (response.Invalid)
        {
            return new CancelContactResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new CancelContactResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {typeof(CancelResponse)}.");
    }

    public async Task<RemoveContactResult> RemoveContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        RemoveRequest request = new();
        request.ContactUserId = contactUserId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        RemoveResponse response = await _contactCommandClient.RemoveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new RemoveContactResult
            {
                Success = true
            };
        }
        if (response.MissingContact)
        {
            return new RemoveContactResult
            {
                MissingContact = true
            };
        }
        if (response.Invalid)
        {
            return new RemoveContactResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new RemoveContactResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {typeof(RemoveResponse)}.");
    }
}
