namespace GtaAutoGameplay.Core.Input;

public interface IInputController
{
    ValueTask ExecuteAsync(SemanticAction action, CancellationToken cancellationToken);

    ValueTask<InputToken> PressAsync(SemanticAction action, CancellationToken cancellationToken);

    ValueTask ReleaseAsync(InputToken token, CancellationToken cancellationToken);

    ValueTask ReleaseHeldInputsAsync(
        IReadOnlyCollection<InputToken> heldInputs,
        CancellationToken cancellationToken);
}
