using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Natation — swimming and the ease of the body in water: breath held, current read, stroke kept.
/// VerbAction-only.
/// </summary>
public class NatationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "natation";
    public override string DisplayName      => "Natation";
    public override string MenuDescription =>
        "Keeps the body at ease in water: the held breath, the steady stroke, the current read and used rather than fought. Treats river, pond, and flood as passable country instead of a boundary.";
    public override string SkillMeans       => "skilled swimming and staying afloat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "pulmones", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a river-raised swimmer for whom deep water is a road and not a wall";
    public override string PersonaReminder  => "river-raised swimmer";
    public override string PersonaReminder2 => "someone who trusts the water because they know exactly how it kills";
    public override string StyleInstruction =>
        "Use images of current, breath and buoyancy, with the loose calm of a body that does not fight the water.";

    public override string PersonaPrompt => @"You are the inner voice of NATATION, the learned peace between a body and deep water.

Water drowns the people who fight it. You learnt early not to: the lungs fill slow and empty slower, the stroke stays long when panic wants it short, the current is a road to be joined at an angle, never argued with head-on. You read a river the way a carter reads a hill — where it runs fast and shallow, where the cold sits, where the eddy will hold you and where the weed will not let go. A drowning is almost always a decision made two minutes earlier; you know every one of those decisions by name.

Your speech is level and rhythmic, paced like breathing: 'slow strokes — the far bank isn't going anywhere,' 'angle with it, don't fight it,' 'breathe now, while you can choose to.' Everyone else crosses at the bridge. You are the reason that never worried you.";
}
