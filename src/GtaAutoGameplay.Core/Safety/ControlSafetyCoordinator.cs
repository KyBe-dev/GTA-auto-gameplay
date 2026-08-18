using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Input;

namespace GtaAutoGameplay.Core.Safety;

public sealed class ControlSafetyCoordinator
{
    private readonly IInputController _inputController;
    private readonly IControlSafetyStateSource _stateSource;
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    private readonly object _sync = new();
    private readonly HashSet<InputToken> _heldInputs = [];
    private readonly HashSet<InputToken> _releasingInputs = [];

    private CancellationTokenSource? _activeOperationCancellation;
    private bool _isArmed;
    private bool _isStopLatched;
    private ControlStopReason _stopReason;

    public ControlSafetyCoordinator(
        IInputController inputController,
        IControlSafetyStateSource stateSource)
    {
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
        _stateSource = stateSource ?? throw new ArgumentNullException(nameof(stateSource));
    }

    public bool IsArmed
    {
        get
        {
            lock (_sync)
            {
                return _isArmed;
            }
        }
    }

    public bool IsStopLatched
    {
        get
        {
            lock (_sync)
            {
                return _isStopLatched;
            }
        }
    }

    public ControlStopReason StopReason
    {
        get
        {
            lock (_sync)
            {
                return _stopReason;
            }
        }
    }

    public bool ArmFromUserAction()
    {
        ControlSafetyState environment;

        try
        {
            environment = GetEnvironmentState();
        }
        catch
        {
            return false;
        }

        lock (_sync)
        {
            if (!environment.MeetsInputSafetyConditions
                || _activeOperationCancellation is not null
                || _heldInputs.Count > 0
                || _releasingInputs.Count > 0)
            {
                return false;
            }

            _isArmed = true;
            _isStopLatched = false;
            _stopReason = ControlStopReason.None;
            return true;
        }
    }

    public ControlSafetyState GetCurrentState()
    {
        ControlSafetyState environment = GetEnvironmentState();

        lock (_sync)
        {
            InputToken[] ledger = [.. _heldInputs, .. _releasingInputs];

            return new ControlSafetyState(
                captureTargetId: environment.CaptureTargetId,
                inputTargetId: environment.InputTargetId,
                captureWindowIdentity: environment.CaptureWindowIdentity,
                captureProcessIdentity: environment.CaptureProcessIdentity,
                inputWindowIdentity: environment.InputWindowIdentity,
                inputProcessIdentity: environment.InputProcessIdentity,
                isInputTargetForeground: environment.IsInputTargetForeground,
                isCaptureHealthy: environment.IsCaptureHealthy,
                isStateFresh: environment.IsStateFresh,
                isArmed: _isArmed && !_isStopLatched,
                heldInputs: ledger,
                stopReason: _stopReason);
        }
    }

    public async ValueTask ExecuteBatchAsync(
        IReadOnlyCollection<SemanticAction> actions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);

        SemanticAction[] actionSnapshot = [.. actions];
        if (actionSnapshot.Length == 0)
        {
            throw new ArgumentException("An input batch must contain at least one action.", nameof(actions));
        }

