using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class GameStateTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void ControlMode_DefinesOnlyDocumentedContexts()
    {
        string[] expected =
        [
            nameof(ControlMode.Unknown),
            nameof(ControlMode.OnFoot),
            nameof(ControlMode.Driving),
            nameof(ControlMode.Aiming),
            nameof(ControlMode.UI),
        ];

        CollectionAssert.AreEqual(expected, Enum.GetNames<ControlMode>());
        CollectionAssert.AreEqual(
            Enumerable.Range(0, expected.Length).ToArray(),
            Enum.GetValues<ControlMode>().Select(value => (int)value).ToArray());
    }

    [TestMethod]
    public void ObjectiveType_DefinesOnlyDocumentedObjectives()
    {
        string[] expected =
        [
            nameof(ObjectiveType.Unknown),
            nameof(ObjectiveType.GoTo),
            nameof(ObjectiveType.Follow),
            nameof(ObjectiveType.Interact),
            nameof(ObjectiveType.Drive),
            nameof(ObjectiveType.Wait),
            nameof(ObjectiveType.Search),
        ];

        CollectionAssert.AreEqual(expected, Enum.GetNames<ObjectiveType>());
        CollectionAssert.AreEqual(
            Enumerable.Range(0, expected.Length).ToArray(),
            Enum.GetValues<ObjectiveType>().Select(value => (int)value).ToArray());
    }

    [TestMethod]
    public void Constructor_UsesSafeDefaults()
    {
        GameState state = new(ObservedAt);

        Assert.AreEqual(GameMode.Unknown, state.GameMode);
        Assert.AreEqual(ControlMode.Unknown, state.ControlMode);
        Assert.AreEqual(ObjectiveType.Unknown, state.ObjectiveType);
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
    public void Constructor_RejectsUnknownControlMode()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GameState(ObservedAt, controlMode: (ControlMode)999));
    }

    [TestMethod]
    public void Constructor_RejectsUnknownObjectiveType()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GameState(ObservedAt, objectiveType: (ObjectiveType)999));
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
