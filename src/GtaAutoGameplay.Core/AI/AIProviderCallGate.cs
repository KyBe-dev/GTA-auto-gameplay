using GtaAutoGameplay.Core.Configuration;
using GtaAutoGameplay.Core.Credentials;
using GtaAutoGameplay.Core.Logging;

namespace GtaAutoGameplay.Core.AI;

public sealed class AIProviderCallGate
{
    private const string NotCheckedStatusCode = "NotChecked";

    private readonly IAIProvider _provider;
    private readonly IUserCredentialStore _credentialStore;
    private readonly IRuntimeConfigurationSource _configurationSource;
    private readonly IStructuredLogSink _logSink;
    private readonly TimeProvider _timeProvider;

    public AIProviderCallGate(
        IAIProvider provider,
        IUserCredentialStore credentialStore,
        IRuntimeConfigurationSource configurationSource,
        IStructuredLogSink logSink,
        TimeProvider? timeProvider = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _configurationSource = configurationSource
            ?? throw new ArgumentNullException(nameof(configurationSource));
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<ProviderGateOutcome> AnalyzeAsync(
        AIAnalysisRequest request,
        CredentialReference? credentialReference,
        LocalCapabilityAssessment localCapability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(localCapability))
        {
            throw new ArgumentOutOfRangeException(nameof(localCapability));
        }

        long startedAt = _timeProvider.GetTimestamp();

        if (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt);
        }

        RuntimeConfiguration configuration;
        try
        {
            configuration = _configurationSource.GetCurrent();
        }
        catch
        {
            return Complete(
                ProviderGateResultType.ConfigurationUnavailable,
                localCapability,
                startedAt);
        }

        if (configuration is null)
        {
            return Complete(
                ProviderGateResultType.ConfigurationUnavailable,
                localCapability,
                startedAt);
        }

        if (configuration.CloudProviderMode != CloudProviderMode.Enabled)
        {
            return Complete(
                ProviderGateResultType.CloudDisabled,
                localCapability,
                startedAt);
        }

        if (configuration.CredentialState != CredentialConfigurationState.Configured
            || credentialReference is null)
        {
            return Complete(
                ProviderGateResultType.CredentialNotConfigured,
                localCapability,
                startedAt);
        }

        string providerId;
        try
        {
            providerId = _provider.ProviderId;
        }
        catch
        {
            return Complete(
                ProviderGateResultType.ProviderUnavailable,
                localCapability,
                startedAt);
        }

        if (string.IsNullOrWhiteSpace(providerId)
            || !string.Equals(
                credentialReference.ProviderId,
                providerId,
                StringComparison.Ordinal))
        {
            return Complete(
                ProviderGateResultType.CredentialProviderMismatch,
                localCapability,
                startedAt);
        }

        CredentialStatus credentialStatus;
        try
        {
            credentialStatus = await _credentialStore
                .GetStatusAsync(credentialReference, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt);
        }
        catch
        {
            return Complete(
                ProviderGateResultType.CredentialUnavailable,
                localCapability,
                startedAt);
        }

        ProviderGateResultType? credentialFailure = credentialStatus switch
        {
            CredentialStatus.Available => null,
            CredentialStatus.NotConfigured => ProviderGateResultType.CredentialNotConfigured,
            CredentialStatus.Invalid => ProviderGateResultType.CredentialInvalid,
            CredentialStatus.Unavailable => ProviderGateResultType.CredentialUnavailable,
            _ => ProviderGateResultType.CredentialUnavailable,
        };

