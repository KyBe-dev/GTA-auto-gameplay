namespace GtaAutoGameplay.Core.Credentials;

public interface IUserCredentialStore
{
    ValueTask<CredentialStatus> GetStatusAsync(
        CredentialReference reference,
        CancellationToken cancellationToken);

    ValueTask StoreAsync(
        CredentialReference reference,
        ReadOnlyMemory<char> secret,
        CancellationToken cancellationToken);

    ValueTask<TResult> UseAsync<TResult>(
        CredentialReference reference,
        Func<ReadOnlyMemory<char>, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken);

    ValueTask DeleteAsync(
        CredentialReference reference,
        CancellationToken cancellationToken);
}
