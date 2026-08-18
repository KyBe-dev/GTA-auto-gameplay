using System.Collections.ObjectModel;

namespace GtaAutoGameplay.Core.Domain;

public sealed class GameState
{
    private readonly ReadOnlyCollection<Evidence> _evidence;

    public GameState(
        DateTimeOffset observedAt,
        GameMode gameMode = GameMode.Unknown,
        ControlMode controlMode = ControlMode.Manual,
        MenuSubstate menuSubstate = MenuSubstate.None,
        string? missionId = null,
        string? missionStageId = null,
        string? visibleObjectiveText = null,
        ObjectiveType objectiveType = ObjectiveType.Unknown,
        string? objectiveTarget = null,
        string? playerLocationEstimate = null,
        double? targetDirectionDegrees = null,
        double? targetDistance = null,
        string? currentCharacter = null,
        string? inputProfileId = null,
        double confidence = 0d,
        IEnumerable<Evidence>? evidence = null)
    {
        if (!Enum.IsDefined(gameMode))
        {
            throw new ArgumentOutOfRangeException(nameof(gameMode));
        }

        if (!Enum.IsDefined(controlMode))
        {
            throw new ArgumentOutOfRangeException(nameof(controlMode));
        }

        if (!Enum.IsDefined(menuSubstate))
        {
            throw new ArgumentOutOfRangeException(nameof(menuSubstate));
        }

        if (!Enum.IsDefined(objectiveType))
        {
            throw new ArgumentOutOfRangeException(nameof(objectiveType));
        }

        if (targetDistance is < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDistance));
        }

        ObservedAt = observedAt;
        GameMode = gameMode;
        ControlMode = controlMode;
        MenuSubstate = menuSubstate;
        MissionId = missionId;
        MissionStageId = missionStageId;
        VisibleObjectiveText = visibleObjectiveText;
        ObjectiveType = objectiveType;
        ObjectiveTarget = objectiveTarget;
        PlayerLocationEstimate = playerLocationEstimate;
        TargetDirectionDegrees = targetDirectionDegrees;
        TargetDistance = targetDistance;
        CurrentCharacter = currentCharacter;
        InputProfileId = inputProfileId;
        Confidence = Domain.Confidence.EnsureValid(confidence, nameof(confidence));
        _evidence = Array.AsReadOnly((evidence ?? []).ToArray());
    }

    public DateTimeOffset ObservedAt { get; }

    public GameMode GameMode { get; }

    public ControlMode ControlMode { get; }

    public MenuSubstate MenuSubstate { get; }

    public string? MissionId { get; }

    public string? MissionStageId { get; }

    public string? VisibleObjectiveText { get; }

    public ObjectiveType ObjectiveType { get; }

    public string? ObjectiveTarget { get; }

    public string? PlayerLocationEstimate { get; }

    public double? TargetDirectionDegrees { get; }

    public double? TargetDistance { get; }

    public string? CurrentCharacter { get; }

    public string? InputProfileId { get; }

    public double Confidence { get; }

    public IReadOnlyList<Evidence> Evidence => _evidence;
}