        if (credentialFailure is not null)
        {
            return Complete(credentialFailure.Value, localCapability, startedAt);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt);
        }

        AIProviderAvailability availability;
        try
        {
            availability = _provider.Availability;
        }
        catch
        {
            return Complete(
                ProviderGateResultType.ProviderUnavailable,
                localCapability,
                startedAt);
        }

        ProviderGateResultType? availabilityFailure = MapAvailability(availability);
        if (availabilityFailure is not null)
        {
            return Complete(
                availabilityFailure.Value,
                localCapability,
                startedAt,
                NormalizeProviderStatus(availability));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt,
                NormalizeProviderStatus(availability));
        }

        AIAnalysisResult? result;
        try
        {
            result = await _provider
                .AnalyzeAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt,
                availability);
        }
        catch (OperationCanceledException)
        {
            return Complete(
                ProviderGateResultType.TimedOut,
                localCapability,
                startedAt,
                availability);
        }
        catch (TimeoutException)
        {
            return Complete(
                ProviderGateResultType.TimedOut,
                localCapability,
                startedAt,
                availability);
        }
        catch
        {
            return Complete(
                ProviderGateResultType.ProviderFailure,
                localCapability,
                startedAt,
                availability);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Complete(
                ProviderGateResultType.Cancelled,
                localCapability,
                startedAt,
                availability);
        }

        if (result is null)
        {
            return Complete(
                ProviderGateResultType.InvalidProviderResult,
                localCapability,
                startedAt,
                availability);
        }

        ProviderGateResultType? returnedAvailabilityFailure = MapAvailability(result.Availability);
        if (returnedAvailabilityFailure is not null)
        {
            if (result.Candidates.Count > 0)
            {
                return Complete(
                    ProviderGateResultType.InvalidProviderResult,
                    localCapability,
                    startedAt,
                    result.Availability);
            }

            return Complete(
                returnedAvailabilityFailure.Value,
                localCapability,
                startedAt,
                result.Availability);
        }

        if (!IsValidSuccessResult(result))
        {
            return Complete(
                ProviderGateResultType.InvalidProviderResult,
                localCapability,
                startedAt,
                result.Availability);
        }

        return CompleteSuccess(result, startedAt);
    }

    private static ProviderGateResultType? MapAvailability(AIProviderAvailability availability) =>
        availability switch
        {
            AIProviderAvailability.Ready => null,
            AIProviderAvailability.NotConfigured => ProviderGateResultType.CredentialNotConfigured,
            AIProviderAvailability.InvalidCredential => ProviderGateResultType.CredentialInvalid,
            AIProviderAvailability.RateLimited => ProviderGateResultType.RateLimited,
            AIProviderAvailability.QuotaExhausted => ProviderGateResultType.QuotaExhausted,
            AIProviderAvailability.Offline => ProviderGateResultType.Offline,
            AIProviderAvailability.Disabled or AIProviderAvailability.Unavailable =>
                ProviderGateResultType.ProviderUnavailable,
            _ => ProviderGateResultType.ProviderUnavailable,
        };

    private static bool IsValidSuccessResult(AIAnalysisResult result)
    {
        if (result.Availability != AIProviderAvailability.Ready
            || !string.IsNullOrWhiteSpace(result.FailureCode)
            || result.Candidates.Count is < 1 or > AIProviderGateLimits.MaximumCandidates)
        {
            return false;
        }

        foreach (AIStateCandidate? candidate in result.Candidates)
        {
            if (candidate is null
                || candidate.TargetField.Length > AIProviderGateLimits.MaximumTargetFieldLength
                || !AIProviderGateLimits.IsAllowedTargetField(candidate.TargetField)
                || candidate.CandidateValue.Length > AIProviderGateLimits.MaximumCandidateValueLength
                || !double.IsFinite(candidate.Confidence)
                || candidate.Confidence is < 0d or > 1d)
            {
                return false;
            }
        }

        return true;
    }

    private ProviderGateOutcome CompleteSuccess(
        AIAnalysisResult result,
        long startedAt)
    {
        TimeSpan duration = GetDuration(startedAt);
        ProviderGateOutcome outcome = new(
            ProviderGateResultType.Succeeded,
            ProviderFallbackDirective.None,
            duration,
            AIProviderAvailability.Ready,
            result);
        WriteLog(outcome);
        return outcome;
    }

    private static AIProviderAvailability? NormalizeProviderStatus(
        AIProviderAvailability availability) =>
        Enum.IsDefined(availability) ? availability : null;

    private ProviderGateOutcome Complete(
        ProviderGateResultType resultType,
        LocalCapabilityAssessment localCapability,
        long startedAt,
        AIProviderAvailability? providerStatus = null)
    {
        TimeSpan duration = GetDuration(startedAt);
        ProviderGateOutcome outcome = new(
            resultType,
            GetFallbackDirective(resultType, localCapability),
            duration,
            providerStatus);
        WriteLog(outcome);
        return outcome;
    }

    private static ProviderFallbackDirective GetFallbackDirective(
        ProviderGateResultType resultType,
        LocalCapabilityAssessment localCapability)
    {
        if (resultType == ProviderGateResultType.Cancelled)
        {
            return ProviderFallbackDirective.PauseAutomaticControl;
        }

        if (localCapability == LocalCapabilityAssessment.SafeToContinue)
        {
            return ProviderFallbackDirective.ContinueLocally;
        }

        return resultType switch
        {
            ProviderGateResultType.CloudDisabled
                or ProviderGateResultType.CredentialNotConfigured
                or ProviderGateResultType.CredentialInvalid
                or ProviderGateResultType.CredentialUnavailable
                or ProviderGateResultType.CredentialProviderMismatch
                or ProviderGateResultType.ProviderUnavailable
                or ProviderGateResultType.QuotaExhausted
                or ProviderGateResultType.RateLimited
                or ProviderGateResultType.Offline =>
                    ProviderFallbackDirective.UserActionRequired,
            _ => ProviderFallbackDirective.PauseAutomaticControl,
        };
    }

    private TimeSpan GetDuration(long startedAt)
    {
        TimeSpan duration = _timeProvider.GetElapsedTime(startedAt);
        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    private void WriteLog(ProviderGateOutcome outcome)
    {
        string providerStatusCode = outcome.ProviderStatus?.ToString()
            ?? NotCheckedStatusCode;
        StructuredLogLevel level = outcome.ResultType == ProviderGateResultType.Succeeded
            ? StructuredLogLevel.Information
            : StructuredLogLevel.Warning;

        _logSink.Write(new StructuredLogEvent(
            "provider.gate.completed",
            _timeProvider.GetUtcNow(),
            level,
            StructuredLogCategory.Provider,
            [
                new(
                    StructuredLogFieldNames.ProviderStatusCode,
                    StructuredLogValue.FromString(providerStatusCode)),
                new(
                    StructuredLogFieldNames.ProviderResultType,
                    StructuredLogValue.FromString(outcome.ResultType.ToString())),
                new(
                    StructuredLogFieldNames.DurationMilliseconds,
                    StructuredLogValue.FromDouble(outcome.Duration.TotalMilliseconds)),
            ]));
    }
}
