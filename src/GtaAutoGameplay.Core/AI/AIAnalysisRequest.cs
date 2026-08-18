using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.AI;

public sealed class AIAnalysisRequest
{
    private readonly ReadOnlyCollection<Evidence> _contextEvidence;

    public AIAnalysisRequest(
        Guid requestId,
        string purpose,
        GameState currentState,
        IEnumerable<Evidence>? contextEvidence = null)
    {
        if (requestId == Guid.Empty)
        {
            throw new ArgumentException("Request ID cannot be empty.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Purpose cannot be empty or whitespace.", nameof(purpose));
        }

        RequestId = requestId;
        Purpose = purpose;
        CurrentState = currentState ?? throw new ArgumentNullException(nameof(currentState));
        _contextEvidence = Array.AsReadOnly((contextEvidence ?? []).ToArray());
    }

    public Guid RequestId { get; }

    public string Purpose { get; }

    public GameState CurrentState { get; }

    public IReadOnlyList<Evidence> ContextEvidence => _contextEvidence;
}
