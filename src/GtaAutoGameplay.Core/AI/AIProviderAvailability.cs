namespace GtaAutoGameplay.Core.AI;

public enum AIProviderAvailability
{
    Disabled = 0,
    NotConfigured,
    Ready,
    InvalidCredential,
    RateLimited,
    QuotaExhausted,
    Offline,
    Unavailable,
}
