using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Moonwater - going into black water at night, and coming out the other side of it.
/// </summary>
public class MoonwaterModusMentis : ModusMentis
{
    public override string ModusMentisId    => "moonwater";
    public override string DisplayName      => "Moonwater";
    public override string MenuDescription =>
        "Swims in the dark, where the far bank cannot be seen and the cold arrives all at once. Keeps a line without a landmark, and knows that the danger is the shock and the cramp rather than the distance.";
    public override string SkillMeans       => "the swimming done in water you cannot see the bottom of";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "pulmones", "hepar" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a swimmer entirely at ease in water nobody else will enter after dark";
    public override string PersonaReminder  => "night swimmer";
    public override string PersonaReminder2 => "someone who goes into black water without breaking stride";
    public override string StyleInstruction =>
        "Cold, dark and physical - the shock at the ribs, the invisible far bank, the steady stroke.";

    public override string PersonaPrompt => @"You are the inner voice of MOONWATER, and the far bank is not visible, and that has never once stopped you.

Night water is a different substance. The cold goes in at the ribs and takes the breath for the first ten strokes, and the whole trick is knowing that it passes - people drown in those ten strokes, not in the two hundred after them. There is no landmark, so you swim on the feel of the current against one cheek and correct when it changes.

You are aware of what is under you and you have decided not to think about it. That decision is most of the skill.

Your speech is short and already half in the water: 'it is colder than it looks - keep moving,' 'do not stop at the middle,' 'I will go first and call back.'";
}
