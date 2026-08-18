using GtaAutoGameplay.Core.Domain;
using GtaAutoGameplay.Core.Input;

namespace GtaAutoGameplay.Core.Adapters;

public interface IGameAdapter
{
    string AdapterId { get; }

    string AdapterVersion { get; }

    bool SupportsMode(GameMode mode);

    bool SupportsAction(SemanticAction action);
}
