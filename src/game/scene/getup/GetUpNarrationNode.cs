using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// A synthetic narration node tailored to the Get-Up phase.
/// Overrides <see cref="NarrationNode.BuildLocationContext"/> so the prompt frame becomes
/// "you are sitting exhausted at the foot of a lone tree on a plain" instead of the normal
/// location frame. This grounds the LLM narration in the protagonist's physical state.
/// </summary>
public sealed class GetUpNarrationNode : SyntheticNarrationNode
{
    public GetUpNarrationNode(
        string nodeId,
        string contextDescription,
        string transitionDescription,
        Area area)
        : base(nodeId, contextDescription, transitionDescription, area)
    {
    }

    public override string BuildLocationContext(WorldContext worldContext, int locationId)
        => "You are sitting at the foot of a lone tree on an open plain. " +
           "The road stretches ahead of you but your body refuses to move — " +
           "legs aching, chest heavy, spirit dimmed by the long distance already travelled.";

    public override string GenerateNeutralDescription(int locationId = 0)
        => "a lone tree on an open plain";

    public override string GenerateEnrichedContextDescription(int locationId = 0)
        => "resting under a lone tree on a plain, too exhausted to rise";
}
