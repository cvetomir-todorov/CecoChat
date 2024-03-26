namespace Common;

/// <summary>
/// Thrown when a result object cannot be processed.
/// E.g. when a result object has multiple properties each of which represent a different case that needs to be handled:
///
/// if (result.Success) {
///   // handle it
/// }
/// if (result.Failure1) {
///   // handle it
/// }
/// if (result.Failure2) {
///   // handle it
/// }
///
/// throw new ProcessingFailureException(typeof(Result));
/// </summary>
public class ProcessingFailureException : Exception
{
    public ProcessingFailureException(Type type)
        : base($"Failed to process {nameof(type.FullName)}.")
    {
        Type = type;
    }

    public Type Type { get; }
}
