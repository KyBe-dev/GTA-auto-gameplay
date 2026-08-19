using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.StateEstimation;

public interface IStateEstimator
{
    StateEstimationResult Estimate(
        IReadOnlyCollection<Evidence> evidence,
        DateTimeOffset evaluatedAt,
        string adapterId,
        string adapterVersion,
        GameState? previousState = null);
}
