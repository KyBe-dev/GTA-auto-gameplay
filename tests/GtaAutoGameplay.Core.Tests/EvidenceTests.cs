using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class EvidenceTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

    [TestMethod]
    [DataRow(0d)]
    [DataRow(1d)]
    [DataRow(0.5d)]
    public void Constructor_AcceptsConfidenceBoundaryValues(double confidence)
    {
        Evidence evidence = CreateEvidence(confidence);

        Assert.AreEqual(confidence, evidence.FieldConfidence);
    }

    [TestMethod]
    [DataRow(-0.001d)]
    [DataRow(1.001d)]
    [DataRow(double.NaN)]
    public void Constructor_RejectsConfidenceOutsideBoundary(double confidence)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CreateEvidence(confidence));
    }

    [TestMethod]
    public void GetStatusAt_ReturnsExpiredAtValidityBoundary()
    {
        Evidence evidence = CreateEvidence(0.8d);

        Assert.AreEqual(EvidenceStatus.Fresh, evidence.GetStatusAt(ObservedAt.AddSeconds(4)));
        Assert.AreEqual(EvidenceStatus.Expired, evidence.GetStatusAt(ObservedAt.AddSeconds(5)));
    }

    [TestMethod]
    public void GetStatusAt_PreservesConflictEvenAfterExpiry()
    {
        Evidence evidence = CreateEvidence(0.8d, EvidenceStatus.Conflicting);

        Assert.AreEqual(EvidenceStatus.Conflicting, evidence.GetStatusAt(ObservedAt.AddMinutes(1)));
    }

    [TestMethod]
    public void Constructor_RejectsExpiryBeforeObservation()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new Evidence(
                Guid.NewGuid(),
                EvidenceSourceType.Vision,
                ObservedAt,
                ObservedAt.AddTicks(-1),
                "GameMode",
                "Gameplay",
                0.8d,
                "test-adapter",
                "1.0.0"));
    }

    private static Evidence CreateEvidence(
        double confidence,
        EvidenceStatus status = EvidenceStatus.Fresh) =>
        new(
            Guid.NewGuid(),
            EvidenceSourceType.Vision,
            ObservedAt,
            ObservedAt.AddSeconds(5),
            "GameMode",
            "Gameplay",
            confidence,
            "test-adapter",
            "1.0.0",
            status);
}
