namespace GtaAutoGameplay.Core.Targeting;

public sealed class WindowSelectionResult
{
    private WindowSelectionResult(
        WindowSelection? selection,
        WindowSelectionFailure? failure)
    {
        if ((selection is null) == (failure is null))
        {
            throw new ArgumentException(
                "A selection result must contain exactly one selection or failure.");
        }

        if (failure is not null &&
            (!Enum.IsDefined(failure.Value) || failure == WindowSelectionFailure.Unknown))
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        Selection = selection;
        Failure = failure;
    }

    public bool IsSuccess => Selection is not null;

    public WindowSelection? Selection { get; }

    public WindowSelectionFailure? Failure { get; }

    public static WindowSelectionResult Succeeded(WindowSelection selection) =>
        new(selection ?? throw new ArgumentNullException(nameof(selection)), failure: null);

    public static WindowSelectionResult Failed(WindowSelectionFailure failure) =>
        new(selection: null, failure);
}
