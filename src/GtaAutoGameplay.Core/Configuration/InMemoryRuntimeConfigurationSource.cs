namespace GtaAutoGameplay.Core.Configuration;

public sealed class InMemoryRuntimeConfigurationSource : IRuntimeConfigurationSource
{
    private readonly object _sync = new();
    private RuntimeConfiguration _current;

    public InMemoryRuntimeConfigurationSource(RuntimeConfiguration initialConfiguration)
    {
        _current = (initialConfiguration
            ?? throw new ArgumentNullException(nameof(initialConfiguration))).Copy();
    }

    public RuntimeConfiguration GetCurrent()
    {
        lock (_sync)
        {
            return _current.Copy();
        }
    }

    public void Replace(RuntimeConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_sync)
        {
            _current = configuration.Copy();
        }
    }
}
