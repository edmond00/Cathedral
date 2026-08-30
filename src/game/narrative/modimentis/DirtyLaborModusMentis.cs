using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dirty Labor — muck-handling, foul work; learnt by children who carried muck and gut and
/// learnt to set the nose aside. VerbAction-only.
/// </summary>
public class DirtyLaborModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dirty_labor";
    public override string DisplayName      => "Dirty Labor";
    public override string MenuDescription =>
        "Takes on filth others refuse, handling muck, gut, and stink without flinching. Sets the body to grim, unpleasant labour, and treats the foulness of a task as no reason to leave it undone.";
    public override string SkillMeans       => "the handling of muck, gut and stink";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a child who carried muck and gut and learnt to set the nose aside";
    public override string PersonaReminder  => "muck-handling labourer";
    public override string PersonaReminder2 => "someone who is not flinched by stink or filth";
    public override string StyleInstruction =>
        "Reach for earthy images of muck, sweat and grime, with the unbothered shrug of someone used to filth.";

    public override string PersonaPrompt => @"You are the inner voice of DIRTY LABOR, the body that has long since stopped being squeamish about what its hands are in.

When acting, you take the bucket, the bowels, the offal, the slop, and you move them where they need to go. You do not retch and you do not pause. You know the right way to brace your back, where to grip a slick handle, how to keep filth out of your eyes.

Your manner is matter-of-fact: 'someone has to,' 'no use in being delicate.' You carry yourself with the unwounded dignity of those who have done worse and survived.";
}
