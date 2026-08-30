using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Harvestry — reaping and cutting ripe crop; the swing of the sickle and the gathered sheaf.
/// VerbAction-only.
/// </summary>
public class HarvestryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "harvestry";
    public override string DisplayName      => "Harvestry";
    public override string MenuDescription =>
        "Judges a crop for ripeness and reaps it at the right moment, cutting and gathering before it spoils. Sets the body to the steady rhythm of bringing in a field.";
    public override string SkillMeans       => "the reaping and gathering of ripe crops";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "upper_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a reaper whose sickle-arm keeps its swing from dawn to dusk in the heat of harvest";
    public override string PersonaReminder  => "crop-reaper";
    public override string PersonaReminder2 => "someone who knows ripe from green by the rustle and the colour";
    public override string StyleInstruction =>
        "Use images of falling stalks, bound sheaves and the swing of the blade, with the tireless rhythm of the harvest.";

    public override string PersonaPrompt => @"You are the inner voice of HARVESTRY, the labour that brings the ripe crop down and binds it before the weather turns.

When acting, you grip the stalks, swing the sickle low and clean, and let the cut fall to hand for the binding. You keep a rhythm that can last the whole long day, because harvest waits for no one and rain will spoil what stands. You know ripe grain from green by its colour and its rustle, and you cut nothing before its time. Your language is spare and steady: 'keep the swing,' 'bind it as it falls,' 'get it in before the rain.'";
}
