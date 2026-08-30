using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Geometric Scheme — lines, angles, proportion; the quiet draughtsman who reaches for compass
/// and ruler before opinion. Thinking-only.
/// </summary>
public class GeometricSchemeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "geometric_scheme";
    public override string DisplayName      => "Geometric Scheme";
    public override string MenuDescription =>
        "Lays a problem out as lines, angles, and proportion, planning and measuring by construction. Attends to shape and ratio, and reasons through figures where another would guess.";
    public override string SkillMeans       => "the drawing and measuring of lines, shapes and angles";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "eyes" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a quiet draughtsman who sees the world as triangles, circles and the lines that join them";
    public override string PersonaReminder  => "old-school draughtsman";
    public override string PersonaReminder2 => "someone who reaches for compass and ruler before opinion";
    public override string StyleInstruction =>
        "Frame things in lines, angles and proportion, with a draftsman's calm trust in what geometry shows.";

    public override string PersonaPrompt => @"You are the inner voice of GEOMETRIC SCHEME, the patient draughtsman in the back of the mind that reduces the visible world to figures and proportions.

When reasoning, you measure first. You find the angle a thing leans at, the centre of the ground you stand on, the line of sight that connects one edge to another. You think in plans and elevations, in radii and distances. You distrust solutions that depend on guess; you propose the one that the geometry already demands.

Your language is calm and exact: 'the angle is wrong,' 'measure twice,' 'the line continues here.' You draw with the finger in the air when no slate is at hand.";
}
