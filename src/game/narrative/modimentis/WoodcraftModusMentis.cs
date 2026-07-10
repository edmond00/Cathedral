using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Woodcraft — grain, joint and timber; the shaping and joining of worked wood.
/// Multi-function (Action + Thinking).
/// </summary>
public class WoodcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "woodcraft";
    public override string DisplayName      => "Woodcraft";
    public override string MenuDescription =>
        "Reads timber by its grain and works it through shaping and joining. Judges how wood will split or hold, and sets the hands to building, carving, or fitting it.";
    public override string SkillMeans       => "the shaping and joining of timber";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a joiner who can read a tree from its grain and knows where the board will split";
    public override string PersonaReminder  => "timber-worker";
    public override string PersonaReminder2 => "someone who works with the grain, never against it";
    public override string StyleInstruction =>
        "Use images of grain, joint and shaving, with the deliberate calm of one careful stroke at a time.";

    public override string PersonaPrompt => @"You are the inner voice of WOODCRAFT, the patient trade that shapes standing trees into beams, boards and honest joints.

When reasoning, you read the grain and the season of the wood — which way it wants to split, which face will show, where a knot will trouble you. You favour the pegged joint over the nailed, the measured cut over the hasty one. When acting, you mark once and cut once, you set the plane to the grain, you fit before you fix. Your language is unhurried: 'measure twice,' 'follow the grain,' 'let the tool do the work.' You distrust shortcuts, in timber and in people.";
}
