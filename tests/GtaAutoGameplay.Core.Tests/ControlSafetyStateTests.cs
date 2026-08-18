using GtaAutoGameplay.Core.Domain;
using GtaAutoGameplay.Core.Input;
using GtaAutoGameplay.Core.Safety;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class ControlSafetyStateTests
{
    [TestMethod]
    public void Constructor_DefaultsToDisarmedAndDenied()
    {
        ControlSafetyState safetyState = new();

        Assert.IsFalse(safetyState.IsArmed);
        Assert.IsFalse(safetyState.TargetsMatch);
        Assert.IsFalse(safetyState.CanSendInput);
        Assert.IsEmpty(safetyState.HeldInputs);
    }

    [TestMethod]
    public void MaximumGameStateConfidence_DoesNotAuthorizeInput()
    {
        GameState gameState = new(DateTimeOffset.UtcNow, confidence: 1d);
        ControlSafetyState safetyState = new();

        Assert.AreEqual(1d, gameState.Confidence);
        Assert.IsFalse(safetyState.CanSendInput);
    }

    [TestMethod]
    public void CanSendInput_RequiresEveryIndependentSafetyCondition()
    {
        ControlSafetyState allowed = CreateFullyValidatedState();
        ControlSafetyState stale = CreateFullyValidatedState(isStateFresh: false);
        ControlSafetyState background = CreateFullyValidatedState(isInputTargetForeground: false);
        ControlSafetyState disarmed = CreateFullyValidatedState(isArmed: false);

        Assert.IsTrue(allowed.CanSendInput);
        Assert.IsFalse(stale.CanSendInput);
        Assert.IsFalse(background.CanSendInput);
        Assert.IsFalse(disarmed.CanSendInput);
    }

    [TestMethod]
    public void TargetsMatch_RejectsDifferentCaptureAndInputIdentity()
    {
        ControlSafetyState state = new(
            captureTargetId: "capture-target",
            inputTargetId: "input-target",
            captureWindowIdentity: "window-a",
            captureProcessIdentity: "process-a",
            inputWindowIdentity: "window-b",
            inputProcessIdentity: "process-a",
            isInputTargetForeground: true,
            isCaptureHealthy: true,
            isStateFresh: true,
            isArmed: true);

        Assert.IsFalse(state.TargetsMatch);
        Assert.IsFalse(state.CanSendInput);
    }

    [TestMethod]
    public void Constructor_CopiesHeldInputLedger()
    {
        List<InputToken> source = [new InputToken(Guid.NewGuid())];
        ControlSafetyState state = new(heldInputs: source);
        source.Clear();

        Assert.HasCount(1, state.HeldInputs);
    }

    private static ControlSafetyState CreateFullyValidatedState(
        bool isInputTargetForeground = true,
        bool isStateFresh = true,
        bool isArmed = true) =>
        new(
            captureTargetId: "capture-target",
            inputTargetId: "input-target",
            captureWindowIdentity: "window-1",
            captureProcessIdentity: "process-1",
            inputWindowIdentity: "window-1",
            inputProcessIdentity: "process-1",
            isInputTargetForeground: isInputTargetForeground,
            isCaptureHealthy: true,
            isStateFresh: isStateFresh,
            isArmed: isArmed);
}
