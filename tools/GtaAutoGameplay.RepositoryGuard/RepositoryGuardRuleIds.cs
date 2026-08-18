namespace GtaAutoGameplay.RepositoryGuard;

public static class RepositoryGuardRuleIds
{
    public const string AssignedSecret = "SECRET001";
    public const string AccessToken = "SECRET002";
    public const string PrivateKey = "SECRET003";
    public const string JsonWebToken = "SECRET004";
    public const string SecretFile = "PATH001";
    public const string PrivateCertificate = "PATH002";
    public const string GameResource = "PATH003";
    public const string UnreviewedMediaOrModel = "PATH004";
    public const string GeneratedOrReleaseOutput = "PATH005";
    public const string BinaryFile = "FILE001";
    public const string LargeFile = "FILE002";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        [
            AssignedSecret,
            AccessToken,
            PrivateKey,
            JsonWebToken,
            SecretFile,
            PrivateCertificate,
            GameResource,
            UnreviewedMediaOrModel,
            GeneratedOrReleaseOutput,
            BinaryFile,
            LargeFile,
        ],
        StringComparer.Ordinal);
}
