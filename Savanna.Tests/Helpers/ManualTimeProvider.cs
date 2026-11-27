namespace Savanna.Tests.Helpers;

public sealed class ManualTimeProvider : TimeProvider
{
    public DateTimeOffset Current { get; set; } = DateTimeOffset.UnixEpoch;

    /// Summary:
    /// Supplies a deterministic clock for session-expiry tests.
    public override DateTimeOffset GetUtcNow() => Current;
}
