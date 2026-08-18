namespace GtaAutoGameplay.Core.Credentials;

public sealed record CredentialReference
{
    public CredentialReference(string providerId, string credentialName)
    {
        ProviderId = RequireText(providerId, nameof(providerId));
        CredentialName = RequireText(credentialName, nameof(credentialName));
    }

    public string ProviderId { get; }

    public string CredentialName { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }
}
