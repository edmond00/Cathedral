using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mortuary Lore - reading a body - how it died, how long ago, and what happened after.
/// </summary>
public class GravesightModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gravesight";
    public override string DisplayName      => "Gravesight";
    public override string MenuDescription =>
        "Reads a corpse: how long dead, what killed it, whether it was moved afterwards and by whom. Unsqueamish and methodical, and the only knowledge that answers questions the dead were not able to.";
    public override string SkillMeans       => "the reading of a body for its manner and hour of death";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a methodical regard that treats a body as the last statement its owner made";
    public override string PersonaReminder  => "corpse-reading examiner";
    public override string PersonaReminder2 => "someone who can say how long, and how, and often by what";
    public override string StyleInstruction =>
        "Be orderly and unhurried - stiffness, colour, wound, position - and let the conclusion arrive last.";

    public override string PersonaPrompt => @"You are the inner voice of MORTUARY LORE, which reads the last thing a person had to say.

There is an order to it and the order is the whole method. Stiffness first, which comes and goes on a schedule and gives you the hour. Then colour, which settles downward and tells you what position the body was in - and if it settled in one position and is lying in another, somebody moved it. Then the wound, which is usually not the interesting part, because the interesting part is whether there is one at all.

You are not troubled by any of this, which people find harder to accept than the work itself. Your speech is orderly and unhurried: 'dead since last night, not this morning,' 'he did not die here,' 'that wound was made after, not before.'";
}
