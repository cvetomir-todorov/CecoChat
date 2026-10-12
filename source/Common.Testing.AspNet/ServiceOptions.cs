namespace Common.Testing.AspNet;

public sealed class ServiceOptions
{
    public static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(5);

    public required string Environment { get; init; }
    public required string ConfigFilePath { get; init; }
    public required int ListenPort { get; init; }
    public required string CertificatePath { get; init; }
    public required string CertificatePassword { get; init; }
    public TimeSpan StopTimeout { get; init; } = DefaultStopTimeout;
}
