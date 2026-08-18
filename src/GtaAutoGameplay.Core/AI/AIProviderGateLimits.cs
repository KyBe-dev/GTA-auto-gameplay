namespace GtaAutoGameplay.Core.AI;

public static class AIProviderGateLimits
{
    private static readonly HashSet<string> AllowedTargetFieldSet = new(
        [
            "gameMode",
            "menuSubstate",
            "controlMode",
            "missionId",
            "missionStageId",
            "visibleObjectiveText",
            "objectiveType",
            "objectiveTarget",
            "playerLocationEstimate",
            "targetDirection",
            "targetDistance",
            "currentCharacter",
            "inputProfileId",
        ],
        StringComparer.Ordinal);

    public const int MaximumCandidates = 16;
    public const int MaximumTargetFieldLength = 64;
    public const int MaximumCandidateValueLength = 256;

    public static bool IsAllowedTargetField(string targetField)
    {
        ArgumentNullException.ThrowIfNull(targetField);
        return AllowedTargetFieldSet.Contains(targetField);
    }
}
