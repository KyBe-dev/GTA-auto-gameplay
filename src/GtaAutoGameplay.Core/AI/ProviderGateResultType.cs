namespace GtaAutoGameplay.Core.AI;

public enum ProviderGateResultType
{
    Succeeded = 0,
    CloudDisabled,
    ConfigurationUnavailable,
    CredentialNotConfigured,
    CredentialInvalid,
    CredentialUnavailable,
    CredentialProviderMismatch,
    ProviderUnavailable,
    QuotaExhausted,
    RateLimited,
    TimedOut,
    Offline,
    Cancelled,
    InvalidProviderResult,
    ProviderFailure,
}
