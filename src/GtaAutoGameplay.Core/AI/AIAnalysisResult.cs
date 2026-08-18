using System.Collections.ObjectModel;

namespace GtaAutoGameplay.Core.AI;

public sealed class AIAnalysisResult
{
    private readonly ReadOnlyCollection<AIStateCandidate> _candidates;

    public AIAnalysisResult(
        AIProviderAvailability availability,
        IEnumerable<AIStateCandidate>? candidates = null,
        bool requiresHumanConfirmation = false,
        string? failureCode = null)
    {
        if (!Enum.IsDefined(availability))
        {
            throw new ArgumentOutOfRangeException(nameof(availability));
        }

        Availability = availability;
        _candidates = Array.AsReadOnly((candidates ?? []).ToArray());
        RequiresHumanConfirmation = requiresHumanConfirmation;
        FailureCode = failureCode;
    }

    public AIProviderAvailability Availability { get; }

    public IReadOnlyList<AIStateCandidate> Candidates => _candidates;

    public bool RequiresHumanConfirmation { get; }

    public string? FailureCode { get; }
}
