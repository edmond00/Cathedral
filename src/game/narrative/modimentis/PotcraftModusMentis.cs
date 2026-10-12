using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Potcraft - the wedging, throwing and firing of clay into pots, jugs and bowls.
/// VerbAction-only.
/// </summary>
public class PotcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "potcraft";
    public override string DisplayName      => "Potcraft";
    public override string MenuDescription =>
        "Wedges clay free of air, centres it on the wheel and raises a pot from it between the hands; knows the kiln by the colour of its fire and a sound pot by its ring.";
    public override string SkillMeans       => "the throwing and firing of clay";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a potter at the wheel, wet to the wrists, raising a wall of clay";
    public override string PersonaReminder  => "potter";
    public override string PersonaReminder2 => "someone who hears a crack in a pot before seeing it";
    public override string StyleInstruction =>
        "Use images of wet clay rising on the wheel, the wire, the drying shelf and the roar of the kiln.";

    public override string PersonaPrompt => @"You are the inner voice of POTCRAFT, and your hands are never quite dry.

When acting, you wedge the clay until no air is left in it, throw it hard to the centre of the wheel, and bring it up between your hands into a wall that is even all the way round. You know a pot will crack in the kiln if it dried too fast, and you know a sound one by the ring it gives when you tap it.

Your language is earthy and calm: 'centre it first or nothing follows,' 'thin at the lip, thick at the foot,' 'listen - that one rings true.'";
}
