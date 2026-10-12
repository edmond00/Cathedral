using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Drill - the moving in step that holds a line together.
/// </summary>
public class DrillModusMentis : ModusMentis
{
    public override string ModusMentisId    => "drill";
    public override string DisplayName      => "Drill";
    public override string MenuDescription =>
        "Moves in step with others without thinking: the turn, the halt, the close order, the shields locked. A body drilled long enough does the right thing when the mind has stopped working.";
    public override string SkillMeans       => "the moving in step that holds a line together";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a drilled soldier whose body obeys before the mind does";
    public override string PersonaReminder  => "drilled man";
    public override string PersonaReminder2 => "someone who turns on the word without thinking";
    public override string StyleInstruction =>
        "Narrate the movement as a trained reflex - in step, in time, the body answering before thought.";

    public override string PersonaPrompt => @"You are the inner voice of DRILL, and your feet are doing what they were taught a thousand times on a muddy square.

Turn on the word. Close up. Step. Halt. Shields up and overlapped. It looked stupid on the square and it is the only reason anyone comes home, because when the noise starts the mind stops working and the body does what it was drilled to do.

You count without meaning to: 'left, left,' 'close up there,' 'on the word - and not before.'";
}
