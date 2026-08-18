using GtaAutoGameplay.Core.Configuration;
using GtaAutoGameplay.Core.Input;
using GtaAutoGameplay.Core.Logging;
using GtaAutoGameplay.Core.Safety;
using GtaAutoGameplay.Core.Tests.Fakes;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class RuntimeConfigurationTests
{
    [TestMethod]
    public void SafeDefault_IsLocalWithCloudAndCredentialsDisabled()
    {
        RuntimeConfiguration configuration = RuntimeConfiguration.SafeDefault;

        Assert.AreEqual(CloudProviderMode.Disabled, configuration.CloudProviderMode);
        Assert.AreEqual(
            CredentialConfigurationState.NotConfigured,
            configuration.CredentialState);
        Assert.IsTrue(configuration.Logging.UsesFieldWhitelist);
        Assert.AreEqual(
            StructuredLogOptions.DefaultCapacity,
            configuration.Logging.Capacity);
        Assert.AreEqual(
            StructuredLogOptions.DefaultRetentionPeriod,
            configuration.Logging.RetentionPeriod);
    }

    [TestMethod]
    public void SafeDefault_DoesNotStoreScreenshotsRecordingsOrFrames()
    {
        CaptureDataOptions captureData = RuntimeConfiguration.SafeDefault.CaptureData;

        Assert.IsFalse(captureData.SaveScreenshots);
        Assert.IsFalse(captureData.SaveRecordings);
        Assert.IsFalse(captureData.EnableFrameReplay);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(100_001)]
    public void InvalidLogCapacity_IsRejected(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new StructuredLogOptions(capacity, TimeSpan.FromHours(1)));
    }

    [TestMethod]
    public void InvalidLogRetention_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new StructuredLogOptions(10, TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new StructuredLogOptions(10, TimeSpan.FromDays(31)));
    }

    [TestMethod]
    public void UnknownConfigurationEnums_AreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RuntimeConfiguration(
            (CloudProviderMode)999,
            CredentialConfigurationState.NotConfigured,
            CaptureDataOptions.Disabled,
            StructuredLogOptions.SafeDefault));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RuntimeConfiguration(
            CloudProviderMode.Disabled,
            (CredentialConfigurationState)999,
            CaptureDataOptions.Disabled,
            StructuredLogOptions.SafeDefault));
    }

    [TestMethod]
    public void CloudCannotBeEnabledWithoutConfiguredUserCredentialReference()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new RuntimeConfiguration(
            CloudProviderMode.Enabled,
            CredentialConfigurationState.NotConfigured,
            CaptureDataOptions.Disabled,
            StructuredLogOptions.SafeDefault));
    }

    [TestMethod]
    public void InMemorySource_ReturnsDefensiveImmutableSnapshots()
    {
        RuntimeConfiguration initial = RuntimeConfiguration.SafeDefault;
        InMemoryRuntimeConfigurationSource source = new(initial);

        RuntimeConfiguration first = source.GetCurrent();
        RuntimeConfiguration second = source.GetCurrent();

        Assert.AreNotSame(initial, first);
        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first.CaptureData, second.CaptureData);
        Assert.AreNotSame(first.Logging, second.Logging);
        Assert.IsFalse(
            typeof(RuntimeConfiguration).GetProperties().Any(property => property.CanWrite));
        Assert.AreEqual(first.Logging.Capacity, second.Logging.Capacity);
    }

    [TestMethod]
    public async Task LoggingAndConfigurationOperations_DoNotClearSafetyStopLatch()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        await coordinator.EmergencyStopAsync();

        InMemoryRuntimeConfigurationSource configuration = new(RuntimeConfiguration.SafeDefault);
        InMemoryStructuredLog log = new(configuration.GetCurrent().Logging);
        log.Write(new StructuredLogEvent(
            "safety.stop-observed",
            DateTimeOffset.UtcNow,
            StructuredLogLevel.Warning,
            StructuredLogCategory.Safety,
            [
                new(
                    StructuredLogFieldNames.IsArmed,
                    StructuredLogValue.FromBoolean(false)),
                new(
                    StructuredLogFieldNames.StopReasonCode,
                    StructuredLogValue.FromString("emergency-stop")),
            ]));

        _ = configuration.GetCurrent();
        _ = log.GetSnapshot();

        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.AreEqual(ControlStopReason.EmergencyStop, coordinator.StopReason);
        await Assert.ThrowsExactlyAsync<ControlSafetyException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.MoveForward]).AsTask());
        Assert.IsEmpty(input.ExecutedActions);
    }

    private static ControlSafetyState CreateSafeState() =>
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
}
