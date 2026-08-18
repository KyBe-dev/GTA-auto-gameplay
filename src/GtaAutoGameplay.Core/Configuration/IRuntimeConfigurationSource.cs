namespace GtaAutoGameplay.Core.Configuration;

public interface IRuntimeConfigurationSource
{
    RuntimeConfiguration GetCurrent();
}
