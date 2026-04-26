using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;

namespace Cathedral.Game.Scene.Reminescence;

/// <summary>
/// A synthetic narration node tailored to the childhood reminescence phase.
/// Overrides <see cref="NarrationNode.BuildLocationContext"/> so the prompt frame becomes
/// "you are sitting exhausted at the foot of a tree, remembering …" plus the protagonist's
/// currently filled childhood-history summary, instead of the normal "you are in a [biome]
/// — you are currently [exploring …]" frame.
/// </summary>
public sealed class ReminescenceNarrationNode : SyntheticNarrationNode
{
    private readonly Protagonist _protagonist;
    private readonly ReminescenceData _data;

    public ReminescenceNarrationNode(
        string nodeId,
        string contextDescription,
        string transitionDescription,
        Area area,
        Protagonist protagonist,
        ReminescenceData data)
        : base(nodeId, contextDescription, transitionDescription, area)
    {
        _protagonist = protagonist;
        _data        = data;
    }

    public override string BuildLocationContext(WorldContext worldContext, int locationId)
    {
        var history = _protagonist.ChildhoodHistory.ToPromptSummary();
        var historyClause = history.Length == 0 ? string.Empty : $" {history}";
        var theme = _data.ContentLines.Count > 1 ? $" You are reminded of {_data.ContentLines[^1]}." : string.Empty;
        return
            $"You are sitting exhausted at the foot of a tree, remembering what brought you here." +
            $" Half-formed images of your childhood drift through your mind.{theme}{historyClause}";
    }

    public override string GenerateNeutralDescription(int locationId = 0)
        => "fuzzy childhood memories";

    public override string GenerateEnrichedContextDescription(int locationId = 0)
        => "remembering fragments of your childhood at the foot of a tree";
}
