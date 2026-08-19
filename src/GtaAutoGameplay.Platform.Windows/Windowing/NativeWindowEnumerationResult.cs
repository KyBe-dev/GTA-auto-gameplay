using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Targeting;

namespace GtaAutoGameplay.Platform.Windows.Windowing;

internal sealed class NativeWindowEnumerationResult
{
    private readonly ReadOnlyCollection<NativeWindowReference> _windows;

    private NativeWindowEnumerationResult(
        IEnumerable<NativeWindowReference> windows,
        WindowDiscoveryFailure? failure)
    {
        NativeWindowReference[] snapshot = windows.ToArray();
        if (failure is not null && snapshot.Length != 0)
        {
            throw new ArgumentException("Failed enumeration cannot contain windows.");
        }

        _windows = Array.AsReadOnly(snapshot);
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;

    public IReadOnlyList<NativeWindowReference> Windows => _windows;

    public WindowDiscoveryFailure? Failure { get; }

    public static NativeWindowEnumerationResult Succeeded(
        IEnumerable<NativeWindowReference> windows) =>
        new(windows ?? throw new ArgumentNullException(nameof(windows)), failure: null);

    public static NativeWindowEnumerationResult Failed(WindowDiscoveryFailure failure)
    {
        if (!Enum.IsDefined(failure) || failure == WindowDiscoveryFailure.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        return new([], failure);
    }
}
