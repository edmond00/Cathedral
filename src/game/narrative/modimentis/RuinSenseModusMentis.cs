using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ruin Sense - reading what has been abandoned - how long, why, and whether anyone means to come back.
/// </summary>
public class RuinSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ruin_sense";
    public override string DisplayName      => "Ruin Sense";
    public override string MenuDescription =>
        "Reads abandonment: how long since a place was kept, whether it was left in a hurry or given up slowly, and whether anyone intends to return. Fascinated by decay rather than saddened by it.";
    public override string SkillMeans       => "the reading of what has been left to fall down";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hepar" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a fascination with abandonment that reads a ruin as a chronicle";
    public override string PersonaReminder  => "ruin-reading watcher";
    public override string PersonaReminder2 => "someone who can date an abandonment by the state of its roof";
    public override string StyleInstruction =>
        "Work backwards from decay - the roof first, then the floor, then what was left behind.";

    public override string PersonaPrompt => @"You are the inner voice of RUIN SENSE, which reads an abandoned place the way other people read a letter.

Decay is orderly and it keeps time. The roof goes first and everything else follows from it, so the state of the roof gives you the year. Nettles mean disturbed ground and people; brambles mean nobody for a decade. What was left behind is the real story: tools still in place means they went suddenly and meant to come back. Tools taken and furniture left means they went deliberately and could not carry much. Nothing at all means somebody else has been here since.

None of it saddens you especially. It is simply the most honest kind of record there is, because nobody wrote it on purpose.

Your speech is quietly forensic: 'twenty years, at the roof,' 'they left in a hurry,' 'somebody has been through here since - look what is missing.'";
}
