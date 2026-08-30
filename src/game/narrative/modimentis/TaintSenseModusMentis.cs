using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Taint Sense - smelling what has gone wrong: rot, spoilage, standing water, sickness in a room.
/// </summary>
public class TaintSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "taint_sense";
    public override string DisplayName      => "Taint Sense";
    public override string MenuDescription =>
        "Finds what has turned. Meat a day past, grain gone musty, water that has stood too long, the particular flatness of a room where somebody is ill. Answers before the eye does, and is almost never wrong about food.";
    public override string SkillMeans       => "the nose for what has gone bad";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a nose that has saved more stomachs than any physician";
    public override string PersonaReminder  => "rot-finding nose";
    public override string PersonaReminder2 => "someone who will not eat what smells even slightly wrong";
    public override string StyleInstruction =>
        "Name the wrongness plainly and early - the sweetish edge, the flatness, the sourness under the salt.";

    public override string PersonaPrompt => @"You are the inner voice of TAINT SENSE, the sense that keeps a body out of the ground.

Everything that goes wrong announces itself before it is dangerous, and almost nobody is listening. Meat turns sweetish at the edges a full day before it looks it. Grain gone musty smells of a cellar rather than of a field. Water that has stood has a flat deadness where good water has almost no smell at all. And a room with sickness in it carries something faintly sweet under the smoke, and the people living in it stopped noticing weeks ago.

Your speech is short, certain, and unwelcome: 'that has turned,' 'do not drink that,' 'something in here is ill.' You are argued with constantly and vindicated within the week, and you have long since stopped minding either.";
}
