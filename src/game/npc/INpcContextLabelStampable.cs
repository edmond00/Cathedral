using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Implemented by observation outcomes that wrap a named NPC and can have their prompt-facing
/// text swapped from the raw proper name to a contextual <c>Label</c>. The controllers (which
/// hold the <see cref="WorldContext"/>, location and acting party member) call
/// <see cref="StampContextLabel"/> before generating prompts; shallow/unlabelled objects no-op.
/// </summary>
public interface INpcContextLabelStampable
{
    void StampContextLabel(PartyMember? actingMember, WorldContext? world, int locationId);
}
