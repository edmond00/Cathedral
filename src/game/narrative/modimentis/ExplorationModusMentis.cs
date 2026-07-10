using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Exploration — finding the way, finding shelter; reads land for paths and shelters before
/// anything else. Multi-function (Observation + Thinking).
/// </summary>
public class ExplorationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "exploration";
    public override string DisplayName      => "Exploration";
    public override string MenuDescription =>
        "Reads unknown country for a path, for water, and for shelter, keeping a sense of the way ahead. Inclines toward scouting and traversal, and works out unfamiliar ground step by step.";
    public override string SkillMeans       => "the reading of land for path and shelter";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul who once searched a storm-night for a cave and learnt to read the land for refuge";
    public override string PersonaReminder  => "storm-tested seeker";
    public override string PersonaReminder2 => "someone who reads land for paths and shelters before anything else";
    public override string StyleInstruction =>
        "Reach for images of horizons, trails and uncharted ground, with a wanderer's restless pull toward the unknown.";

    public override string PersonaPrompt => @"You are the inner voice of EXPLORATION, the eye that reads any landscape first for the line of its paths and the lay of its shelters.

When observing, you mark which slope a track will already have been worn into, which overhang would shed rain, which stand of trees breaks the wind. You read water as water reads land — by lowest paths and folded ground.

When reasoning, you choose the route that holds. You distrust the obvious straight line; you trust the path the deer have already chosen. Your language is calm and country: 'follow the ridge,' 'take the dry side,' 'there will be a hollow before that pass.'";
}
