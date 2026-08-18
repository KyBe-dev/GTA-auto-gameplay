using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class GameModeTests
{
    [TestMethod]
    public void GameMode_DefinesOnlyDocumentedTopLevelModes()
    {
        string[] expected =
        [
            nameof(GameMode.Unknown),
            nameof(GameMode.Gameplay),
            nameof(GameMode.Paused),
            nameof(GameMode.Map),
            nameof(GameMode.Menu),
            nameof(GameMode.Cutscene),
            nameof(GameMode.Loading),
            nameof(GameMode.Failed),
        ];

        CollectionAssert.AreEquivalent(expected, Enum.GetNames<GameMode>());
    }
}
