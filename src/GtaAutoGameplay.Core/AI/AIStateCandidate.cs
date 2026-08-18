using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.AI;

public sealed class AIStateCandidate
{
    public AIStateCandidate(string targetField, string candidateValue, double confidence)
    {
        TargetField = RequireText(targetField, nameof(targetField));
        CandidateValue = RequireText(candidateValue, nameof(candidateValue));
        Confidence = Domain.Confidence.EnsureValid(confidence, nameof(confidence));
    }

    public string TargetField { get; }

    public string CandidateValue { get; }

    public double Confidence { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }
}
