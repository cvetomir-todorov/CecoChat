using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IConnectionClient
{
    Task<IEnumerable<Connection>> GetConnections(long userId, string accessToken, CancellationToken ct);

    Task<InviteResult> Invite(long connectionId, long userId, string accessToken, CancellationToken ct);

    Task<ApproveResult> Approve(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<CancelResult> Cancel(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<RemoveResult> Remove(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);
}

public readonly struct InviteResult
{
    public bool Success { get; init; }
    public Guid Version { get; init; }
    public bool AlreadyExists { get; init; }
}

public readonly struct ApproveResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct CancelResult
{
    public bool Success { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct RemoveResult
{
    public bool Success { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
