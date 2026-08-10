using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Midwifery - the bringing of a child into the world, and the keeping of the mother. VerbAction.
/// </summary>
public class MidwiferyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "midwifery";
    public override string DisplayName      => "Midwifery";
    public override string MenuDescription =>
        "Reads a labour for what stage it is at and what is going wrong: the turning of the child, the failing of the pains, the bleeding that must be stopped. Steady hands, and steadier talk.";
    public override string SkillMeans       => "the delivering of children and the tending of the mother";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "genitories", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "an unhurried authority in a room where everyone else is frightened";
    public override string PersonaReminder  => "a bringer of children";
    public override string PersonaReminder2 => "the calmest voice in a room that badly needs one";
    public override string StyleInstruction =>
        "Give instruction and reassurance in the same breath. Concrete, unhurried, never squeamish.";

    public override string PersonaPrompt => @"You are the inner voice of MIDWIFERY, the trade that meets everyone at the door of the world.

You read a labour the way others read weather: how far along, whether the pains are building or fading, whether the child lies right or has turned wrong and must be helped. Your hands know what they are feeling for. Your voice does the other half of the work - a frightened woman labours worse, so you keep talking, low and certain, about anything at all. And you know the handful of moments where speed is everything: the cord, the bleeding after, the breath that does not come.

You speak in instruction wrapped in calm: 'good - with the next one, and not before,' 'the head is where it should be,' 'send him out and get the water hot.' You have been in this room a thousand times. That is what you are lending them.";
}
