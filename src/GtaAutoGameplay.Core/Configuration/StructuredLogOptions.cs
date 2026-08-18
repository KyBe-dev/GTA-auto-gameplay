namespace GtaAutoGameplay.Core.Configuration;

public sealed class StructuredLogOptions
{
    public const int MinimumCapacity = 1;
    public const int MaximumCapacity = 100_000;
    public const int DefaultCapacity = 512;

    public static readonly TimeSpan MinimumRetentionPeriod = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaximumRetentionPeriod = TimeSpan.FromDays(30);
    public static readonly TimeSpan DefaultRetentionPeriod = TimeSpan.FromHours(24);

    public StructuredLogOptions(int capacity, TimeSpan retentionPeriod)
    {
        if (capacity < MinimumCapacity || capacity > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"Log capacity must be from {MinimumCapacity} through {MaximumCapacity} events.");
        }

        if (retentionPeriod < MinimumRetentionPeriod
            || retentionPeriod > MaximumRetentionPeriod)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionPeriod),
                retentionPeriod,
                $"Log retention must be from {MinimumRetentionPeriod} through {MaximumRetentionPeriod}.");
        }

        Capacity = capacity;
        RetentionPeriod = retentionPeriod;
    }

    public int Capacity { get; }

    public TimeSpan RetentionPeriod { get; }

    public bool UsesFieldWhitelist => true;

    public static StructuredLogOptions SafeDefault { get; } = new(
        DefaultCapacity,
        DefaultRetentionPeriod);

    internal StructuredLogOptions Copy() => new(Capacity, RetentionPeriod);
}
