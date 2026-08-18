using GtaAutoGameplay.Core.Input;

namespace GtaAutoGameplay.Core.Tests.Fakes;

internal sealed class FakeInputController : IInputController
{
    private readonly object _sync = new();
    private readonly List<SemanticAction> _executedActions = [];
    private readonly List<SemanticAction> _pressedActions = [];
    private readonly List<InputToken> _releasedTokens = [];
    private readonly List<IReadOnlyList<InputToken>> _releaseBatches = [];

    public Func<SemanticAction, CancellationToken, ValueTask>? ExecuteHandler { get; set; }

    public Func<SemanticAction, CancellationToken, ValueTask<InputToken>>? PressHandler { get; set; }

    public IReadOnlyList<SemanticAction> ExecutedActions
    {
        get
        {
            lock (_sync)
            {
                return _executedActions.ToArray();
            }
        }
    }

    public IReadOnlyList<SemanticAction> PressedActions
    {
        get
        {
            lock (_sync)
            {
                return _pressedActions.ToArray();
            }
        }
    }

    public IReadOnlyList<InputToken> ReleasedTokens
    {
        get
        {
            lock (_sync)
            {
                return _releasedTokens.ToArray();
            }
        }
    }

    public IReadOnlyList<IReadOnlyList<InputToken>> ReleaseBatches
    {
        get
        {
            lock (_sync)
            {
                return _releaseBatches.Select(batch => (IReadOnlyList<InputToken>)batch.ToArray()).ToArray();
            }
        }
    }

    public ValueTask ExecuteAsync(SemanticAction action, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _executedActions.Add(action);
        }

        return ExecuteHandler?.Invoke(action, cancellationToken) ?? ValueTask.CompletedTask;
    }

    public ValueTask<InputToken> PressAsync(
        SemanticAction action,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _pressedActions.Add(action);
        }

        return PressHandler?.Invoke(action, cancellationToken)
            ?? ValueTask.FromResult(new InputToken(Guid.NewGuid()));
    }

    public ValueTask ReleaseAsync(InputToken token, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _releasedTokens.Add(token);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ReleaseHeldInputsAsync(
        IReadOnlyCollection<InputToken> heldInputs,
        CancellationToken cancellationToken)
    {
        InputToken[] snapshot = [.. heldInputs];

        lock (_sync)
        {
            _releaseBatches.Add(snapshot);
            _releasedTokens.AddRange(snapshot);
        }

        return ValueTask.CompletedTask;
    }
}
