using GtaAutoGameplay.Core.Safety;

namespace GtaAutoGameplay.Core.Tests.Fakes;

internal sealed class FakeControlSafetyStateSource : IControlSafetyStateSource
{
    private ControlSafetyState _currentState;

    public FakeControlSafetyStateSource(ControlSafetyState? initialState = null)
    {
        _currentState = initialState ?? new ControlSafetyState();
    }

    public Exception? ExceptionToThrow { get; set; }

    public ControlSafetyState GetCurrentState()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Volatile.Read(ref _currentState);
    }

    public void SetCurrentState(ControlSafetyState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Volatile.Write(ref _currentState, state);
    }
}
