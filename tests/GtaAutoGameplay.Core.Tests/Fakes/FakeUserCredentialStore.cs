using System.Collections.Concurrent;
using GtaAutoGameplay.Core.Credentials;

namespace GtaAutoGameplay.Core.Tests.Fakes;

internal sealed class FakeUserCredentialStore : IUserCredentialStore
{
    private readonly ConcurrentDictionary<CredentialReference, CredentialStatus> _statuses = new();
    private int _statusCallCount;

    public CredentialStatus DefaultStatus { get; set; } = CredentialStatus.NotConfigured;

    public Exception? StatusException { get; set; }

    public int StatusCallCount => Volatile.Read(ref _statusCallCount);

    public void SetStatus(CredentialReference reference, CredentialStatus status)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _statuses[reference] = status;
    }

    public ValueTask<CredentialStatus> GetStatusAsync(
        CredentialReference reference,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _statusCallCount);
        cancellationToken.ThrowIfCancellationRequested();

        if (StatusException is not null)
        {
            return ValueTask.FromException<CredentialStatus>(StatusException);
        }

        return ValueTask.FromResult(
            _statuses.TryGetValue(reference, out CredentialStatus status)
                ? status
                : DefaultStatus);
    }

    public ValueTask StoreAsync(
        CredentialReference reference,
        ReadOnlyMemory<char> secret,
        CancellationToken cancellationToken) =>
        ValueTask.FromException(new NotSupportedException(
            "The M0 fake stores credential status only and never accepts secret content."));

    public ValueTask<TResult> UseAsync<TResult>(
        CredentialReference reference,
        Func<ReadOnlyMemory<char>, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<TResult>(new NotSupportedException(
            "The M0 fake never exposes secret content."));

    public ValueTask DeleteAsync(
        CredentialReference reference,
        CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}
