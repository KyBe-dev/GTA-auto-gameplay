namespace GtaAutoGameplay.Core.StateEstimation;

public static class StateFieldNames
{
    public const string GameMode = "gameMode";
    public const string ControlMode = "controlMode";
    public const string MenuSubstate = "menuSubstate";
    public const string ObjectiveType = "objectiveType";

    public static bool TryGetField(string value, out StateField field)
    {
        field = value switch
        {
            GameMode => StateField.GameMode,
            ControlMode => StateField.ControlMode,
            MenuSubstate => StateField.MenuSubstate,
            ObjectiveType => StateField.ObjectiveType,
            _ => default,
        };

        return value is GameMode or ControlMode or MenuSubstate or ObjectiveType;
    }

    public static string GetName(StateField field) => field switch
    {
        StateField.GameMode => GameMode,
        StateField.ControlMode => ControlMode,
        StateField.MenuSubstate => MenuSubstate,
        StateField.ObjectiveType => ObjectiveType,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };
}
