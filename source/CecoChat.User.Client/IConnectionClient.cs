using CecoChat.User.Contracts;

namespace CecoChat.User.Client;

public interface IConnectionClient
{
    Task<IReadOnlyCollection<Connection>> GetConnections(long userId, string accessToken, CancellationToken ct);

    Task<Connection?> GetConnection(long userId, long connectionId, string accessToken, CancellationToken ct);

    Task<InviteResult> Invite(long connectionId, long userId, string accessToken, CancellationToken ct);

    Task<ApproveResult> Approve(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct);

    Task<CancelResult> Cancel(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct);

    Task<RemoveResult> Remove(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct);
}

public readonly struct InviteResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool MissingUser { get; init; }
    public bool AlreadyExists { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct ApproveResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct CancelResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct RemoveResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
