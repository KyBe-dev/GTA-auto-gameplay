using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class GameStateTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Constructor_UsesSafeDefaults()
    {
        GameState state = new(ObservedAt);

        Assert.AreEqual(GameMode.Unknown, state.GameMode);
        Assert.AreEqual(ControlMode.Manual, state.ControlMode);
        Assert.AreEqual(MenuSubstate.None, state.MenuSubstate);
        Assert.AreEqual(0d, state.Confidence);
        Assert.IsEmpty(state.Evidence);
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(1d)]
    public void Constructor_AcceptsConfidenceBoundaryValues(double confidence)
    {
        GameState state = new(ObservedAt, confidence: confidence);

        Assert.AreEqual(confidence, state.Confidence);
    }

    [TestMethod]
    [DataRow(-0.001d)]
    [DataRow(1.001d)]
    [DataRow(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidConfidence(double confidence)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GameState(ObservedAt, confidence: confidence));
    }

    [TestMethod]
    public void Constructor_CopiesEvidenceCollection()
    {
        List<Evidence> source =
        [
            new Evidence(
                Guid.NewGuid(),
                EvidenceSourceType.Ocr,
                ObservedAt,
                ObservedAt.AddSeconds(2),
                "VisibleObjectiveText",
                "Test objective",
                0.7d,
                "test-adapter",
                "1.0.0"),
        ];

        GameState state = new(ObservedAt, evidence: source);
        source.Clear();

        Assert.HasCount(1, state.Evidence);
    }
}
