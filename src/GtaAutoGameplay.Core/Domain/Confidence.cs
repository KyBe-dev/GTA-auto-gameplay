namespace GtaAutoGameplay.Core.Domain;

internal static class Confidence
{
    public static double EnsureValid(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Confidence must be a finite value from 0 through 1.");
        }

        return value;
    }
}