        foreach (SemanticAction action in actionSnapshot)
        {
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(actions), action, "Unknown semantic action.");
            }
        }

        await RunSerializedOperationAsync(
            async operationCancellation =>
            {
                foreach (SemanticAction action in actionSnapshot)
                {
                    operationCancellation.ThrowIfCancellationRequested();
                    await DispatchExecuteAsync(action, operationCancellation).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<InputToken> PressAsync(
        SemanticAction action,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        InputToken? pressedToken = null;

        await RunSerializedOperationAsync(
            async operationCancellation =>
            {
                pressedToken = await DispatchPressAsync(action, operationCancellation).ConfigureAwait(false);

                bool stoppedWhilePressing;
                ControlStopReason stopReason;

                lock (_sync)
                {
                    if (!_heldInputs.Add(pressedToken))
                    {
                        throw new InvalidOperationException("The input controller returned a duplicate input token.");
                    }

                    stoppedWhilePressing = _isStopLatched || !_isArmed;
                    stopReason = _stopReason == ControlStopReason.None
                        ? ControlStopReason.NotArmed
                        : _stopReason;
                }

                if (stoppedWhilePressing)
                {
                    await StopAndReleaseAsync(stopReason).ConfigureAwait(false);
                    throw new ControlSafetyException(stopReason);
                }
            },
            cancellationToken).ConfigureAwait(false);

        return pressedToken!;
    }

    public async ValueTask ReleaseAsync(
        InputToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        bool enteredGate = false;
        try
        {
            await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            lock (_sync)
            {
                if (!_heldInputs.Remove(token))
                {
                    return;
                }

                _releasingInputs.Add(token);
            }

            try
            {
                await _inputController.ReleaseAsync(token, cancellationToken).ConfigureAwait(false);

                lock (_sync)
                {
                    _releasingInputs.Remove(token);
                }
            }
            catch (OperationCanceledException)
            {
                lock (_sync)
                {
                    _releasingInputs.Remove(token);
                    _heldInputs.Add(token);
                }

                throw;
            }
            catch
            {
                lock (_sync)
                {
                    _releasingInputs.Remove(token);
                    _heldInputs.Add(token);
                }

                await StopAndReleaseAsync(ControlStopReason.InputControllerFailure).ConfigureAwait(false);
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            await StopAndReleaseAsync(ControlStopReason.Cancelled).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (enteredGate)
            {
                _executionGate.Release();
            }
        }
    }

    public ValueTask EmergencyStopAsync() =>
        StopAndReleaseAsync(ControlStopReason.EmergencyStop);

    private async ValueTask RunSerializedOperationAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken)
    {
        bool enteredGate = false;
        CancellationTokenSource? operationCancellation = null;

        try
        {
            await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (_sync)
            {
                _activeOperationCancellation = operationCancellation;
            }

            await operation(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (ControlSafetyException exception)
        {
            if (exception.Reason == ControlStopReason.SafetyStateUnavailable)
            {
                await StopAndReleaseAsync(ControlStopReason.SafetyStateUnavailable).ConfigureAwait(false);
            }

            throw;
        }
        catch (OperationCanceledException)
        {
            await StopAndReleaseAsync(ControlStopReason.Cancelled).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await StopAndReleaseAsync(ControlStopReason.InputControllerFailure).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (operationCancellation is not null)
            {
                lock (_sync)
                {
                    if (ReferenceEquals(_activeOperationCancellation, operationCancellation))
                    {
                        _activeOperationCancellation = null;
                    }
                }

                operationCancellation.Dispose();
            }

            if (enteredGate)
            {
                _executionGate.Release();
            }
        }
    }

    private async ValueTask DispatchExecuteAsync(
        SemanticAction action,
        CancellationToken cancellationToken)
    {
        ControlSafetyState environment = GetEnvironmentStateOrStop();
        ControlStopReason denialReason;
        ValueTask dispatch = ValueTask.CompletedTask;

        lock (_sync)
        {
            denialReason = GetDenialReasonLocked(environment);
            if (denialReason == ControlStopReason.None)
            {
                dispatch = _inputController.ExecuteAsync(action, cancellationToken);
            }
        }

        if (denialReason != ControlStopReason.None)
        {
            await HandleDenialAsync(denialReason).ConfigureAwait(false);
        }

        await dispatch.ConfigureAwait(false);
    }

    private async ValueTask<InputToken> DispatchPressAsync(
        SemanticAction action,
        CancellationToken cancellationToken)
    {
        ControlSafetyState environment = GetEnvironmentStateOrStop();
        ControlStopReason denialReason;
        ValueTask<InputToken>? dispatch = null;

        lock (_sync)
        {
            denialReason = GetDenialReasonLocked(environment);
            if (denialReason == ControlStopReason.None)
            {
                dispatch = _inputController.PressAsync(action, cancellationToken);
            }
        }

        if (denialReason != ControlStopReason.None)
        {
            await HandleDenialAsync(denialReason).ConfigureAwait(false);
        }

        return await dispatch!.Value.ConfigureAwait(false);
    }

    private ControlSafetyState GetEnvironmentStateOrStop()
    {
        try
        {
            return GetEnvironmentState();
        }
        catch (Exception exception)
        {
            throw new ControlSafetyException(ControlStopReason.SafetyStateUnavailable, exception);
        }
    }

    private ControlSafetyState GetEnvironmentState() =>
        _stateSource.GetCurrentState()
        ?? throw new InvalidOperationException("The safety state source returned null.");

    private ControlStopReason GetDenialReasonLocked(ControlSafetyState environment)
    {
        if (!_isArmed || _isStopLatched)
        {
            return _stopReason == ControlStopReason.None
                ? ControlStopReason.NotArmed
                : _stopReason;
        }

        if (!environment.TargetsMatch)
        {
            return ControlStopReason.TargetIdentityMismatch;
        }

        if (!environment.IsInputTargetForeground)
        {
            return ControlStopReason.InputTargetNotForeground;
        }

        if (!environment.IsCaptureHealthy)
        {
            return ControlStopReason.CaptureUnhealthy;
        }

        return environment.IsStateFresh
            ? ControlStopReason.None
            : ControlStopReason.StateStale;
    }

    private async ValueTask HandleDenialAsync(ControlStopReason reason)
    {
        if (reason != ControlStopReason.NotArmed && !IsStopLatched)
        {
            await StopAndReleaseAsync(reason).ConfigureAwait(false);
        }

        throw new ControlSafetyException(reason);
    }

    private async ValueTask StopAndReleaseAsync(ControlStopReason reason)
    {
        InputToken[] tokensToRelease;
        CancellationTokenSource? activeOperation;

        lock (_sync)
        {
            _isArmed = false;
            _isStopLatched = true;
            SetStopReasonLocked(reason);
            activeOperation = _activeOperationCancellation;

            tokensToRelease = [.. _heldInputs];
            foreach (InputToken token in tokensToRelease)
            {
                _heldInputs.Remove(token);
                _releasingInputs.Add(token);
            }
        }

        Exception? cancellationException = null;
        try
        {
            activeOperation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The operation completed between the snapshot and cancellation.
        }
        catch (Exception exception)
        {
            cancellationException = exception;
        }

        if (tokensToRelease.Length > 0)
        {
            ReadOnlyCollection<InputToken> releaseBatch = Array.AsReadOnly(tokensToRelease);

            try
            {
                await _inputController
                    .ReleaseHeldInputsAsync(releaseBatch, CancellationToken.None)
                    .ConfigureAwait(false);

                lock (_sync)
                {
                    foreach (InputToken token in tokensToRelease)
                    {
                        _releasingInputs.Remove(token);
                    }
                }
            }
            catch
            {
                lock (_sync)
                {
                    foreach (InputToken token in tokensToRelease)
                    {
                        _releasingInputs.Remove(token);
                        _heldInputs.Add(token);
                    }
                }

                throw;
            }
        }

        if (cancellationException is not null)
        {
            throw cancellationException;
        }
    }

    private void SetStopReasonLocked(ControlStopReason reason)
    {
        if (_stopReason == ControlStopReason.EmergencyStop)
        {
            return;
        }

        if (reason == ControlStopReason.EmergencyStop || _stopReason == ControlStopReason.None)
        {
            _stopReason = reason;
        }
    }
}
