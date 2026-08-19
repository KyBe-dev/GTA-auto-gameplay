namespace GtaAutoGameplay.Platform.Windows.Windowing;

internal readonly record struct NativeWindowReference
{
    public NativeWindowReference(nint value)
    {
        if (value == nint.Zero)
        {
            throw new ArgumentException("Native window reference cannot be zero.", nameof(value));
        }

        Value = value;
    }

    public nint Value { get; }
}
