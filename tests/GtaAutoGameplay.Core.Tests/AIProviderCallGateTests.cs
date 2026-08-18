using GtaAutoGameplay.Core.AI;
using GtaAutoGameplay.Core.Configuration;
using GtaAutoGameplay.Core.Credentials;
using GtaAutoGameplay.Core.Domain;
using GtaAutoGameplay.Core.Input;
using GtaAutoGameplay.Core.Logging;
using GtaAutoGameplay.Core.Safety;
using GtaAutoGameplay.Core.Tests.Fakes;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class AIProviderCallGateTests
{
    private static readonly CredentialReference UserCredentialReference =
        new("fake-provider", "terminal-user-credential-reference");

    [TestMethod]
    public async Task SafeDefaultConfiguration_NeverCallsProvider()
    {
        GateFixture fixture = CreateFixture(RuntimeConfiguration.SafeDefault);

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            credentialReference: null,
            LocalCapabilityAssessment.SafeToContinue);

        Assert.AreEqual(ProviderGateResultType.CloudDisabled, outcome.ResultType);
        Assert.AreEqual(ProviderFallbackDirective.ContinueLocally, outcome.FallbackDirective);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
        Assert.AreEqual(0, fixture.Credentials.StatusCallCount);
    }

    [TestMethod]
    public async Task CloudDisabled_DoesNotCallProviderEvenWhenCredentialExists()
    {
        GateFixture fixture = CreateFixture(RuntimeConfiguration.SafeDefault);
        fixture.Credentials.SetStatus(UserCredentialReference, CredentialStatus.Available);

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.CloudDisabled, outcome.ResultType);
        Assert.AreEqual(ProviderFallbackDirective.UserActionRequired, outcome.FallbackDirective);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
        Assert.AreEqual(0, fixture.Credentials.StatusCallCount);
    }

    [TestMethod]
    public async Task MissingCredentialReference_DoesNotCallCredentialStoreOrProvider()
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            credentialReference: null,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.CredentialNotConfigured, outcome.ResultType);
        Assert.AreEqual(0, fixture.Credentials.StatusCallCount);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    [DataRow(CredentialStatus.NotConfigured, ProviderGateResultType.CredentialNotConfigured)]
    [DataRow(CredentialStatus.Invalid, ProviderGateResultType.CredentialInvalid)]
    [DataRow(CredentialStatus.Unavailable, ProviderGateResultType.CredentialUnavailable)]
    public async Task UnusableCredentialStatus_DoesNotCallProvider(
        CredentialStatus credentialStatus,
        ProviderGateResultType expectedResult)
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());
        fixture.Credentials.SetStatus(UserCredentialReference, credentialStatus);

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(expectedResult, outcome.ResultType);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task CredentialStoreFailure_IsCredentialUnavailableAndDoesNotCallProvider()
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());
        fixture.Credentials.StatusException = new InvalidOperationException(
            "Synthetic credential status failure.");

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.CredentialUnavailable, outcome.ResultType);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task CredentialForDifferentProvider_IsRejectedBeforeStoreLookup()
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());
        CredentialReference mismatched = new("different-provider", "terminal-user-reference");

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            mismatched,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.CredentialProviderMismatch, outcome.ResultType);
        Assert.AreEqual(0, fixture.Credentials.StatusCallCount);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    [DataRow(AIProviderAvailability.Disabled, ProviderGateResultType.ProviderUnavailable)]
    [DataRow(AIProviderAvailability.Unavailable, ProviderGateResultType.ProviderUnavailable)]
    [DataRow(AIProviderAvailability.NotConfigured, ProviderGateResultType.CredentialNotConfigured)]
    [DataRow(AIProviderAvailability.InvalidCredential, ProviderGateResultType.CredentialInvalid)]
    [DataRow(AIProviderAvailability.QuotaExhausted, ProviderGateResultType.QuotaExhausted)]
    [DataRow(AIProviderAvailability.RateLimited, ProviderGateResultType.RateLimited)]
    [DataRow(AIProviderAvailability.Offline, ProviderGateResultType.Offline)]
    public async Task ProviderNotReady_IsDistinguishedWithoutSendingRequest(
        AIProviderAvailability availability,
        ProviderGateResultType expectedResult)
    {
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.CurrentAvailability = availability;

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(expectedResult, outcome.ResultType);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task AllConditionsSatisfied_CallsProviderExactlyOnce()
    {
        GateFixture fixture = CreateReadyFixture();

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.Succeeded, outcome.ResultType);
        Assert.AreEqual(ProviderFallbackDirective.None, outcome.FallbackDirective);
        Assert.IsNotNull(outcome.AnalysisResult);
        Assert.AreEqual(1, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task AlreadyCancelledRequest_DoesNotReadConfigurationOrCallProvider()
    {
        GateFixture fixture = CreateReadyFixture();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.SafeToContinue,
            cancellation.Token);

        Assert.AreEqual(ProviderGateResultType.Cancelled, outcome.ResultType);
        Assert.AreEqual(
            ProviderFallbackDirective.PauseAutomaticControl,
            outcome.FallbackDirective);
        Assert.AreEqual(0, fixture.Credentials.StatusCallCount);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task CancellationAfterProviderStarts_ReturnsCancelledOutcome()
    {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.AnalyzeHandler = async (_, cancellationToken) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return FakeAIProvider.CreateValidResult();
        };
        using CancellationTokenSource cancellation = new();

        Task<ProviderGateOutcome> running = fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue,
            cancellation.Token).AsTask();
        await started.Task.ConfigureAwait(false);
        cancellation.Cancel();
        ProviderGateOutcome outcome = await running.ConfigureAwait(false);

        Assert.AreEqual(ProviderGateResultType.Cancelled, outcome.ResultType);
        Assert.AreEqual(1, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task ProviderException_IsSanitizedIntoExplicitFailure()
    {
        string sensitiveDetail = "synthetic-sensitive-detail-" + new string('Z', 32);
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.AnalyzeHandler = (_, _) =>
            ValueTask.FromException<AIAnalysisResult>(
                new InvalidOperationException(sensitiveDetail));

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.ProviderFailure, outcome.ResultType);
        Assert.AreEqual(
            ProviderFallbackDirective.PauseAutomaticControl,
            outcome.FallbackDirective);
        Assert.IsNull(outcome.AnalysisResult);
        Assert.IsFalse(LogText(fixture.Log).Contains(sensitiveDetail, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ProviderTimeout_IsDistinctFromOtherFailures()
    {
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.AnalyzeHandler = (_, _) =>
            ValueTask.FromException<AIAnalysisResult>(new TimeoutException());

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.TimedOut, outcome.ResultType);
        Assert.AreEqual(1, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task ProviderCancellationWithoutCallerCancellation_IsTreatedAsTimeout()
    {
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.AnalyzeHandler = (_, _) =>
            ValueTask.FromException<AIAnalysisResult>(new OperationCanceledException());

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.TimedOut, outcome.ResultType);
        Assert.AreEqual(1, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task ProviderMetadataFailure_DefaultRejectsWithoutSendingRequest()
    {
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.ProviderIdHandler = () =>
            throw new InvalidOperationException("Synthetic provider metadata failure.");

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.ProviderUnavailable, outcome.ResultType);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task UnknownProviderAvailability_DefaultRejectsWithoutSendingRequest()
    {
        GateFixture fixture = CreateReadyFixture();
        fixture.Provider.CurrentAvailability = (AIProviderAvailability)999;

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.ProviderUnavailable, outcome.ResultType);
        Assert.IsNull(outcome.ProviderStatus);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task ReturnedAvailabilityFailures_AreDistinct()
    {
        AIProviderAvailability[] statuses =
        [
            AIProviderAvailability.QuotaExhausted,
            AIProviderAvailability.RateLimited,
            AIProviderAvailability.Offline,
        ];
        ProviderGateResultType[] expected =
        [
            ProviderGateResultType.QuotaExhausted,
            ProviderGateResultType.RateLimited,
            ProviderGateResultType.Offline,
        ];

        for (int index = 0; index < statuses.Length; index++)
        {
            GateFixture fixture = CreateReadyFixture();
            AIProviderAvailability returnedStatus = statuses[index];
            fixture.Provider.AnalyzeHandler = (_, _) => ValueTask.FromResult(
                new AIAnalysisResult(returnedStatus));

            ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
                CreateRequest(),
                UserCredentialReference,
                LocalCapabilityAssessment.CannotSafelyContinue);

            Assert.AreEqual(expected[index], outcome.ResultType);
            Assert.AreEqual(1, fixture.Provider.AnalyzeCallCount);
        }
    }

    [TestMethod]
    public async Task InvalidOrOutOfBoundsProviderResults_AreRejected()
    {
        AIAnalysisResult?[] invalidResults =
        [
            null,
            new AIAnalysisResult(AIProviderAvailability.Ready),
            new AIAnalysisResult(
                AIProviderAvailability.Ready,
                [new AIStateCandidate("unreviewedField", "value", 0.5d)]),
            new AIAnalysisResult(
                AIProviderAvailability.Ready,
                [
                    new AIStateCandidate(
                        "gameMode",
                        new string('V', AIProviderGateLimits.MaximumCandidateValueLength + 1),
                        0.5d),
                ]),
            new AIAnalysisResult(
                AIProviderAvailability.Ready,
                Enumerable.Range(0, AIProviderGateLimits.MaximumCandidates + 1)
                    .Select(_ => new AIStateCandidate("gameMode", "Unknown", 0.5d))),
            new AIAnalysisResult(
                AIProviderAvailability.RateLimited,
                [new AIStateCandidate("gameMode", "Unknown", 0.5d)]),
        ];

        foreach (AIAnalysisResult? invalidResult in invalidResults)
        {
            GateFixture fixture = CreateReadyFixture();
            fixture.Provider.AnalyzeHandler = (_, _) =>
                ValueTask.FromResult(invalidResult!);

            ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
                CreateRequest(),
                UserCredentialReference,
                LocalCapabilityAssessment.CannotSafelyContinue);

            Assert.AreEqual(ProviderGateResultType.InvalidProviderResult, outcome.ResultType);
            Assert.IsNull(outcome.AnalysisResult);
        }
    }

    [TestMethod]
    public async Task ProviderGate_HasNoInputDependencyAndCannotSendActions()
    {
        GateFixture fixture = CreateReadyFixture();
        FakeInputController input = new();

        ProviderGateOutcome outcome = await fixture.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(ProviderGateResultType.Succeeded, outcome.ResultType);
        Assert.IsEmpty(input.ExecutedActions);
        Assert.IsEmpty(input.PressedActions);
        Assert.IsFalse(typeof(AIProviderCallGate)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(IInputController)));
        Assert.IsFalse(typeof(AIProviderCallGate)
            .GetMethods()
            .SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(SemanticAction)));
    }

    [TestMethod]
    public async Task ProviderSuccessAndFailure_DoNotClearEmergencyStopLatch()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeControlState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        await coordinator.EmergencyStopAsync();

        GateFixture success = CreateReadyFixture();
        _ = await success.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);
        GateFixture failure = CreateReadyFixture();
        failure.Provider.AnalyzeHandler = (_, _) =>
            ValueTask.FromException<AIAnalysisResult>(new InvalidOperationException());
        _ = await failure.Gate.AnalyzeAsync(
            CreateRequest(),
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.AreEqual(ControlStopReason.EmergencyStop, coordinator.StopReason);
        Assert.IsEmpty(input.ExecutedActions);
    }

    [TestMethod]
    public async Task ProviderLog_ContainsOnlyWhitelistedStatusResultAndDuration()
    {
        GateFixture fixture = CreateReadyFixture();
        AIAnalysisRequest request = CreateRequest("private-purpose-not-for-log");

        _ = await fixture.Gate.AnalyzeAsync(
            request,
            UserCredentialReference,
            LocalCapabilityAssessment.CannotSafelyContinue);

        IReadOnlyList<StructuredLogEvent> snapshot = fixture.Log.GetSnapshot();
        Assert.HasCount(1, snapshot);
        StructuredLogEvent logEvent = snapshot[0];
        Assert.AreEqual(StructuredLogCategory.Provider, logEvent.Category);
        CollectionAssert.AreEquivalent(
            new[]
            {
                StructuredLogFieldNames.ProviderStatusCode,
                StructuredLogFieldNames.ProviderResultType,
                StructuredLogFieldNames.DurationMilliseconds,
            },
            logEvent.Fields.Keys.ToArray());
        string text = LogText(fixture.Log);
        Assert.IsFalse(text.Contains(UserCredentialReference.CredentialName, StringComparison.Ordinal));
        Assert.IsFalse(text.Contains(request.Purpose, StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Unknown", StringComparison.Ordinal));

        Assert.ThrowsExactly<ArgumentException>(() => new StructuredLogEvent(
            "provider.forbidden-field",
            DateTimeOffset.UtcNow,
            StructuredLogLevel.Warning,
            StructuredLogCategory.Provider,
            [new("rawProviderResponse", StructuredLogValue.FromString("forbidden"))]));
    }

    [TestMethod]
    public async Task ConcurrentRequests_CannotBypassCredentialCheck()
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());
        fixture.Credentials.SetStatus(
            UserCredentialReference,
            CredentialStatus.Unavailable);

        Task<ProviderGateOutcome>[] requests = Enumerable.Range(0, 32)
            .Select(_ => fixture.Gate.AnalyzeAsync(
                CreateRequest(),
                UserCredentialReference,
                LocalCapabilityAssessment.CannotSafelyContinue).AsTask())
            .ToArray();
        ProviderGateOutcome[] outcomes = await Task.WhenAll(requests);

        Assert.IsTrue(outcomes.All(outcome =>
            outcome.ResultType == ProviderGateResultType.CredentialUnavailable));
        Assert.AreEqual(32, fixture.Credentials.StatusCallCount);
        Assert.AreEqual(0, fixture.Provider.AnalyzeCallCount);
    }

    [TestMethod]
    public async Task ContinueLocally_IsOnlySuggestedWhenLocalCapabilityIsSafe()
    {
        GateFixture safe = CreateFixture(RuntimeConfiguration.SafeDefault);
        ProviderGateOutcome safeOutcome = await safe.Gate.AnalyzeAsync(
            CreateRequest(),
            credentialReference: null,
            LocalCapabilityAssessment.SafeToContinue);
        GateFixture unsafeFixture = CreateFixture(RuntimeConfiguration.SafeDefault);
        ProviderGateOutcome unsafeOutcome = await unsafeFixture.Gate.AnalyzeAsync(
            CreateRequest(),
            credentialReference: null,
            LocalCapabilityAssessment.CannotSafelyContinue);

        Assert.AreEqual(
            ProviderFallbackDirective.ContinueLocally,
            safeOutcome.FallbackDirective);
        Assert.AreNotEqual(
            ProviderFallbackDirective.ContinueLocally,
            unsafeOutcome.FallbackDirective);
    }

    private static GateFixture CreateReadyFixture()
    {
        GateFixture fixture = CreateFixture(CreateEnabledConfiguration());
        fixture.Credentials.SetStatus(UserCredentialReference, CredentialStatus.Available);
        return fixture;
    }

    private static GateFixture CreateFixture(RuntimeConfiguration configuration)
    {
        FakeAIProvider provider = new();
        FakeUserCredentialStore credentials = new();
        InMemoryRuntimeConfigurationSource configurationSource = new(configuration);
        InMemoryStructuredLog log = new(configuration.Logging);
        AIProviderCallGate gate = new(provider, credentials, configurationSource, log);
        return new GateFixture(provider, credentials, log, gate);
    }

    private static RuntimeConfiguration CreateEnabledConfiguration() =>
        new(
            CloudProviderMode.Enabled,
            CredentialConfigurationState.Configured,
            CaptureDataOptions.Disabled,
            StructuredLogOptions.SafeDefault);

    private static AIAnalysisRequest CreateRequest(
        string purpose = "structured-state-analysis") =>
        new(
            Guid.NewGuid(),
            purpose,
            new GameState(DateTimeOffset.UtcNow));

    private static string LogText(InMemoryStructuredLog log) =>
        string.Join(
            "|",
            log.GetSnapshot().SelectMany(logEvent => logEvent.Fields.Values)
                .Select(value => value.StringValue
                    ?? value.Int64Value?.ToString()
                    ?? value.DoubleValue?.ToString()
                    ?? value.BooleanValue?.ToString()
                    ?? string.Empty));

    private static ControlSafetyState CreateSafeControlState() =>
        new(
            captureTargetId: "capture-target",
            inputTargetId: "input-target",
            captureWindowIdentity: "window-1",
            captureProcessIdentity: "process-1",
            inputWindowIdentity: "window-1",
            inputProcessIdentity: "process-1",
            isInputTargetForeground: true,
            isCaptureHealthy: true,
            isStateFresh: true);

    private sealed record GateFixture(
        FakeAIProvider Provider,
        FakeUserCredentialStore Credentials,
        InMemoryStructuredLog Log,
        AIProviderCallGate Gate);
}
