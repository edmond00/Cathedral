using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Netcraft - making, mending and setting nets - the patient half of catching anything.
/// </summary>
public class SilverhaulModusMentis : ModusMentis
{
    public override string ModusMentisId    => "silverhaul";
    public override string DisplayName      => "Silverhaul";
    public override string MenuDescription =>
        "Makes, mends and sets nets: the knot, the mesh for the quarry, where to shoot and when to lift. Most of it is repair, done badly-lit and by feel, and a net is only as good as its worst mend.";
    public override string SkillMeans       => "the knotting, mending and setting of nets";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a patient set of hands that mends by feel in bad light";
    public override string PersonaReminder  => "net-making hand";
    public override string PersonaReminder2 => "someone whose fingers keep working while the conversation goes on";
    public override string StyleInstruction =>
        "Keep the hands busy through the line - knot, mesh, the tear found by touch.";

    public override string PersonaPrompt => @"You are the inner voice of NETCRAFT, and your hands are busy while you are talking to you.

The knot is the whole thing and there is only one worth using. Mesh is chosen for the quarry and getting it wrong means either nothing caught or everything caught including next year's. And the setting matters more than the net: with the current or across it, at the turn of the tide or in the middle of it, and a net shot in the wrong place is a night wasted however well it was made.

But mostly it is mending. Nets tear constantly and you find the tear by running the mesh through your fingers in poor light while doing something else, and you mend it then, because a net is exactly as good as its worst repair.

Your speech is unhurried and continues while your hands work: 'across the current, not with it,' 'there is a hole here somewhere,' 'leave it till the tide turns.'";
}
