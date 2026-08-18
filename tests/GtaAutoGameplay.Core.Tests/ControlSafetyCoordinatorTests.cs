using GtaAutoGameplay.Core.Input;
using GtaAutoGameplay.Core.Safety;
using GtaAutoGameplay.Core.Tests.Fakes;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class ControlSafetyCoordinatorTests
{
    [TestMethod]
    public async Task DefaultState_DeniesAutomaticInput()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new();
        ControlSafetyCoordinator coordinator = new(input, states);

        ControlSafetyException exception = await Assert.ThrowsExactlyAsync<ControlSafetyException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.MoveForward]).AsTask());

        Assert.AreEqual(ControlStopReason.NotArmed, exception.Reason);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.IsEmpty(input.ExecutedActions);
    }

    [TestMethod]
    public async Task ExplicitArmWithSafeConditions_AllowsShortActionBatch()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);

        Assert.IsTrue(coordinator.ArmFromUserAction());

        await coordinator.ExecuteBatchAsync(
            [SemanticAction.MoveForward, SemanticAction.Interact]);

        CollectionAssert.AreEqual(
            new[] { SemanticAction.MoveForward, SemanticAction.Interact },
            input.ExecutedActions.ToArray());
        Assert.IsTrue(coordinator.IsArmed);
        Assert.IsFalse(coordinator.IsStopLatched);
    }

    [TestMethod]
    [DataRow("target")]
    [DataRow("foreground")]
    [DataRow("capture")]
    [DataRow("freshness")]
    public async Task EachBatchRevalidatesCurrentSafetyState_AndStopsWhenUnsafe(string failure)
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());

        await coordinator.ExecuteBatchAsync([SemanticAction.MoveForward]);
        states.SetCurrentState(CreateUnsafeState(failure));

        ControlSafetyException exception = await Assert.ThrowsExactlyAsync<ControlSafetyException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.Interact]).AsTask());

        Assert.AreNotEqual(ControlStopReason.None, exception.Reason);
        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        CollectionAssert.AreEqual(
            new[] { SemanticAction.MoveForward },
            input.ExecutedActions.ToArray());
    }

    [TestMethod]
    public async Task EmergencyStop_RemainsLatchedWhenEnvironmentRecovers()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());

        await coordinator.EmergencyStopAsync();
        states.SetCurrentState(CreateUnsafeState("foreground"));
        states.SetCurrentState(CreateSafeState());

        ControlSafetyException exception = await Assert.ThrowsExactlyAsync<ControlSafetyException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.MoveForward]).AsTask());

        Assert.AreEqual(ControlStopReason.EmergencyStop, exception.Reason);
        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.IsEmpty(input.ExecutedActions);
    }

    [TestMethod]
    public async Task ExplicitRearmAfterEmergencyStop_RestoresEligibility()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        await coordinator.EmergencyStopAsync();

        Assert.IsTrue(coordinator.ArmFromUserAction());
        await coordinator.ExecuteBatchAsync([SemanticAction.Interact]);

        Assert.IsFalse(coordinator.IsStopLatched);
        Assert.IsTrue(coordinator.IsArmed);
        CollectionAssert.AreEqual(
            new[] { SemanticAction.Interact },
            input.ExecutedActions.ToArray());
    }

    [TestMethod]
    public async Task EmergencyStop_ReleasesOnlyTokensHeldByCoordinatorLedger()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        InputToken first = await coordinator.PressAsync(SemanticAction.MoveForward);
        InputToken second = await coordinator.PressAsync(SemanticAction.Aim);
        InputToken unrelated = new(Guid.NewGuid());

        await coordinator.EmergencyStopAsync();

        CollectionAssert.AreEquivalent(
            new[] { first, second },
            input.ReleasedTokens.ToArray());
        Assert.IsFalse(input.ReleasedTokens.Contains(unrelated));
        Assert.IsEmpty(coordinator.GetCurrentState().HeldInputs);
    }

    [TestMethod]
    public async Task RepeatedEmergencyStop_DoesNotReleaseSameTokenTwice()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        InputToken held = await coordinator.PressAsync(SemanticAction.MoveForward);

        Task firstStop = coordinator.EmergencyStopAsync().AsTask();
        Task repeatedStop = coordinator.EmergencyStopAsync().AsTask();
        await Task.WhenAll(firstStop, repeatedStop);

        Assert.HasCount(1, input.ReleaseBatches);
        Assert.HasCount(1, input.ReleasedTokens);
        Assert.AreEqual(held, input.ReleasedTokens[0]);
    }

    [TestMethod]
    public async Task Cancellation_LatchesSafeStopAndReleasesHeldTokens()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        InputToken held = await coordinator.PressAsync(SemanticAction.MoveForward);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => coordinator.ExecuteBatchAsync(
                [SemanticAction.Interact],
                cancellation.Token).AsTask());

        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.AreEqual(ControlStopReason.Cancelled, coordinator.StopReason);
        CollectionAssert.AreEqual(new[] { held }, input.ReleasedTokens.ToArray());
        Assert.IsEmpty(input.ExecutedActions);
    }

    [TestMethod]
    public async Task InputControllerException_LatchesSafeStopAndReleasesHeldTokens()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        InputToken held = await coordinator.PressAsync(SemanticAction.MoveForward);
        input.ExecuteHandler = (_, _) => ValueTask.FromException(
            new InvalidOperationException("Synthetic input failure."));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.Interact]).AsTask());

        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.AreEqual(ControlStopReason.InputControllerFailure, coordinator.StopReason);
        CollectionAssert.AreEqual(new[] { held }, input.ReleasedTokens.ToArray());
    }

    [TestMethod]
    public async Task SafetyStateSourceException_LatchesSafeStopAndReleasesHeldTokens()
    {
        FakeInputController input = new();
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        InputToken held = await coordinator.PressAsync(SemanticAction.MoveForward);
        states.ExceptionToThrow = new InvalidOperationException("Synthetic capture-state failure.");

        ControlSafetyException exception = await Assert.ThrowsExactlyAsync<ControlSafetyException>(
            () => coordinator.ExecuteBatchAsync([SemanticAction.Interact]).AsTask());

        Assert.AreEqual(ControlStopReason.SafetyStateUnavailable, exception.Reason);
        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        CollectionAssert.AreEqual(new[] { held }, input.ReleasedTokens.ToArray());
    }

    [TestMethod]
    public async Task ConcurrentExecutionAndEmergencyStop_DoNotDispatchActionAfterStop()
    {
        TaskCompletionSource firstActionStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource allowFirstActionToFinish = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakeInputController input = new()
        {
            ExecuteHandler = async (_, _) =>
            {
                firstActionStarted.TrySetResult();
                await allowFirstActionToFinish.Task.ConfigureAwait(false);
            },
        };
        FakeControlSafetyStateSource states = new(CreateSafeState());
        ControlSafetyCoordinator coordinator = new(input, states);
        Assert.IsTrue(coordinator.ArmFromUserAction());

        Task runningBatch = coordinator.ExecuteBatchAsync(
            [SemanticAction.MoveForward, SemanticAction.Interact]).AsTask();
        await firstActionStarted.Task.ConfigureAwait(false);

        await coordinator.EmergencyStopAsync();
        allowFirstActionToFinish.TrySetResult();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => runningBatch);
        CollectionAssert.AreEqual(
            new[] { SemanticAction.MoveForward },
            input.ExecutedActions.ToArray());
        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
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

    private static ControlSafetyState CreateUnsafeState(string failure) =>
        failure switch
        {
            "target" => new ControlSafetyState(
                captureTargetId: "capture-target",
                inputTargetId: "input-target",
                captureWindowIdentity: "window-1",
                captureProcessIdentity: "process-1",
                inputWindowIdentity: "window-2",
                inputProcessIdentity: "process-1",
                isInputTargetForeground: true,
                isCaptureHealthy: true,
                isStateFresh: true),
            "foreground" => CreateStateWith(isInputTargetForeground: false),
            "capture" => CreateStateWith(isCaptureHealthy: false),
            "freshness" => CreateStateWith(isStateFresh: false),
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };

    private static ControlSafetyState CreateStateWith(
        bool isInputTargetForeground = true,
        bool isCaptureHealthy = true,
        bool isStateFresh = true) =>
        new(
            captureTargetId: "capture-target",
            inputTargetId: "input-target",
            captureWindowIdentity: "window-1",
            captureProcessIdentity: "process-1",
            inputWindowIdentity: "window-1",
            inputProcessIdentity: "process-1",
            isInputTargetForeground: isInputTargetForeground,
            isCaptureHealthy: isCaptureHealthy,
            isStateFresh: isStateFresh);
}
