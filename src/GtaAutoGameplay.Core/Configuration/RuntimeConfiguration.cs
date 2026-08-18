namespace GtaAutoGameplay.Core.Configuration;

public sealed class RuntimeConfiguration
{
    public RuntimeConfiguration(
        CloudProviderMode cloudProviderMode,
        CredentialConfigurationState credentialState,
        CaptureDataOptions captureData,
        StructuredLogOptions logging)
    {
        if (!Enum.IsDefined(cloudProviderMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cloudProviderMode),
                cloudProviderMode,
                "Unknown cloud Provider mode.");
        }

        if (!Enum.IsDefined(credentialState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(credentialState),
                credentialState,
                "Unknown credential configuration state.");
        }

        if (cloudProviderMode == CloudProviderMode.Enabled
            && credentialState != CredentialConfigurationState.Configured)
        {
            throw new ArgumentException(
                "Cloud Provider mode cannot be enabled without a configured user credential reference.",
                nameof(credentialState));
        }

        CloudProviderMode = cloudProviderMode;
        CredentialState = credentialState;
        CaptureData = (captureData ?? throw new ArgumentNullException(nameof(captureData))).Copy();
        Logging = (logging ?? throw new ArgumentNullException(nameof(logging))).Copy();
    }

    public CloudProviderMode CloudProviderMode { get; }

    public CredentialConfigurationState CredentialState { get; }

    public CaptureDataOptions CaptureData { get; }

    public StructuredLogOptions Logging { get; }

    public static RuntimeConfiguration SafeDefault { get; } = new(
        CloudProviderMode.Disabled,
        CredentialConfigurationState.NotConfigured,
        CaptureDataOptions.Disabled,
        StructuredLogOptions.SafeDefault);

    internal RuntimeConfiguration Copy() =>
        new(CloudProviderMode, CredentialState, CaptureData, Logging);
}
