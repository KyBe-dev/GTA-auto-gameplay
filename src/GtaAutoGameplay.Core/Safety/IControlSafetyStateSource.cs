namespace GtaAutoGameplay.Core.Safety;

/// <summary>
/// Supplies the latest independently observed target, foreground, capture-health, and freshness state.
/// The coordinator owns and replaces the returned snapshot's armed state, stop reason, and input ledger.
/// </summary>
public interface IControlSafetyStateSource
{
    ControlSafetyState GetCurrentState();
}
